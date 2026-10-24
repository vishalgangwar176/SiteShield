using SafetyTraining.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SafetyTraining.Editor
{
    static class ProfessionalChatBuilder
    {
        const string PanelPath = "Assets/ThirdParty/Kenney/UI-Pack-SciFi/PNG/Extra/Default/panel_glass_notches.png";
        const string FontPath = "Assets/ThirdParty/Kenney/UI-Pack-SciFi/Font/Kenney Future Narrow.ttf";
        const string MaterialPath = "Assets/SafetyTraining/GeneratedMaterials/Safety HUD UI.mat";

        static readonly Color Panel = new Color(0.063f, 0.11f, 0.153f, 0.72f);
        static readonly Color Field = new Color(0.063f, 0.11f, 0.153f, 1f);
        static readonly Color Primary = new Color(0.957f, 0.973f, 0.984f);
        static readonly Color Muted = new Color(0.624f, 0.69f, 0.749f);
        static readonly Color Accent = new Color(0.216f, 0.839f, 0.753f);

        public static void Create(Camera viewer)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPath);
            var displayFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            var eventSystemObject = new GameObject("UI Event System");
            var eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = true;
            var inputModule = eventSystemObject.AddComponent<XRUIInputModule>();
            inputModule.enableMouseInput = true;
            inputModule.enableTouchInput = true;
            inputModule.enableXRInput = true;

            var canvasObject = new GameObject("NPC Chat Canvas");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200f, 700f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 300f);

            var panel = Image("NPC Chat Panel", canvasObject.transform, sprite, material, Panel,
                new Vector2(24f, 24f), new Vector2(520f, 300f), Vector2.zero);
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.035f, 0.055f, 0.7f);
            shadow.effectDistance = new Vector2(10f, -12f);

            var title = Text("Coach Title", panel.transform, "SAFETY COACH", displayFont, 22, Accent,
                new Vector2(28f, -22f), new Vector2(350f, 34f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 16;
            title.resizeTextMaxSize = 22;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text("Coach Status", panel.transform, "LIVE  /  ASK ABOUT THE CONDITION, RISK, OR CONTROL", displayFont,
                13, Muted, new Vector2(28f, -54f), new Vector2(454f, 20f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f));
            Image("Chat Accent", panel.transform, null, material, Accent,
                new Vector2(28f, -80f), new Vector2(464f, 3f), new Vector2(0f, 1f));

            var transcript = Text("Coach Response", panel.transform,
                "Ask the mentor about visible hazards, controls, or your progress.", bodyFont, 22, Primary,
                new Vector2(28f, -96f), new Vector2(464f, 108f), TextAnchor.UpperLeft,
                new Vector2(0f, 1f));
            transcript.lineSpacing = 1.08f;
            transcript.resizeTextForBestFit = true;
            transcript.resizeTextMinSize = 15;
            transcript.resizeTextMaxSize = 22;

            var inputObject = new GameObject("Chat Input");
            inputObject.transform.SetParent(panel.transform, false);
            var inputRect = inputObject.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.zero;
            inputRect.pivot = Vector2.zero;
            inputRect.anchoredPosition = new Vector2(28f, 24f);
            inputRect.sizeDelta = new Vector2(340f, 50f);
            var inputBackground = inputObject.AddComponent<Image>();
            inputBackground.sprite = sprite;
            inputBackground.material = material;
            inputBackground.type = UnityEngine.UI.Image.Type.Sliced;
            inputBackground.color = Field;
            var input = inputObject.AddComponent<InputField>();
            input.targetGraphic = inputBackground;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 240;
            input.caretColor = Accent;
            input.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);

            var inputText = StretchText("Input Text", inputObject.transform, bodyFont, 18, Primary,
                new Vector2(14f, 8f), new Vector2(-14f, -8f));
            var placeholder = StretchText("Input Placeholder", inputObject.transform, bodyFont, 17, Muted,
                new Vector2(14f, 8f), new Vector2(-14f, -8f));
            placeholder.text = "Type a question, then press Enter";
            placeholder.fontStyle = FontStyle.Italic;
            input.textComponent = inputText;
            input.placeholder = placeholder;

            var buttonObject = new GameObject("Send Button");
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.zero;
            buttonRect.pivot = Vector2.zero;
            buttonRect.anchoredPosition = new Vector2(382f, 24f);
            buttonRect.sizeDelta = new Vector2(110f, 50f);
            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.sprite = sprite;
            buttonImage.material = material;
            buttonImage.type = UnityEngine.UI.Image.Type.Sliced;
            buttonImage.color = Accent;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.3f, 0.95f, 0.85f);
            colors.pressedColor = new Color(0.14f, 0.62f, 0.56f);
            button.colors = colors;
            var send = Text("Send Label", buttonObject.transform, "SEND", displayFont, 18,
                new Color(0.02f, 0.07f, 0.09f), Vector2.zero, new Vector2(110f, 50f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var closeObject = new GameObject("Close Button");
            closeObject.transform.SetParent(panel.transform, false);
            var closeRect = closeObject.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0f, 1f);
            closeRect.anchorMax = new Vector2(0f, 1f);
            closeRect.pivot = new Vector2(0f, 1f);
            closeRect.anchoredPosition = new Vector2(392f, -18f);
            closeRect.sizeDelta = new Vector2(100f, 34f);
            var closeImage = closeObject.AddComponent<Image>();
            closeImage.sprite = sprite;
            closeImage.material = material;
            closeImage.type = UnityEngine.UI.Image.Type.Sliced;
            closeImage.color = Field;
            var close = closeObject.AddComponent<Button>();
            close.targetGraphic = closeImage;
            var closeColors = close.colors;
            closeColors.highlightedColor = new Color(0.14f, 0.24f, 0.3f);
            closeColors.pressedColor = new Color(0.08f, 0.15f, 0.2f);
            close.colors = closeColors;
            Text("Close Label", closeObject.transform, "ESC  CLOSE", displayFont, 14, Primary,
                Vector2.zero, new Vector2(100f, 34f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var chat = canvasObject.AddComponent<NpcChatPanel>();
            chat.Configure(new NpcChatPanel.Bindings
            {
                Panel = panel.gameObject,
                Title = title,
                Transcript = transcript,
                Input = input,
                SendLabel = send,
                SendButton = button,
                CloseButton = close
            });
            panel.gameObject.SetActive(false);
        }

        static Image Image(string name, Transform parent, Sprite sprite, Material material, Color color,
            Vector2 position, Vector2 size, Vector2 anchor)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            var rect = item.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = item.AddComponent<Image>();
            image.sprite = sprite;
            image.material = material;
            image.color = color;
            return image;
        }

        static Text Text(string name, Transform parent, string value, Font font, int size, Color color,
            Vector2 position, Vector2 dimensions, TextAnchor alignment, Vector2 anchor)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            var rect = item.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = item.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static Text StretchText(string name, Transform parent, Font font, int size, Color color,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var text = Text(name, parent, string.Empty, font, size, color, Vector2.zero, Vector2.zero,
                TextAnchor.MiddleLeft, Vector2.zero);
            var rect = text.rectTransform;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return text;
        }
    }
}
