using SafetyTraining.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Editor
{
    static class ProfessionalSpeechBubbleBuilder
    {
        const string PanelPath = "Assets/ThirdParty/Kenney/UI-Pack-SciFi/PNG/Extra/Default/panel_glass_notches.png";
        const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string MaterialPath = "Assets/SafetyTraining/GeneratedMaterials/Safety HUD UI.mat";

        static readonly Color Panel = new(0.035f, 0.065f, 0.095f, 0.96f);
        static readonly Color Primary = new(0.96f, 0.98f, 1f, 1f);
        static readonly Color Muted = new(0.64f, 0.72f, 0.78f, 1f);
        static readonly Color Accent = new(0.216f, 0.839f, 0.753f, 1f);

        public static NpcSpeechBubbleView Create(Transform parent)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            var bubble = new GameObject("NPC Speech Bubble", typeof(RectTransform));
            bubble.transform.SetParent(parent, false);
            bubble.transform.localPosition = new Vector3(0.66f, 2.17f, -0.08f);
            bubble.transform.localScale = Vector3.one * 0.0036f;
            bubble.AddComponent<BillboardLabel>();
            var canvas = bubble.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 250;
            var scaler = bubble.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 48f;
            scaler.referencePixelsPerUnit = 100f;
            var group = bubble.AddComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var bubbleRect = bubble.GetComponent<RectTransform>();
            bubbleRect.sizeDelta = new Vector2(500f, 230f);
            bubbleRect.pivot = new Vector2(0.5f, 0f);

            var tail = Image("Speech Bubble Tail", bubble.transform, sprite, material, Panel,
                new Vector2(0f, -12f), new Vector2(36f, 36f), new Vector2(0.5f, 0f));
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var panel = Image("Dialogue Backplate", bubble.transform, sprite, material, Panel,
                Vector2.zero, new Vector2(500f, 230f), new Vector2(0.5f, 0.5f));
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.015f, 0.025f, 0.72f);
            shadow.effectDistance = new Vector2(8f, -10f);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.5f);
            outline.effectDistance = new Vector2(2f, -2f);

            Image("Coach Accent", panel.transform, null, material, Accent,
                new Vector2(18f, -20f), new Vector2(6f, 178f), new Vector2(0f, 1f));
            var eyebrow = Text("Coach Eyebrow", panel.transform, "SAFETY COACH", font, 23, Accent,
                new Vector2(38f, -20f), new Vector2(250f, 30f), TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
            eyebrow.fontStyle = FontStyles.Bold;
            var status = Text("Coach Status", panel.transform, "LIVE  /  GUIDANCE", font, 15, Muted,
                new Vector2(-26f, -22f), new Vector2(160f, 24f), TextAlignmentOptions.TopRight, new Vector2(1f, 1f));
            status.fontStyle = FontStyles.Bold;
            Image("Reply Divider", panel.transform, null, material, new Color(Accent.r, Accent.g, Accent.b, 0.48f),
                new Vector2(38f, -58f), new Vector2(414f, 3f), new Vector2(0f, 1f));

            var reply = Text("Coach Reply", panel.transform,
                "Ask me about the condition, risk, or safest control.", font, 42, Primary,
                new Vector2(38f, -72f), new Vector2(434f, 132f), TextAlignmentOptions.TopLeft, new Vector2(0f, 1f));
            reply.enableAutoSizing = true;
            reply.fontSizeMin = 36f;
            reply.fontSizeMax = 42f;
            reply.lineSpacing = 1f;
            reply.outlineColor = new Color32(0, 0, 0, 235);
            reply.outlineWidth = 0.12f;
            var page = Text("Page Indicator", panel.transform, string.Empty, font, 16, Muted,
                new Vector2(-26f, 14f), new Vector2(90f, 20f), TextAlignmentOptions.BottomRight, new Vector2(1f, 0f));

            var view = bubble.AddComponent<NpcSpeechBubbleView>();
            view.Configure(reply, page, group);
            bubble.SetActive(false);
            return view;
        }

        static Image Image(string name, Transform parent, Sprite sprite, Material material, Color color,
            Vector2 position, Vector2 size, Vector2 anchor)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = item.AddComponent<Image>();
            image.sprite = sprite;
            image.material = material;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI Text(string name, Transform parent, string value, TMP_FontAsset font, int size, Color color,
            Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment, Vector2 anchor)
        {
            var item = new GameObject(name, typeof(RectTransform));
            item.transform.SetParent(parent, false);
            var rect = item.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = item.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.extraPadding = true;
            text.raycastTarget = false;
            return text;
        }
    }
}
