using System.Collections;
using UnityEngine;
using EchoStation.Haunt;
using EchoStation.Killer;
using EchoStation.World;

namespace EchoStation.Audio
{
    /// <summary>
    /// The sound of fear (docs/team/sound-study.md). Principles applied:
    ///  - A low dread layer under everything (-30 dB), raised to -18 dB ~20 s before events: the player
    ///    gets uneasy without knowing why. Call RaiseDread() from DemoClock beats / anomaly steps.
    ///  - "Somewhere else" sounds: a distant iron door, a chair dragged upstairs, a drip in a room you never reach,
    ///    at random intervals, only when nothing else is happening.
    ///  - Whispers in ONE ear, dry (no reverb: hallucination).
    ///  - Marsh frogs outside go quiet when danger is close (killer near or tantal active).
    /// Room reverb is done with AudioReverbZone components per room (short corridor, long hall, metallic pipe);
    /// hallucination sources set bypassReverbZones = true.
    /// </summary>
    public class AmbienceDirector : MonoBehaviour
    {
        [Header("Dread layer")]
        [SerializeField] AudioSource dread;            // dread_drone_loop, 2D
        [SerializeField] float quietDb = -30f, tenseDb = -18f;

        [Header("Somewhere else")]
        [SerializeField] AudioSource elsewhere;        // 3D source, moved around
        [SerializeField] AudioClip[] elsewhereClips;   // distant door, chair drag, far drip, pipe groan
        [SerializeField] Transform[] elsewherePoints;  // behind walls, above ceilings, rooms you never reach
        [SerializeField] Vector2 elsewhereInterval = new Vector2(40f, 90f);

        [Header("Whispers")]
        [SerializeField] AudioSource whisper;          // 2D, bypassReverbZones = true

        [Header("Frogs (outside)")]
        [SerializeField] AudioSource frogs;
        [SerializeField] KillerController killer;
        [SerializeField] TantalStalker tantal;
        [SerializeField] float frogsSilenceDistance = 12f;

        float elsewhereTimer, frogsBase;
        Coroutine dreadRoutine;

        void Start()
        {
            if (dread) { dread.volume = DbToLin(quietDb); if (!dread.isPlaying) dread.Play(); }
            if (whisper) { whisper.spatialBlend = 0f; whisper.bypassReverbZones = true; }
            if (frogs) frogsBase = frogs.volume;
            elsewhereTimer = Random.Range(elsewhereInterval.x, elsewhereInterval.y);
        }

        void Update()
        {
            // Somewhere-else sounds only while the generator hums (the "calm").
            elsewhereTimer -= Time.deltaTime;
            if (elsewhereTimer <= 0f)
            {
                elsewhereTimer = Random.Range(elsewhereInterval.x, elsewhereInterval.y);
                if (GeneratorPower.PowerOn) PlayElsewhere();
            }

            if (frogs)
            {
                bool danger = (killer && killer.isActiveAndEnabled && killer.Current != KillerController.State.Idle
                               && Camera.main && Vector3.Distance(killer.transform.position, Camera.main.transform.position) < frogsSilenceDistance)
                              || (tantal && tantal.isActiveAndEnabled);
                frogs.volume = Mathf.MoveTowards(frogs.volume, danger ? 0f : frogsBase, Time.deltaTime * frogsBase * 0.8f);
            }
        }

        /// <summary>Raise the dread layer over 'rise' seconds, hold, then fall back.</summary>
        public void RaiseDread(float rise = 20f, float hold = 15f)
        {
            if (!dread) return;
            if (dreadRoutine != null) StopCoroutine(dreadRoutine);
            dreadRoutine = StartCoroutine(Dread(rise, hold));
        }

        IEnumerator Dread(float rise, float hold)
        {
            float from = dread.volume, to = DbToLin(tenseDb);
            for (float t = 0; t < rise; t += Time.deltaTime) { dread.volume = Mathf.Lerp(from, to, t / rise); yield return null; }
            yield return new WaitForSeconds(hold);
            float q = DbToLin(quietDb);
            for (float t = 0; t < 8f; t += Time.deltaTime) { dread.volume = Mathf.Lerp(to, q, t / 8f); yield return null; }
        }

        public void PlayElsewhere()
        {
            if (!elsewhere || elsewhereClips == null || elsewhereClips.Length == 0) return;
            if (elsewherePoints != null && elsewherePoints.Length > 0)
                elsewhere.transform.position = elsewherePoints[Random.Range(0, elsewherePoints.Length)].position;
            elsewhere.pitch = Random.Range(0.85f, 1.05f);
            elsewhere.PlayOneShot(elsewhereClips[Random.Range(0, elsewhereClips.Length)], Random.Range(0.4f, 0.8f));
        }

        /// <summary>A dry whisper in one ear (left = -1, right = +1).</summary>
        public void Whisper(AudioClip clip, int side = 1)
        {
            if (!whisper || !clip) return;
            whisper.panStereo = Mathf.Clamp(side, -1, 1) * 0.9f;
            whisper.PlayOneShot(clip);
        }

        static float DbToLin(float db) => Mathf.Pow(10f, db / 20f);
    }
}
