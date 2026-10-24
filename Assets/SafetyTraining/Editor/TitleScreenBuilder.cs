using SafetyTraining.Runtime;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace SafetyTraining.Editor
{
    public static class TitleScreenBuilder
    {
        const string AssetRoot = "Assets/ThirdParty/Kenney/UI-Pack-SciFi";
        const string PanelPath = AssetRoot + "/PNG/Extra/Default/panel_glass_notches.png";
        const string ButtonPath = AssetRoot + "/PNG/Blue/Default/button_square_depth_flat.png";
        const string FontPath = AssetRoot + "/Font/Kenney Future Narrow.ttf";
        const string BodyFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        const string MaterialPath = "Assets/SafetyTraining/GeneratedMaterials/Safety HUD UI.mat";

        public static void Create()
        {
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPath);
            var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonPath);
            var displayFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var bodyFont = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath) ?? displayFont;
            var uiMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            var root = new GameObject("SiteShield Title Screen");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // Below Chat (100), above HUD (80)

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200f, 700f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<TrackedDeviceGraphicRaycaster>();
            var canvasGroup = root.AddComponent<CanvasGroup>();
            var titleManager = root.AddComponent<SiteShieldTitleScreen>();

            // Fullscreen Backdrop (Warm Off-White with deep overlay)
            var backdrop = CreateImage("Screen Backdrop", root.transform, null, uiMaterial,
                new Color(0.06f, 0.11f, 0.16f, 0.88f), Vector2.zero, Vector2.zero,
                Vector2.zero, Vector2.one);
            var backdropRect = backdrop.rectTransform;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;

            // ==========================================
            // 1. TITLE SCREEN VIEW
            // ==========================================
            var titleView = new GameObject("Title Screen View");
            titleView.transform.SetParent(root.transform, false);
            var titleViewRect = titleView.AddComponent<RectTransform>();
            titleViewRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleViewRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleViewRect.sizeDelta = new Vector2(960f, 620f);

            // Elegant Main Hero Card (Light Warm Off-White surface with Navy trim)
            var heroCard = CreateImage("Hero Card", titleView.transform, panelSprite, uiMaterial,
                SiteShieldTheme.WarmOffWhite, Vector2.zero, new Vector2(960f, 620f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            heroCard.type = UnityEngine.UI.Image.Type.Sliced;
            var cardShadow = heroCard.gameObject.AddComponent<Shadow>();
            cardShadow.effectColor = SiteShieldTheme.PanelShadow;
            cardShadow.effectDistance = new Vector2(10f, -12f);

            // Teal Brand Accent Bar across the top of hero card
            var topBrandBar = CreateImage("Brand Top Rail", heroCard.transform, null, uiMaterial,
                SiteShieldTheme.BrandTeal, new Vector2(0f, 302f), new Vector2(960f, 16f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Brand Logo & Title: SITESHIELD
            var logoTitle = CreateText("Brand Title", heroCard.transform, "SITESHIELD", displayFont, 48,
                SiteShieldTheme.TextDarkNavy, new Vector2(0f, 215f), new Vector2(700f, 60f), TextAnchor.MiddleCenter);
            logoTitle.fontStyle = FontStyle.Bold;

            // Brand Safety Badge / Sub-eyebrow
            var badgeText = CreateText("Brand Badge", heroCard.transform, "INDUSTRIAL WORKPLACE SAFETY SIMULATOR",
                displayFont, 14, SiteShieldTheme.BrandTeal, new Vector2(0f, 260f), new Vector2(700f, 24f),
                TextAnchor.MiddleCenter);
            badgeText.fontStyle = FontStyle.Bold;

            // Tagline: "Practise safer decisions before the real worksite."
            var tagline = CreateText("Tagline", heroCard.transform,
                "Practise safer decisions before the real worksite.", bodyFont, 22,
                new Color(0.14f, 0.22f, 0.30f), new Vector2(0f, 165f), new Vector2(800f, 32f),
                TextAnchor.MiddleCenter);
            tagline.fontStyle = FontStyle.Bold;

            // Supporting Description
            var desc = CreateText("Description", heroCard.transform,
                "A Unity 6 and OpenXR workplace-safety training simulation featuring hazard identification,\n" +
                "hands-on procedural controls, and intelligent AI coaching.", bodyFont, 16,
                new Color(0.32f, 0.42f, 0.52f), new Vector2(0f, 115f), new Vector2(800f, 48f),
                TextAnchor.MiddleCenter);

            // Decorative separator line
            CreateImage("Divider Line", heroCard.transform, null, uiMaterial,
                new Color(0.80f, 0.85f, 0.90f), new Vector2(0f, 75f), new Vector2(720f, 2f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Primary Navigation Buttons
            var btnEnter = CreateButton("Btn Enter Campus", heroCard.transform, buttonSprite,
                SiteShieldTheme.DeepNavy, SiteShieldTheme.BrandTeal, new Vector2(0f, 15f), new Vector2(440f, 54f));
            var txtEnter = CreateText("Text", btnEnter.transform, "ENTER TRAINING CAMPUS", displayFont, 20,
                SiteShieldTheme.BrightTeal, Vector2.zero, new Vector2(400f, 40f), TextAnchor.MiddleCenter);
            txtEnter.fontStyle = FontStyle.Bold;
            btnEnter.onClick.AddListener(titleManager.StartCampusTraining);
            AddPersistent(btnEnter, titleManager.StartCampusTraining);

            var btnModules = CreateButton("Btn Modules", heroCard.transform, buttonSprite,
                SiteShieldTheme.SecondaryNavy, SiteShieldTheme.SlateBlue, new Vector2(0f, -50f), new Vector2(440f, 50f));
            var txtModules = CreateText("Text", btnModules.transform, "QUICK MODULE ACCESS", displayFont, 18,
                SiteShieldTheme.TextPrimary, Vector2.zero, new Vector2(400f, 40f), TextAnchor.MiddleCenter);
            btnModules.onClick.AddListener(titleManager.ShowModuleSelect);
            AddPersistent(btnModules, titleManager.ShowModuleSelect);

            var btnSettings = CreateButton("Btn Settings", heroCard.transform, buttonSprite,
                SiteShieldTheme.SecondaryNavy, SiteShieldTheme.SlateBlue, new Vector2(0f, -112f), new Vector2(440f, 50f));
            var txtSettings = CreateText("Text", btnSettings.transform, "SETTINGS & ACCESSIBILITY", displayFont, 18,
                SiteShieldTheme.TextPrimary, Vector2.zero, new Vector2(400f, 40f), TextAnchor.MiddleCenter);
            btnSettings.onClick.AddListener(titleManager.ShowSettings);
            AddPersistent(btnSettings, titleManager.ShowSettings);

            var btnExit = CreateButton("Btn Exit", heroCard.transform, buttonSprite,
                new Color(0.20f, 0.12f, 0.14f), SiteShieldTheme.CriticalRed, new Vector2(0f, -174f), new Vector2(440f, 46f));
            var txtExit = CreateText("Text", btnExit.transform, "EXIT SIMULATOR", displayFont, 16,
                new Color(0.95f, 0.65f, 0.65f), Vector2.zero, new Vector2(400f, 40f), TextAnchor.MiddleCenter);
            btnExit.onClick.AddListener(titleManager.ExitSimulator);
            AddPersistent(btnExit, titleManager.ExitSimulator);

            // Footer metadata
            CreateText("Footer Info", heroCard.transform,
                "Unity 6 LTS • OpenXR • Dual Desktop (KBM) and XR Controller Supported • Press [Esc] to return to menu",
                bodyFont, 13, new Color(0.45f, 0.55f, 0.65f), new Vector2(0f, -270f), new Vector2(850f, 25f),
                TextAnchor.MiddleCenter);

            // ==========================================
            // 2. MODULE SELECT VIEW
            // ==========================================
            var moduleView = new GameObject("Module Select View");
            moduleView.transform.SetParent(root.transform, false);
            var moduleRect = moduleView.AddComponent<RectTransform>();
            moduleRect.anchorMin = new Vector2(0.5f, 0.5f);
            moduleRect.anchorMax = new Vector2(0.5f, 0.5f);
            moduleRect.sizeDelta = new Vector2(1040f, 620f);
            moduleView.SetActive(false);

            var moduleCard = CreateImage("Module Card Shell", moduleView.transform, panelSprite, uiMaterial,
                SiteShieldTheme.DeepNavy, Vector2.zero, new Vector2(1040f, 620f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            moduleCard.type = UnityEngine.UI.Image.Type.Sliced;

            var moduleTitle = CreateText("Module Header", moduleCard.transform, "SELECT TRAINING WORK ZONE",
                displayFont, 28, SiteShieldTheme.TextPrimary, new Vector2(0f, 245f), new Vector2(700f, 40f),
                TextAnchor.MiddleCenter);
            moduleTitle.fontStyle = FontStyle.Bold;

            CreateText("Module Subtitle", moduleCard.transform,
                "Select any of the five specialized field modules to launch directly into focused training.",
                bodyFont, 16, SiteShieldTheme.TextSecondary, new Vector2(0f, 210f), new Vector2(800f, 26f),
                TextAnchor.MiddleCenter);

            // 5 Module Cards
            var mod1 = CreateModuleLauncherCard(moduleCard.transform, buttonSprite, displayFont, bodyFont,
                "01", "CONSTRUCTION SITE", "Fall Edge Protection & 5 Hands-on Controls",
                SiteShieldTheme.AmberWarning, new Vector2(-280f, 80f), () => titleManager.LaunchModule(0));

            var mod2 = CreateModuleLauncherCard(moduleCard.transform, buttonSprite, displayFont, bodyFont,
                "02", "WAREHOUSE OPERATIONS", "Spill Isolation & Pedestrian Separation",
                SiteShieldTheme.ActionBlue, new Vector2(280f, 80f), () => titleManager.LaunchModule(1));

            var mod3 = CreateModuleLauncherCard(moduleCard.transform, buttonSprite, displayFont, bodyFont,
                "03", "FIRE RESPONSE", "Extinguisher Clearance & Egress Routes",
                SiteShieldTheme.CriticalRed, new Vector2(-280f, -40f), () => titleManager.LaunchModule(2));

            var mod4 = CreateModuleLauncherCard(moduleCard.transform, buttonSprite, displayFont, bodyFont,
                "04", "CHEMICAL PROCESSING", "GHS Drum Labeling, Segregation & Eyewash",
                SiteShieldTheme.SuccessGreen, new Vector2(280f, -40f), () => titleManager.LaunchModule(3));

            var mod5 = CreateModuleLauncherCard(moduleCard.transform, buttonSprite, displayFont, bodyFont,
                "05", "ELECTRICAL MAINTENANCE", "Lockout/Tagout (LOTO) & Cable Bridges",
                new Color(0.65f, 0.50f, 1.0f), new Vector2(0f, -155f), () => titleManager.LaunchModule(4));

            // Back button
            var btnModuleBack = CreateButton("Btn Module Back", moduleCard.transform, buttonSprite,
                SiteShieldTheme.SecondaryNavy, SiteShieldTheme.BrandTeal, new Vector2(0f, -250f), new Vector2(260f, 44f));
            CreateText("Text", btnModuleBack.transform, "← BACK TO MENU", displayFont, 16,
                SiteShieldTheme.BrightTeal, Vector2.zero, new Vector2(240f, 36f), TextAnchor.MiddleCenter);
            btnModuleBack.onClick.AddListener(titleManager.ShowTitleMenu);
            AddPersistent(btnModuleBack, titleManager.ShowTitleMenu);

            // ==========================================
            // 3. SETTINGS & ACCESSIBILITY VIEW
            // ==========================================
            var settingsView = new GameObject("Settings View");
            settingsView.transform.SetParent(root.transform, false);
            var settingsRect = settingsView.AddComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
            settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.sizeDelta = new Vector2(860f, 580f);
            settingsView.SetActive(false);

            var settingsCard = CreateImage("Settings Shell", settingsView.transform, panelSprite, uiMaterial,
                SiteShieldTheme.DeepNavy, Vector2.zero, new Vector2(860f, 580f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            settingsCard.type = UnityEngine.UI.Image.Type.Sliced;

            var settingsTitle = CreateText("Settings Title", settingsCard.transform, "SETTINGS & ACCESSIBILITY",
                displayFont, 28, SiteShieldTheme.TextPrimary, new Vector2(0f, 220f), new Vector2(600f, 40f),
                TextAnchor.MiddleCenter);
            settingsTitle.fontStyle = FontStyle.Bold;

            CreateText("Settings Subtitle", settingsCard.transform,
                "Customize simulation sound, motion comfort, and control parameters.",
                bodyFont, 16, SiteShieldTheme.TextSecondary, new Vector2(0f, 180f), new Vector2(650f, 26f),
                TextAnchor.MiddleCenter);

            // Volume Control
            CreateText("Vol Label", settingsCard.transform, "MASTER AUDIO VOLUME", displayFont, 16,
                SiteShieldTheme.TextPrimary, new Vector2(-150f, 90f), new Vector2(280f, 30f), TextAnchor.MiddleLeft);
            var volSlider = CreateSlider("Volume Slider", settingsCard.transform, buttonSprite,
                new Vector2(150f, 90f), new Vector2(260f, 26f), 0.85f);
            volSlider.onValueChanged.AddListener(titleManager.OnVolumeChanged);
            AddPersistent(volSlider.onValueChanged, titleManager.OnVolumeChanged);

            // Mute Toggle
            CreateText("Mute Label", settingsCard.transform, "MUTE ALL AUDIO", displayFont, 16,
                SiteShieldTheme.TextPrimary, new Vector2(-150f, 30f), new Vector2(280f, 30f), TextAnchor.MiddleLeft);
            var muteToggle = CreateToggle("Mute Toggle", settingsCard.transform, buttonSprite,
                new Vector2(150f, 30f), false);
            muteToggle.onValueChanged.AddListener(titleManager.OnMuteToggled);
            AddPersistent(muteToggle.onValueChanged, titleManager.OnMuteToggled);

            // Reduced Motion Toggle (Accessibility)
            CreateText("RedMo Label", settingsCard.transform, "REDUCED MOTION (VR COMFORT)", displayFont, 16,
                SiteShieldTheme.TextPrimary, new Vector2(-150f, -30f), new Vector2(280f, 30f), TextAnchor.MiddleLeft);
            var redMoToggle = CreateToggle("Reduced Motion Toggle", settingsCard.transform, buttonSprite,
                new Vector2(150f, -30f), false);
            redMoToggle.onValueChanged.AddListener(titleManager.OnReducedMotionToggled);
            AddPersistent(redMoToggle.onValueChanged, titleManager.OnReducedMotionToggled);

            // Look Sensitivity Slider
            CreateText("Sens Label", settingsCard.transform, "DESKTOP LOOK SENSITIVITY", displayFont, 16,
                SiteShieldTheme.TextPrimary, new Vector2(-150f, -90f), new Vector2(280f, 30f), TextAnchor.MiddleLeft);
            var sensSlider = CreateSlider("Sens Slider", settingsCard.transform, buttonSprite,
                new Vector2(150f, -90f), new Vector2(260f, 26f), 1.0f);
            sensSlider.minValue = 0.2f;
            sensSlider.maxValue = 3.0f;
            sensSlider.onValueChanged.AddListener(titleManager.OnLookSensitivityChanged);
            AddPersistent(sensSlider.onValueChanged, titleManager.OnLookSensitivityChanged);

            // Back button
            var btnSettingsBack = CreateButton("Btn Settings Back", settingsCard.transform, buttonSprite,
                SiteShieldTheme.SecondaryNavy, SiteShieldTheme.BrandTeal, new Vector2(0f, -190f), new Vector2(260f, 44f));
            CreateText("Text", btnSettingsBack.transform, "← BACK TO MENU", displayFont, 16,
                SiteShieldTheme.BrightTeal, Vector2.zero, new Vector2(240f, 36f), TextAnchor.MiddleCenter);
            btnSettingsBack.onClick.AddListener(titleManager.ShowTitleMenu);
            AddPersistent(btnSettingsBack, titleManager.ShowTitleMenu);

            // Link serialized fields to manager
            SetField(titleManager, "rootCanvas", canvas);
            SetField(titleManager, "mainCanvasGroup", canvasGroup);
            SetField(titleManager, "screenBackdrop", backdrop.gameObject);
            SetField(titleManager, "titleScreenView", titleView);
            SetField(titleManager, "moduleSelectView", moduleView);
            SetField(titleManager, "settingsView", settingsView);
            SetField(titleManager, "btnEnterCampus", btnEnter);
            SetField(titleManager, "btnModuleAccess", btnModules);
            SetField(titleManager, "btnSettings", btnSettings);
            SetField(titleManager, "btnExit", btnExit);
            SetField(titleManager, "btnModuleBack", btnModuleBack);
            SetField(titleManager, "btnSettingsBack", btnSettingsBack);
            SetField(titleManager, "moduleButtons", new[] { mod1, mod2, mod3, mod4, mod5 });
            SetField(titleManager, "volumeSlider", volSlider);
            SetField(titleManager, "muteToggle", muteToggle);
            SetField(titleManager, "reducedMotionToggle", redMoToggle);
            SetField(titleManager, "lookSensitivitySlider", sensSlider);

            EditorUtility.SetDirty(titleManager);
            EditorUtility.SetDirty(root);
        }

        static Button CreateModuleLauncherCard(Transform parent, Sprite sprite, Font displayFont, Font bodyFont,
            string number, string title, string subtitle, Color accent, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var btn = CreateButton($"Module Card {number}", parent, sprite,
                SiteShieldTheme.SecondaryNavy, accent, pos, new Vector2(490f, 85f));
            btn.onClick.AddListener(action);

            // Number tag
            var numText = CreateText("Num", btn.transform, number, displayFont, 24, accent,
                new Vector2(-205f, 0f), new Vector2(50f, 50f), TextAnchor.MiddleCenter);
            numText.fontStyle = FontStyle.Bold;

            // Title
            var titleText = CreateText("Title", btn.transform, title, displayFont, 18, SiteShieldTheme.TextPrimary,
                new Vector2(25f, 15f), new Vector2(380f, 28f), TextAnchor.MiddleLeft);
            titleText.fontStyle = FontStyle.Bold;

            // Subtitle
            CreateText("Sub", btn.transform, subtitle, bodyFont, 13, SiteShieldTheme.TextSecondary,
                new Vector2(25f, -15f), new Vector2(380f, 24f), TextAnchor.MiddleLeft);

            return btn;
        }

        static Image CreateImage(string name, Transform parent, Sprite sprite, Material material,
            Color color, Vector2 pos, Vector2 size, Vector2 anchor, Vector2 pivot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.material = material;
            image.color = color;
            return image;
        }

        static Text CreateText(string name, Transform parent, string content, Font font, int size,
            Color color, Vector2 pos, Vector2 dimensions, TextAnchor alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = dimensions;

            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static Button CreateButton(string name, Transform parent, Sprite sprite, Color normalColor,
            Color highlightColor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = normalColor;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = Color.Lerp(normalColor, highlightColor, 0.55f);
            colors.pressedColor = highlightColor;
            colors.selectedColor = Color.Lerp(normalColor, highlightColor, 0.40f);
            button.colors = colors;

            return button;
        }

        static Slider CreateSlider(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, float defaultValue)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var bg = CreateImage("Background", go.transform, sprite, null,
                SiteShieldTheme.SecondaryNavy, Vector2.zero, size, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            bg.type = Image.Type.Sliced;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.sizeDelta = Vector2.zero;

            var fill = CreateImage("Fill", fillArea.transform, sprite, null,
                SiteShieldTheme.BrandTeal, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(1f, 1f));
            fill.type = Image.Type.Sliced;

            var slider = go.AddComponent<Slider>();
            slider.targetGraphic = bg;
            slider.fillRect = fill.rectTransform;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultValue;
            return slider;
        }

        static Toggle CreateToggle(string name, Transform parent, Sprite sprite, Vector2 pos, bool defaultValue)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(36f, 36f);

            var bg = CreateImage("Background", go.transform, sprite, null,
                SiteShieldTheme.SecondaryNavy, Vector2.zero, new Vector2(36f, 36f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            bg.type = Image.Type.Sliced;

            var check = CreateImage("Checkmark", bg.transform, sprite, null,
                SiteShieldTheme.BrightTeal, Vector2.zero, new Vector2(22f, 22f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.isOn = defaultValue;
            return toggle;
        }

        static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }

        static void AddPersistent(Button button, UnityEngine.Events.UnityAction action)
        {
            UnityEventTools.AddPersistentListener(button.onClick, action);
            button.onClick.SetPersistentListenerState(
                button.onClick.GetPersistentEventCount() - 1,
                UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
        }

        static void AddPersistent<T>(UnityEngine.Events.UnityEvent<T> unityEvent, UnityEngine.Events.UnityAction<T> action)
        {
            UnityEventTools.AddPersistentListener(unityEvent, action);
            unityEvent.SetPersistentListenerState(
                unityEvent.GetPersistentEventCount() - 1,
                UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
        }
    }
}
