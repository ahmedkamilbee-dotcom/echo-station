using System.Collections.Generic;
using UnityEngine;

namespace EchoStation.Loop
{
    /// <summary>
    /// The ordered sequence of corridor passes (see docs/demo-design.md, "the looping corridor"):
    /// 0, 1..6, 6b, 7..9, empty, 10..15, spare. Each entry is one pass; its index is the "step".
    /// The corridor breaks after the last entry.
    ///
    /// Each anomaly is a group of GameObjects enabled (or disabled) only on its step.
    /// "Empty" steps (no anomaly) are allowed and important: they keep the player unsure.
    /// Hallucinations (Si'luwa, Tantal, Ali's mind) are moved to the Hallucination layer while active,
    /// so the echo pulse passes straight through them: no echo, no shadow, nothing.
    /// </summary>
    public class AnomalyDirector : MonoBehaviour
    {
        [System.Serializable]
        public class Anomaly
        {
            public string id = "A01";
            [TextArea] public string note;
            public GameObject[] enableOnStep;
            public GameObject[] disableOnStep;
            public bool requiresTurnBack;
            public bool isHallucination;
        }

        [SerializeField] List<Anomaly> sequence = new List<Anomaly>();
        [Tooltip("Layer excluded from EchoPulse.echoLayers. Create it in Project Settings > Tags and Layers.")]
        [SerializeField] string hallucinationLayer = "Hallucination";

        public int StepCount => sequence.Count;
        public bool CurrentRequiresTurnBack => current != null && current.requiresTurnBack;

        Anomaly current;
        readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();

        /// <summary>Walking forward is wrong only when the current anomaly asks you to turn back.</summary>
        public bool ForwardPassIsCorrect() => current == null || !current.requiresTurnBack;

        /// <summary>DemoClock: if the player is late, drop the "empty" and "spare" steps that are still ahead.</summary>
        public void RemoveEmptyStepsAfter(int step)
        {
            for (int i = sequence.Count - 1; i > step; i--)
                if (sequence[i].id == "empty" || sequence[i].id == "spare") sequence.RemoveAt(i);
        }

        public void ApplyStep(int step)
        {
            foreach (var a in sequence) Set(a, false);
            current = step >= 0 && step < sequence.Count ? sequence[step] : null;
            if (current != null) Set(current, true);
        }

        void Set(Anomaly a, bool on)
        {
            if (a.enableOnStep != null)
                foreach (var g in a.enableOnStep)
                {
                    if (!g) continue;
                    g.SetActive(on);
                    if (a.isHallucination) SetHallucinationLayer(g, on);
                }
            if (a.disableOnStep != null)
                foreach (var g in a.disableOnStep) if (g) g.SetActive(!on);
        }

        void SetHallucinationLayer(GameObject root, bool on)
        {
            int layer = LayerMask.NameToLayer(hallucinationLayer);
            if (layer < 0) { Debug.LogWarning($"[AnomalyDirector] Layer '{hallucinationLayer}' missing.", this); return; }
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var go = t.gameObject;
                if (on)
                {
                    if (!originalLayers.ContainsKey(go)) originalLayers[go] = go.layer;
                    go.layer = layer;
                }
                else if (originalLayers.TryGetValue(go, out int orig))
                {
                    go.layer = orig;
                }
            }
        }
    }
}
