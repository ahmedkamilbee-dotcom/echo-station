using UnityEngine;

namespace EchoStation.Echo
{
    public enum EchoMaterial { Concrete, Metal, Reed, Water, Body, Glass, Tape }

    /// <summary>
    /// Put this on anything the echo pulse can "hear". It decides which click plays back.
    /// Tick "Is Hallucination" on the Si'luwa, the Tantal and the drowned rows:
    /// they return no echo at all. That is the fair test the player learns by ear.
    /// The killer is real: give him Material = Body (a dull thud).
    /// </summary>
    public class EchoSurface : MonoBehaviour
    {
        [SerializeField] EchoMaterial material = EchoMaterial.Concrete;
        [SerializeField] AudioClip clip;
        [SerializeField] bool isHallucination;

        public EchoMaterial Material => material;
        public AudioClip Clip => clip;
        public bool IsHallucination { get => isHallucination; set => isHallucination = value; }
    }
}
