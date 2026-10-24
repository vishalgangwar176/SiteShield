using System.Linq;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    [DisallowMultipleComponent]
    public sealed class InteractiveHoverFeedback : MonoBehaviour
    {
        [SerializeField, Range(1f, 1.12f)] float hoverScale = 1.035f;
        [SerializeField] Color highlightColor = new Color(0.08f, 0.9f, 0.78f, 1f);

        Renderer[] renderers;
        Vector3 restingScale;
        Vector3 targetScale;
        bool hovered;

        void Awake()
        {
            restingScale = transform.localScale;
            targetScale = restingScale;
            renderers = GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.GetComponent<TextMesh>() == null).ToArray();
        }

        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale,
                1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        }

        void OnDisable()
        {
            transform.localScale = restingScale;
            ApplyEmission(Color.black);
        }

        public void SetHovered(bool value)
        {
            if (hovered == value)
                return;
            hovered = value;
            targetScale = restingScale * (hovered ? hoverScale : 1f);
            ApplyEmission(hovered ? highlightColor * 0.75f : Color.black);
        }

        static MaterialPropertyBlock propBlock;
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        void ApplyEmission(Color color)
        {
            if (renderers == null)
                return;
            if (propBlock == null)
                propBlock = new MaterialPropertyBlock();

            foreach (var item in renderers)
            {
                if (item == null)
                    continue;

                var mats = item.sharedMaterials;
                if (mats != null)
                {
                    for (var i = 0; i < mats.Length; i++)
                    {
                        var mat = mats[i];
                        if (mat != null && mat.HasProperty(EmissionColorId) && !mat.IsKeywordEnabled("_EMISSION"))
                        {
                            mat.EnableKeyword("_EMISSION");
                        }
                    }
                }

                item.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorId, color);
                item.SetPropertyBlock(propBlock);
            }
        }
    }
}
