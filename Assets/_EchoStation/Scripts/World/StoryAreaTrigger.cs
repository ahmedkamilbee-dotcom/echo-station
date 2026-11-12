using UnityEngine;
using EchoStation.Player;

namespace EchoStation.World
{
    [RequireComponent(typeof(Collider))]
    public sealed class StoryAreaTrigger : MonoBehaviour
    {
        public enum EventType { PumpHall, LeaveStation, ReachCar }

        [SerializeField] StationStoryDirector story;
        [SerializeField] EventType eventType;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var ali = other.GetComponent<AliController>();
            if (!ali || !story) return;

            switch (eventType)
            {
                case EventType.PumpHall: story.EnterPumpHall(ali); break;
                case EventType.LeaveStation: story.LeaveStation(ali); break;
                case EventType.ReachCar: story.ReachCar(); break;
            }
        }
    }
}
