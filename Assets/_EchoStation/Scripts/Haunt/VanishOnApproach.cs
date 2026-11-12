using UnityEngine;

namespace EchoStation.Haunt
{
    /// <summary>The figure you see from far away is never there when you arrive.</summary>
    public class VanishOnApproach : MonoBehaviour
    {
        [SerializeField] float distance = 6f;

        void Update()
        {
            var cam = Camera.main;
            if (cam != null && Vector3.Distance(cam.transform.position, transform.position) < distance)
                gameObject.SetActive(false);
        }
    }
}
