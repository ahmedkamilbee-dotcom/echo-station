using UnityEngine;

namespace EchoStation.Haunt
{
    /// <summary>A hanging lamp that swings as if someone just pushed it. Enable this object to start, disable to reset.</summary>
    public class SwingingLight : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float amplitude = 0.4f;
        [SerializeField] float speed = 1.7f;

        Vector3 rest; bool hasRest; float t;

        void OnEnable() { if (target) { rest = target.position; hasRest = true; } t = 0f; }
        void OnDisable() { if (target && hasRest) target.position = rest; }

        void Update()
        {
            if (!target) return;
            t += Time.deltaTime;
            float s = Mathf.Sin(t * speed * 2f);
            target.position = rest + Vector3.forward * (s * amplitude) + Vector3.down * (Mathf.Abs(s) * amplitude * 0.15f);
        }
    }
}
