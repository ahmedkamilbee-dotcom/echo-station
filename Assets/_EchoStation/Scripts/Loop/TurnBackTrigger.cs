using UnityEngine;
using EchoStation.Player;

namespace EchoStation.Loop
{
    /// <summary>
    /// Put on a trigger just inside door A. If the current anomaly requires turning back,
    /// walking back through here counts as a correct pass.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class TurnBackTrigger : MonoBehaviour
    {
        [SerializeField] LoopCorridor corridor;

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (corridor != null && other.GetComponent<AliController>() != null) corridor.TurnBackPass();
        }
    }
}
