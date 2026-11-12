using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.World;

namespace EchoStation.Puzzles
{
    /// <summary>A military radio, knob 1..6 (E cycles). Channel 4 plays the corridor itself, delayed: secret 3.</summary>
    public class RadioChannelPuzzle : MonoBehaviour, IInteractable
    {
        [SerializeField] AudioSource radio;
        [SerializeField] AudioClip staticClip, channelFour, knobClick;
        [SerializeField] Transform knob;
        public bool Focused { get; set; }
        int channel = 1;
        bool dead;

        void OnEnable() { GeneratorPower.OnPowerChanged += Power; }
        void OnDisable() { GeneratorPower.OnPowerChanged -= Power; }
        void Power(bool on) { if (!on) { dead = true; if (radio) radio.Stop(); } }   // dies forever at the blackout

        public void Interact()
        {
            if (dead || !radio) return;
            channel = channel % 6 + 1;
            radio.PlayOneShot(knobClick);
            if (knob) knob.localRotation = Quaternion.Euler(0, 0, -channel * 45f);
            radio.clip = channel == 4 ? channelFour : staticClip;
            radio.loop = channel != 4;
            radio.Play();
            if (channel == 4) SecretTracker.Found(3);
        }
    }
}
