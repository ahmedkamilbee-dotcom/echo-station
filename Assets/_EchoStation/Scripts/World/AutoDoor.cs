using UnityEngine;

namespace EchoStation.World
{
    /// <summary>
    /// A hinged door that opens when Ali comes to it from the front, and shuts behind him once he is through.
    /// Put it on an empty "hinge" object at the door's edge, with the door mesh as a child.
    /// It only opens again after Ali is back on the front side (which, in the loop, happens by itself).
    /// </summary>
    public class AutoDoor : MonoBehaviour
    {
        [SerializeField] Vector3 passDirection = Vector3.forward;   // world direction of travel through the door
        [SerializeField] float openDistance = 2.2f;
        [SerializeField] float closeDepth = 1.3f;                   // how far past the door before it shuts
        [SerializeField] float openAngle = 95f;
        [SerializeField] float openSpeed = 110f, closeSpeed = 420f;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip openClip, slamClip;

        public bool Locked;

        Quaternion rest;
        float angle;
        bool passed, wanted;

        void Awake() { rest = transform.localRotation; }

        void Update()
        {
            var cam = Camera.main; if (cam == null) return;
            Vector3 rel = cam.transform.position - transform.position; rel.y = 0f;
            float side = Vector3.Dot(rel, passDirection.normalized);
            if (side < 0f) passed = false;
            else if (side > closeDepth) passed = true;

            bool want = !Locked && !passed && rel.magnitude < openDistance;
            if (want && !wanted && audioSource && openClip) audioSource.PlayOneShot(openClip, 0.5f);
            wanted = want;

            float before = angle;
            angle = Mathf.MoveTowards(angle, want ? openAngle : 0f, (want ? openSpeed : closeSpeed) * Time.deltaTime);
            if (Mathf.Abs(before) > 0.01f && Mathf.Abs(angle) <= 0.01f && audioSource && slamClip) audioSource.PlayOneShot(slamClip, 0.55f);
            transform.localRotation = rest * Quaternion.Euler(0f, angle, 0f);
        }
    }
}
