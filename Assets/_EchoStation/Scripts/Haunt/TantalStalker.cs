using UnityEngine;

namespace EchoStation.Haunt
{
    /// <summary>
    /// The Tantal (Ali's fear that the truth will catch him). A HALLUCINATION on the Hallucination layer: no echo.
    /// It has no sound of its own: its signature is the ABSENCE of sound. Each time it comes closer,
    /// one more sound of the place fades out (wind, then mud, then Ali's own breathing).
    /// It is NEVER seen moving: it only steps while out of view, or on a "blink" (the hazard lights' dark frame,
    /// the flashlight dying). Each step it is closer AND taller.
    /// Call Blink() from the hazard-light animator or the car ending; it also steps by itself when unseen.
    /// </summary>
    public class TantalStalker : MonoBehaviour
    {
        [SerializeField] Transform cam;
        [SerializeField] Transform target;                 // usually Ali, or the car's windshield at the end
        [SerializeField] Renderer body;                    // tall black shape, no texture, no light
        [SerializeField] float startDistance = 30f;
        [SerializeField] float stepDistance = 4f;
        [SerializeField] float growPerStep = 0.12f;
        [SerializeField] float minDistance = 1.2f;
        [SerializeField] float unseenStepInterval = 6f;
        [SerializeField] AudioSource[] soundsToSteal;      // ordered: first to vanish ... last (Ali's breathing)
        [SerializeField] float fadeTime = 1.5f;

        public event System.Action OnReachedTarget;

        float distance, unseenTimer, baseHeight;
        int stolen;
        float[] baseVolumes;

        void OnEnable()
        {
            distance = startDistance;
            baseHeight = transform.localScale.y;
            stolen = 0;
            baseVolumes = new float[soundsToSteal == null ? 0 : soundsToSteal.Length];
            if (soundsToSteal != null)
                for (int i = 0; i < soundsToSteal.Length; i++) baseVolumes[i] = soundsToSteal[i] ? soundsToSteal[i].volume : 0f;
            Place();
        }

        void OnDisable()
        {
            // Give the world its sounds back when the hallucination ends.
            if (soundsToSteal == null || baseVolumes == null) return;
            for (int i = 0; i < soundsToSteal.Length && i < baseVolumes.Length; i++)
                if (soundsToSteal[i]) soundsToSteal[i].volume = baseVolumes[i];
        }

        void Update()
        {
            if (body == null || cam == null) return;
            if (!Seen()) { unseenTimer += Time.deltaTime; if (unseenTimer > unseenStepInterval) { unseenTimer = 0f; Step(); } }
            else unseenTimer = 0f;

            // Fade stolen sounds smoothly.
            for (int i = 0; soundsToSteal != null && i < soundsToSteal.Length; i++)
            {
                if (!soundsToSteal[i]) continue;
                float to = i < stolen ? 0f : baseVolumes[i];
                soundsToSteal[i].volume = Mathf.MoveTowards(soundsToSteal[i].volume, to, Time.deltaTime / fadeTime * Mathf.Max(0.01f, baseVolumes[i]));
            }
        }

        /// <summary>A dark frame (hazard lights off, flashlight dead): it may step even while looked at.</summary>
        public void Blink() => Step();

        void Step()
        {
            if (distance <= minDistance) return;
            distance = Mathf.Max(minDistance, distance - stepDistance);
            var s = transform.localScale; s.y += growPerStep * baseHeight; transform.localScale = s;
            if (soundsToSteal != null && stolen < soundsToSteal.Length) stolen++;
            Place();
            if (distance <= minDistance) OnReachedTarget?.Invoke();
        }

        void Place()
        {
            if (target == null) return;
            Vector3 dir = cam ? cam.forward : target.forward;
            dir.y = 0f; if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            Vector3 p = target.position + dir.normalized * distance;
            p.y = target.position.y;
            transform.position = p;
            transform.rotation = Quaternion.LookRotation(-dir.normalized);
        }

        bool Seen()
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(cam.GetComponent<Camera>());
            return GeometryUtility.TestPlanesAABB(planes, body.bounds);
        }
    }
}
