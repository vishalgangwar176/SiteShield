using UnityEngine;

namespace SafetyTraining.Runtime
{
    public class NpcSpeakGlow : MonoBehaviour
    {
        public Renderer[] parts;
        public Color glow = new Color(0.13f, 0.83f, 0.77f);
        public float intensity = 0.15f;
        public bool speaking;
        float cur;

        void Update()
        {
            cur = Mathf.MoveTowards(cur, speaking ? intensity : 0f, Time.deltaTime * 2f);
            if (parts == null)
                return;

            foreach (var r in parts)
            {
                if (r == null)
                    continue;
                var m = r.material;
                if (m == null)
                    continue;
                if (cur > 0.001f)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", glow * cur);
                }
                else
                {
                    m.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        public void SetSpeaking(bool isSpeaking)
        {
            speaking = isSpeaking;
        }
    }
}
