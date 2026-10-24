using System.Collections.Generic;
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
        static readonly Color Accent = new Color(0.216f, 0.839f, 0.753f); // Teal
        static readonly Color Warning = new Color(1.0f, 0.761f, 0.102f); // Amber
        static readonly Color Success = new Color(0.180f, 0.800f, 0.443f); // Green

        public static void Create(Camera viewer)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPath);
            var displayFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("UI Event System");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
                eventSystem.sendNavigationEvents = true;
                var inputModule = eventSystemObject.AddComponent<XRUIInputModule>();
                inputModule.enableMouseInput = true;
                inputModule.enableTouchInput = true;
                inputModule.enableXRInput = true;
            }

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
            canvasObject.GetComponent<RectTransform>().sizeDelta = new Vector2(1200f, 700f);

            // Chat Main Panel (Bottom-Left sidecar placement)
            var panel = Image("NPC Chat Panel", canvasObject.transform, sprite, material, Panel,
                new Vector2(24f, 24f), new Vector2(520f, 310f), Vector2.zero);
            panel.type = UnityEngine.UI.Image.Type.Sliced;
            var shadow = panel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.035f, 0.055f, 0.8f);
            shadow.effectDistance = new Vector2(10f, -12f);

            var title = Text("Coach Title", panel.transform, "SAFETY COACH", displayFont, 22, Accent,
                new Vector2(28f, -20f), new Vector2(200f, 32f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 16;
            title.resizeTextMaxSize = 22;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            Text("Coach Status", panel.transform, "LIVE MULTI-TURN AI COACHING  /  OSHA & NFPA VERIFIED", displayFont,
                11, Muted, new Vector2(28f, -48f), new Vector2(360f, 18f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            Image("Chat Accent", panel.transform, null, material, Accent,
                new Vector2(28f, -68f), new Vector2(464f, 2f), new Vector2(0f, 1f));

            // Top action buttons: [CONFIG AI], [CLEAR], [ESC CLOSE]
            var settingsBtn = CreateButton("AI Settings Button", panel.transform, sprite, material, Field,
                new Vector2(245f, -16f), new Vector2(85f, 28f), new Vector2(0f, 1f));
            Text("Settings Label", settingsBtn.transform, "⚙ CONFIG", displayFont, 11, Accent,
                Vector2.zero, new Vector2(85f, 28f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var clearBtn = CreateButton("Clear Button", panel.transform, sprite, material, Field,
                new Vector2(335f, -16f), new Vector2(75f, 28f), new Vector2(0f, 1f));
            Text("Clear Label", clearBtn.transform, "CLEAR", displayFont, 11, Muted,
                Vector2.zero, new Vector2(75f, 28f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var closeBtn = CreateButton("Close Button", panel.transform, sprite, material, Field,
                new Vector2(415f, -16f), new Vector2(75f, 28f), new Vector2(0f, 1f));
            Text("Close Label", closeBtn.transform, "ESC ✕", displayFont, 12, Primary,
                Vector2.zero, new Vector2(75f, 28f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            // Transcript Scroll View / Text Area
            var transcript = Text("Coach Response", panel.transform,
                "Ask the coach about hazards, controls, or procedure steps.", bodyFont, 16, Primary,
                new Vector2(28f, -76f), new Vector2(464f, 145f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            transcript.lineSpacing = 1.15f;
            transcript.supportRichText = true;
            transcript.resizeTextForBestFit = true;
            transcript.resizeTextMinSize = 15;
            transcript.resizeTextMaxSize = 22;
            transcript.verticalOverflow = VerticalWrapMode.Truncate;

            // Loading Indicator
            var loadingObj = new GameObject("Chat Loading Indicator");
            loadingObj.transform.SetParent(panel.transform, false);
            var loadingRect = loadingObj.AddComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(0f, 0f);
            loadingRect.anchorMax = new Vector2(0f, 0f);
            loadingRect.pivot = new Vector2(0f, 0f);
            loadingRect.anchoredPosition = new Vector2(28f, 72f);
            loadingRect.sizeDelta = new Vector2(300f, 18f);
            var loadingText = loadingObj.AddComponent<Text>();
            loadingText.font = bodyFont;
            loadingText.fontSize = 12;
            loadingText.fontStyle = FontStyle.Italic;
            loadingText.color = Accent;
            loadingText.text = "Coach is analyzing site data... ⏳";
            loadingObj.SetActive(false);

            // Input Field
            var inputObject = new GameObject("Chat Input");
            inputObject.transform.SetParent(panel.transform, false);
            var inputRect = inputObject.AddComponent<RectTransform>();
            inputRect.anchorMin = Vector2.zero;
            inputRect.anchorMax = Vector2.zero;
            inputRect.pivot = Vector2.zero;
            inputRect.anchoredPosition = new Vector2(28f, 18f);
            inputRect.sizeDelta = new Vector2(344f, 48f);
            var inputBackground = inputObject.AddComponent<Image>();
            inputBackground.sprite = sprite;
            inputBackground.material = material;
            inputBackground.type = UnityEngine.UI.Image.Type.Sliced;
            inputBackground.color = Field;
            var input = inputObject.AddComponent<InputField>();
            input.targetGraphic = inputBackground;
            input.lineType = InputField.LineType.SingleLine;
            input.characterLimit = 300;
            input.caretColor = Accent;
            input.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);

            var inputText = StretchText("Input Text", inputObject.transform, bodyFont, 16, Primary,
                new Vector2(12f, 6f), new Vector2(-12f, -6f));
            var placeholder = StretchText("Input Placeholder", inputObject.transform, bodyFont, 15, Muted,
                new Vector2(12f, 6f), new Vector2(-12f, -6f));
            placeholder.text = "Type your question and press Enter...";
            placeholder.fontStyle = FontStyle.Italic;
            input.textComponent = inputText;
            input.placeholder = placeholder;

            // Send Button
            var buttonObject = new GameObject("Send Button");
            buttonObject.transform.SetParent(panel.transform, false);
            var buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.zero;
            buttonRect.pivot = Vector2.zero;
            buttonRect.anchoredPosition = new Vector2(382f, 18f);
            buttonRect.sizeDelta = new Vector2(110f, 48f);
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
            var send = Text("Send Label", buttonObject.transform, "SEND", displayFont, 16,
                new Color(0.02f, 0.07f, 0.09f), Vector2.zero, new Vector2(110f, 48f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var chat = canvasObject.AddComponent<NpcChatPanel>();
            chat.Configure(new NpcChatPanel.Bindings
            {
                Panel = panel.gameObject,
                Title = title,
                Transcript = transcript,
                Input = input,
                SendLabel = send,
                SendButton = button,
                CloseButton = closeBtn,
                ClearButton = clearBtn,
                SettingsButton = settingsBtn,
                LoadingIndicator = loadingObj
            });
            panel.gameObject.SetActive(false);

            // AI Settings Modal
            CreateAiSettingsModal(canvasObject.transform, sprite, material, displayFont, bodyFont);

            // Module Completion Summary Modal
            CreateModuleCompletionModal(canvasObject.transform, sprite, material, displayFont, bodyFont);
        }

        static void CreateAiSettingsModal(Transform canvas, Sprite sprite, Material material,
            Font displayFont, Font bodyFont)
        {
            var modal = Image("AI Settings Modal", canvas, sprite, material,
                new Color(0.05f, 0.09f, 0.13f, 0.97f), Vector2.zero, new Vector2(560f, 450f),
                new Vector2(0.5f, 0.5f));
            modal.type = UnityEngine.UI.Image.Type.Sliced;
            var shadow = modal.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.02f, 0.04f, 0.9f);
            shadow.effectDistance = new Vector2(12f, -14f);

            Text("Modal Header", modal.transform, "AI SAFETY COACH CONFIGURATION", displayFont, 18, Accent,
                new Vector2(28f, -20f), new Vector2(400f, 28f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            Text("Modal Subheader", modal.transform, "CONFIGURE LIVE AI PROVIDER (OPENAI / GEMINI / OLLAMA / PROXY)",
                displayFont, 10, Muted, new Vector2(28f, -46f), new Vector2(500f, 18f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            Image("Modal Divider", modal.transform, null, material, Accent,
                new Vector2(28f, -68f), new Vector2(504f, 2f), new Vector2(0f, 1f));

            // Provider Dropdown
            Text("Provider Label", modal.transform, "AI PROVIDER", displayFont, 12, Primary,
                new Vector2(28f, -80f), new Vector2(150f, 20f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            var dropdownObj = new GameObject("Provider Dropdown");
            dropdownObj.transform.SetParent(modal.transform, false);
            var dropRect = dropdownObj.AddComponent<RectTransform>();
            dropRect.anchorMin = new Vector2(0f, 1f);
            dropRect.anchorMax = new Vector2(0f, 1f);
            dropRect.pivot = new Vector2(0f, 1f);
            dropRect.anchoredPosition = new Vector2(28f, -102f);
            dropRect.sizeDelta = new Vector2(504f, 36f);
            var dropImg = dropdownObj.AddComponent<Image>();
            dropImg.sprite = sprite;
            dropImg.material = material;
            dropImg.type = UnityEngine.UI.Image.Type.Sliced;
            dropImg.color = Field;
            var dropdown = dropdownObj.AddComponent<Dropdown>();
            dropdown.targetGraphic = dropImg;

            var dropLabel = StretchText("Dropdown Label", dropdownObj.transform, bodyFont, 14, Primary,
                new Vector2(12f, 4f), new Vector2(-30f, -4f));
            dropdown.captionText = dropLabel;

            dropdown.options = new List<Dropdown.OptionData>
            {
                new Dropdown.OptionData("OpenAI-Compatible (/v1/chat/completions)"),
                new Dropdown.OptionData("Google Gemini (generativelanguage.googleapis.com)"),
                new Dropdown.OptionData("Ollama Local (localhost:11434)"),
                new Dropdown.OptionData("Custom Enterprise Proxy")
            };

            // Endpoint Input
            Text("Endpoint Label", modal.transform, "ENDPOINT URL", displayFont, 12, Primary,
                new Vector2(28f, -146f), new Vector2(150f, 20f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            var endpointInput = CreateInputField("Endpoint Input", modal.transform, sprite, material, bodyFont,
                new Vector2(28f, -168f), new Vector2(504f, 34f), "https://generativelanguage.googleapis.com");

            // Model Input
            Text("Model Label", modal.transform, "MODEL NAME", displayFont, 12, Primary,
                new Vector2(28f, -208f), new Vector2(150f, 20f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            var modelInput = CreateInputField("Model Input", modal.transform, sprite, material, bodyFont,
                new Vector2(28f, -230f), new Vector2(504f, 34f), "gemini-1.5-flash");

            // API Key Input
            Text("ApiKey Label", modal.transform, "API KEY (ENCRYPTED LOCALLY)", displayFont, 12, Primary,
                new Vector2(28f, -270f), new Vector2(300f, 20f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            var apiKeyInput = CreateInputField("ApiKey Input", modal.transform, sprite, material, bodyFont,
                new Vector2(28f, -292f), new Vector2(504f, 34f), "Enter API Key (saved securely in PlayerPrefs)");
            apiKeyInput.inputType = InputField.InputType.Password;

            // AI Enabled Toggle
            var toggleObj = new GameObject("Enabled Toggle");
            toggleObj.transform.SetParent(modal.transform, false);
            var toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0f, 1f);
            toggleRect.anchorMax = new Vector2(0f, 1f);
            toggleRect.pivot = new Vector2(0f, 1f);
            toggleRect.anchoredPosition = new Vector2(28f, -332f);
            toggleRect.sizeDelta = new Vector2(200f, 24f);
            var toggle = toggleObj.AddComponent<Toggle>();
            var toggleBg = Image("Toggle Background", toggleObj.transform, sprite, material, Field,
                new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(0f, 0.5f));
            var toggleCheck = Image("Toggle Checkmark", toggleBg.transform, null, material, Accent,
                new Vector2(4f, 4f), new Vector2(16f, 16f), new Vector2(0f, 0f));
            toggle.graphic = toggleCheck;
            toggle.targetGraphic = toggleBg;
            toggle.isOn = true;
            Text("Toggle Label", toggleObj.transform, "ENABLE AI COACHING", displayFont, 12, Primary,
                new Vector2(32f, 0f), new Vector2(180f, 22f), TextAnchor.MiddleLeft, new Vector2(0f, 0.5f));

            // Status Indicator & Message
            var statusDot = Image("Status Dot", modal.transform, null, material,
                new Color(0.5f, 0.55f, 0.6f), new Vector2(28f, -366f), new Vector2(10f, 10f), new Vector2(0f, 1f));
            var statusLabel = Text("Status Text", modal.transform, "Status: Not tested yet.", bodyFont, 12, Muted,
                new Vector2(46f, -362f), new Vector2(480f, 20f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            // Bottom action buttons: [TEST CONNECTION], [SAVE CONFIG], [CANCEL / CLOSE]
            var testBtn = CreateButton("Test Button", modal.transform, sprite, material, Field,
                new Vector2(28f, 18f), new Vector2(165f, 42f), new Vector2(0f, 0f));
            var testLabel = Text("Test Label", testBtn.transform, "TEST CONNECTION", displayFont, 12, Accent,
                Vector2.zero, new Vector2(165f, 42f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var saveBtn = CreateButton("Save Button", modal.transform, sprite, material, Accent,
                new Vector2(205f, 18f), new Vector2(150f, 42f), new Vector2(0f, 0f));
            Text("Save Label", saveBtn.transform, "SAVE CONFIG", displayFont, 12, new Color(0.02f, 0.07f, 0.09f),
                Vector2.zero, new Vector2(150f, 42f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var closeDialogBtn = CreateButton("Close Dialog Button", modal.transform, sprite, material, Field,
                new Vector2(365f, 18f), new Vector2(167f, 42f), new Vector2(0f, 0f));
            Text("Close Dialog Label", closeDialogBtn.transform, "CLOSE", displayFont, 12, Primary,
                Vector2.zero, new Vector2(167f, 42f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var dialogComponent = modal.gameObject.AddComponent<SafetyAiSettingsDialog>();
            dialogComponent.Configure(new SafetyAiSettingsDialog.Bindings
            {
                ModalPanel = modal.gameObject,
                ProviderDropdown = dropdown,
                EndpointInput = endpointInput,
                ModelInput = modelInput,
                ApiKeyInput = apiKeyInput,
                EnabledToggle = toggle,
                StatusLabel = statusLabel,
                StatusIndicatorDot = statusDot,
                TestButton = testBtn,
                TestButtonLabel = testLabel,
                SaveButton = saveBtn,
                CloseButton = closeDialogBtn
            });
            modal.gameObject.SetActive(false);
        }

        static void CreateModuleCompletionModal(Transform canvas, Sprite sprite, Material material,
            Font displayFont, Font bodyFont)
        {
            var modal = Image("Module Completion Modal", canvas, sprite, material,
                new Color(0.043f, 0.086f, 0.141f, 0.98f), Vector2.zero, new Vector2(600f, 380f),
                new Vector2(0.5f, 0.5f));
            modal.type = UnityEngine.UI.Image.Type.Sliced;
            var shadow = modal.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.02f, 0.04f, 0.9f);
            shadow.effectDistance = new Vector2(12f, -14f);

            Text("Summary Tag", modal.transform, "SITESHIELD TRAINING ASSESSMENT", displayFont, 13, Accent,
                new Vector2(32f, -24f), new Vector2(536f, 20f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            var moduleTitle = Text("Summary Module Title", modal.transform, "CONSTRUCTION FALL PROTECTION",
                displayFont, 24, Primary, new Vector2(32f, -48f), new Vector2(536f, 32f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            Image("Summary Divider", modal.transform, null, material, Accent,
                new Vector2(32f, -84f), new Vector2(536f, 2f), new Vector2(0f, 1f));

            var ratingLabel = Text("Summary Rating", modal.transform, "EXEMPLARY (PASS)", displayFont, 20, Success,
                new Vector2(32f, -96f), new Vector2(536f, 26f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            var scoreLabel = Text("Summary Score", modal.transform, "SCORE: 0320 PTS", displayFont, 15, Primary,
                new Vector2(32f, -130f), new Vector2(250f, 22f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            var hazardsLabel = Text("Summary Hazards", modal.transform, "HAZARDS: 2/2 FOUND", displayFont, 15, Primary,
                new Vector2(300f, -130f), new Vector2(250f, 22f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            var tasksLabel = Text("Summary Tasks", modal.transform, "CONTROLS: 5/5 VERIFIED", displayFont, 15, Primary,
                new Vector2(32f, -158f), new Vector2(536f, 22f), TextAnchor.UpperLeft, new Vector2(0f, 1f));

            var summaryText = Text("Summary Text", modal.transform,
                "All identified hazard conditions neutralized according to OSHA and site safety standards.",
                bodyFont, 15, Muted, new Vector2(32f, -190f), new Vector2(536f, 80f), TextAnchor.UpperLeft, new Vector2(0f, 1f));
            summaryText.lineSpacing = 1.15f;

            var returnBtn = CreateButton("Return Campus Button", modal.transform, sprite, material, Accent,
                new Vector2(32f, 22f), new Vector2(250f, 48f), new Vector2(0f, 0f));
            Text("Return Label", returnBtn.transform, "RETURN TO CAMPUS", displayFont, 14,
                new Color(0.02f, 0.07f, 0.09f), Vector2.zero, new Vector2(250f, 48f),
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var continueBtn = CreateButton("Continue Button", modal.transform, sprite, material, Field,
                new Vector2(318f, 22f), new Vector2(250f, 48f), new Vector2(0f, 0f));
            Text("Continue Label", continueBtn.transform, "CONTINUE REVIEW", displayFont, 14, Primary,
                Vector2.zero, new Vector2(250f, 48f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

            var dialog = modal.gameObject.AddComponent<ModuleCompletionDialog>();
            dialog.Configure(new ModuleCompletionDialog.Bindings
            {
                ModalPanel = modal.gameObject,
                ModuleTitle = moduleTitle,
                RatingLabel = ratingLabel,
                ScoreLabel = scoreLabel,
                HazardsLabel = hazardsLabel,
                TasksLabel = tasksLabel,
                SummaryText = summaryText,
                ReturnCampusButton = returnBtn,
                ContinueButton = continueBtn
            });
            modal.gameObject.SetActive(false);
        }

        static InputField CreateInputField(string name, Transform parent, Sprite sprite, Material material,
            Font bodyFont, Vector2 position, Vector2 size, string placeholderText)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.material = material;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = Field;
            var input = obj.AddComponent<InputField>();
            input.targetGraphic = img;

            var text = StretchText("Text", obj.transform, bodyFont, 13, Primary,
                new Vector2(10f, 4f), new Vector2(-10f, -4f));
            var placeholder = StretchText("Placeholder", obj.transform, bodyFont, 13, Muted,
                new Vector2(10f, 4f), new Vector2(-10f, -4f));
            placeholder.text = placeholderText;
            placeholder.fontStyle = FontStyle.Italic;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        static Button CreateButton(string name, Transform parent, Sprite sprite, Material material,
            Color color, Vector2 position, Vector2 size, Vector2 anchor)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.sprite = sprite;
            img.material = material;
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = color;
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            var c = btn.colors;
            c.highlightedColor = color * 1.25f;
            c.pressedColor = color * 0.8f;
            btn.colors = c;
            return btn;
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
