using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.World;

namespace EchoStation.Killer
{
    /// <summary>
    /// The killer (Khalaf, masked). A real, tidy, polite human: he never runs, never shouts, never teleports.
    /// In the demo he appears only in SCRIPTED ENCOUNTERS (StartEncounter / EndEncounter): no free roaming.
    ///
    /// States:  Walk (patrols a path, rubber boots, slow breathing behind the shemagh)
    ///          Suspicious (heard something: he STOPS BREATHING to listen, a plastic click of the headphones)
    ///          Search (walks to the last noise, looks around)
    ///          Catch (close + sees Ali, or hears him breathing next to the hiding place)
    ///
    /// Hearing: echo pulses (EchoPulse.OnPulse), Ali's footsteps (not when crouching), Ali's breathing
    /// when he is within 2 m and Ali is not holding his breath. A generator blackout makes him come closer.
    /// He is real: give him an EchoSurface (Body) so the pulse returns a dull "thud".
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class KillerController : MonoBehaviour
    {
        public enum State { Idle, Walk, Suspicious, Search, Catch }

        [Header("Movement (never runs)")]
        [SerializeField] float walkSpeed = 1.05f;
        [SerializeField] float searchSpeed = 1.35f;
        [SerializeField] Transform[] path;

        [Header("Senses")]
        [SerializeField] float footstepHearing = 7f;
        [SerializeField] float breathHearing = 2f;
        [SerializeField] float sightRange = 9f;
        [SerializeField] float sightAngle = 50f;
        [SerializeField] float catchDistance = 1.3f;
        [SerializeField] float suspiciousTime = 3.5f;
        [SerializeField] float searchTime = 9f;
        [SerializeField] LayerMask sightBlockers = ~0;

        [Header("Sound")]
        [SerializeField] AudioSource breath;          // loop: slow breathing through cloth
        [SerializeField] AudioSource steps;
        [SerializeField] AudioClip[] bootSteps;       // rubber boots: different from Ali's leather shoes
        [SerializeField] AudioClip headphoneClick;    // he holds his breath and listens
        [SerializeField] float stepInterval = 0.75f;

        public State Current { get; private set; } = State.Idle;
        public event System.Action OnCaughtAli;

        NavMeshAgent agent;
        AliController ali;
        int pathIndex;
        Vector3 lastNoise;
        float stateTimer, stepTimer, breathBase;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = walkSpeed;
            agent.angularSpeed = 140f;
            agent.acceleration = 2f;
            ali = FindFirstObjectByType<AliController>();
            if (breath) breathBase = breath.volume;
        }

        void OnEnable()
        {
            EchoPulse.OnPulse += HeardPulse;
            GeneratorPower.OnPowerChanged += PowerChanged;
            BreathHold.OnGasp += HeardGasp;
        }

        void OnDisable()
        {
            EchoPulse.OnPulse -= HeardPulse;
            GeneratorPower.OnPowerChanged -= PowerChanged;
            BreathHold.OnGasp -= HeardGasp;
        }

        void HeardGasp(Vector3 at)
        {
            if (Current == State.Idle || Current == State.Catch) return;
            if (Vector3.Distance(transform.position, at) < footstepHearing * 1.5f) { lastNoise = at; SetState(State.Search); }
        }

        // ---------- scripted encounters ----------

        public void StartEncounter(Transform[] encounterPath = null)
        {
            if (encounterPath != null && encounterPath.Length > 0) path = encounterPath;
            gameObject.SetActive(true);
            pathIndex = 0;
            SetState(State.Walk);
        }

        public void EndEncounter()
        {
            SetState(State.Idle);
            gameObject.SetActive(false);
        }

        // ---------- loop ----------

        void Update()
        {
            if (Current == State.Idle || Current == State.Catch || ali == null) return;
            stateTimer += Time.deltaTime;
            Footsteps();

            if (CanCatch()) { Catch(); return; }

            // Hearing Ali walk (crouching is silent).
            float d = Vector3.Distance(transform.position, ali.transform.position);
            if (ali.IsMoving && !ali.IsCrouching && d < footstepHearing) Noise(ali.transform.position);
            if (ali.IsHidden && !ali.BreathHeld && d < breathHearing) Noise(ali.transform.position);

            switch (Current)
            {
                case State.Walk:
                    if (path != null && path.Length > 0 && !agent.pathPending && agent.remainingDistance < 0.3f)
                    {
                        pathIndex = (pathIndex + 1) % path.Length;
                        agent.SetDestination(path[pathIndex].position);
                    }
                    break;
                case State.Suspicious:
                    // Standing still, holding his breath, head turning toward the noise.
                    FaceTowards(lastNoise);
                    if (stateTimer > suspiciousTime) SetState(State.Search);
                    break;
                case State.Search:
                    if (!agent.pathPending && agent.remainingDistance < 0.4f) FaceTowards(lastNoise + Random.insideUnitSphere);
                    if (stateTimer > searchTime) SetState(State.Walk);
                    break;
            }
        }

        bool CanCatch()
        {
            Vector3 to = ali.transform.position - transform.position;
            float d = to.magnitude;
            if (d < catchDistance && (!ali.IsHidden || !ali.BreathHeld && Current == State.Search)) return true;
            if (ali.IsHidden || d > sightRange) return false;
            if (Vector3.Angle(transform.forward, to) > sightAngle) return false;
            Vector3 eye = transform.position + Vector3.up * 1.65f;
            Vector3 target = ali.transform.position + Vector3.up * (ali.IsCrouching ? 0.8f : 1.5f);
            // Sight needs light: in darkness he only sees you very close.
            if (!GeneratorPower.PowerOn && d > 3f) return false;
            return !Physics.Linecast(eye, target, sightBlockers & ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore)
                   && d < catchDistance * 2.2f;   // he walks up to you; no lunges
        }

        void Catch()
        {
            SetState(State.Catch);
            agent.isStopped = true;
            OnCaughtAli?.Invoke();
        }

        void HeardPulse(Vector3 at, float radius)
        {
            if (Current == State.Idle || Current == State.Catch) return;
            if (Vector3.Distance(transform.position, at) <= radius) Noise(at);
        }

        void PowerChanged(bool on)
        {
            // In the dark he walks toward the last place he heard Ali, or toward Ali if close.
            if (!on && ali != null && Current != State.Idle && Current != State.Catch) Noise(ali.transform.position);
        }

        void Noise(Vector3 at)
        {
            lastNoise = at;
            if (Current == State.Walk) SetState(State.Suspicious);
            else if (Current == State.Search) agent.SetDestination(at);
        }

        void SetState(State s)
        {
            Current = s; stateTimer = 0f;
            if (agent.isOnNavMesh) agent.isStopped = s == State.Suspicious || s == State.Idle || s == State.Catch;
            switch (s)
            {
                case State.Walk:
                    agent.speed = walkSpeed;
                    if (path != null && path.Length > 0 && agent.isOnNavMesh) agent.SetDestination(path[pathIndex].position);
                    StartCoroutine(FadeBreath(breathBase, 1.2f));
                    break;
                case State.Suspicious:
                    // The warning is an ABSENCE of sound: his breathing stops.
                    StartCoroutine(FadeBreath(0f, 0.25f));
                    if (steps && headphoneClick) steps.PlayOneShot(headphoneClick, 0.5f);
                    break;
                case State.Search:
                    agent.speed = searchSpeed;
                    if (agent.isOnNavMesh) agent.SetDestination(lastNoise);
                    StartCoroutine(FadeBreath(breathBase * 0.6f, 1f));
                    break;
            }
        }

        void Footsteps()
        {
            if (steps == null || bootSteps == null || bootSteps.Length == 0) return;
            if (agent.velocity.sqrMagnitude < 0.05f) return;
            stepTimer += Time.deltaTime;
            if (stepTimer < stepInterval) return;
            stepTimer = 0f;
            steps.pitch = Random.Range(0.9f, 1.02f);
            steps.PlayOneShot(bootSteps[Random.Range(0, bootSteps.Length)], 0.8f);
        }

        void FaceTowards(Vector3 p)
        {
            Vector3 dir = p - transform.position; dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 70f * Time.deltaTime);
        }

        IEnumerator FadeBreath(float to, float dur)
        {
            if (breath == null) yield break;
            if (!breath.isPlaying) breath.Play();
            float from = breath.volume;
            for (float t = 0; t < dur; t += Time.deltaTime) { breath.volume = Mathf.Lerp(from, to, t / dur); yield return null; }
            breath.volume = to;
        }
    }
}
