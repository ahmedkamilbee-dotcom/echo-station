using UnityEngine;

namespace EchoStation.Haunt
{
    /// <summary>
    /// When Ali crosses this trigger, something stands right behind him and stays behind him as he walks.
    /// You hear it at your neck. If you turn around you catch it for a blink, then it is gone.
    /// It is a hallucination: put the figure on the Hallucination layer (no echo).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class BehindYou : MonoBehaviour
    {
        [SerializeField] GameObject figure;
        [SerializeField] float distance = 1.2f;
        [SerializeField] float glimpse = 0.3f;

        bool fired;
        float seen;
        Transform ali;

        void OnEnable() { fired = false; seen = 0f; if (figure) figure.SetActive(false); }

        void OnTriggerEnter(Collider other)
        {
            if (fired || figure == null || other.GetComponent<CharacterController>() == null) return;
            fired = true; ali = other.transform;
            Place();
            figure.SetActive(true);
        }

        void Place()
        {
            var cam = Camera.main; if (cam == null || ali == null) return;
            Vector3 back = -cam.transform.forward; back.y = 0f;
            if (back.sqrMagnitude < 0.001f) back = -ali.forward;
            back.Normalize();
            figure.transform.position = ali.position + back * distance + Vector3.up * (figure.transform.lossyScale.y * 0.5f);
            figure.transform.rotation = Quaternion.LookRotation(-back);
        }

        void Update()
        {
            if (!fired || figure == null || !figure.activeSelf) return;
            var cam = Camera.main; if (cam == null) return;
            Vector3 to = figure.transform.position - cam.transform.position; to.y = 0f;
            Vector3 fwd = cam.transform.forward; fwd.y = 0f;
            float dot = Vector3.Dot(fwd.normalized, to.normalized);
            if (dot > 0.6f)
            {
                seen += Time.deltaTime;
                if (seen >= glimpse) figure.SetActive(false);   // gone, once, for this loop
            }
            else if (dot < 0f) Place();   // still behind you: keep it at your neck
        }
    }
}
