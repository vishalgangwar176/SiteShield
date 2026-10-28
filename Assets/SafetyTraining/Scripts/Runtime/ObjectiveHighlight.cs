using UnityEngine;

namespace SafetyTraining.Runtime
{
    public class ObjectiveHighlight : MonoBehaviour
    {
        public Renderer target;
        public Color color = new Color(0.18f, 0.49f, 1f);
        Material m;

        void OnEnable()
        {
            ResolveMaterial();
            if (m) m.EnableKeyword("_EMISSION");
        }

        void Update()
        {
            if (!m) ResolveMaterial();
            if (!m) return;
            float k = 1.2f + Mathf.Sin(Time.time * 3f) * 0.8f;
            m.SetColor("_EmissionColor", color * k);
        }

        void OnDisable()
        {
            if (m) m.SetColor("_EmissionColor", Color.black);
        }

        void ResolveMaterial()
        {
            if (target == null)
            {
                var renderers = GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r.enabled && r.GetComponent<TextMesh>() == null)
                    {
                        target = r;
                        break;
                    }
                }
            }
            if (target != null)
            {
                m = target.material;
                m.EnableKeyword("_EMISSION");
            }
        }
    }
}
