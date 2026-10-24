using SafetyTraining.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Editor
{
    static class ProfessionalHudBuilder
    {
        const string AssetRoot = "Assets/ThirdParty/Kenney/UI-Pack-SciFi";
        const string PanelPath = AssetRoot + "/PNG/Extra/Default/panel_glass_notches.png";
        const string IconPath = AssetRoot + "/PNG/Blue/Default/crosshair_color_c.png";
        const string BarPath = AssetRoot + "/PNG/Blue/Default/bar_square_gloss_small.png";
        const string FontPath = AssetRoot + "/Font/Kenney Future Narrow.ttf";
        const string BodyFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        const string MaterialPath = "Assets/SafetyTraining/GeneratedMaterials/Safety HUD UI.mat";

        static readonly Color Panel = new Color(0.043f, 0.086f, 0.141f, 0.88f); // #0B1624 @ 88%
        static readonly Color Raised = new Color(0.118f, 0.227f, 0.322f, 0.95f); // #1E3A52
        static readonly Color Primary = new Color(0.949f, 0.965f, 0.980f); // #F2F6FA
        static readonly Color Muted = new Color(0.624f, 0.702f, 0.784f); // #9FB3C8
        static readonly Color Accent = new Color(0.133f, 0.827f, 0.773f); // #22D3C5
        static Material uiMaterial;

        public static void Create(Camera viewer)
        {
            ConfigureSprite(PanelPath, new Vector4(12f, 12f, 12f, 12f));
            ConfigureSprite(IconPath, Vector4.zero);
            ConfigureSprite(BarPath, new Vector4(5f, 5f, 5f, 5f));
            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPath);
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            var barSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarPath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            var bodyFont = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath) ?? font;
            uiMaterial = LoadOrCreateUiMaterial();

            var root = new GameObject("Training HUD");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200f, 700f);
            scaler.matchWidthOrHeight = 0.5f;
            var canvasGroup = root.AddComponent<CanvasGroup>();
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(1200f, 700f);

            var missionShell = Shell("Mission Shell", root.transform, panelSprite,
                new Vector2(36f, -34f), new Vector2(410f, 82f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            AddRail(missionShell.transform, new Vector2(8f, -12f), new Vector2(5f, 56f), Accent);
            Text("Mission Label", missionShell.transform, "ACTIVE WORK ZONE", font, 16, Accent,
                new Vector2(28f, -15f), new Vector2(340f, 22f), TextAnchor.UpperLeft);
            var site = Text("Site Title", missionShell.transform, "TRAINING CAMPUS", font, 30, Primary,
                new Vector2(28f, -35f), new Vector2(350f, 40f), TextAnchor.UpperLeft);

            var scoreShell = Shell("Score Shell", root.transform, panelSprite,
                new Vector2(-36f, -34f), new Vector2(246f, 82f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            Text("Score Label", scoreShell.transform, "ASSESSMENT SCORE", font, 16, Muted,
                new Vector2(-18f, -14f), new Vector2(205f, 22f), TextAnchor.UpperRight);
            var score = Text("Score Value", scoreShell.transform, "0000", font, 30, Primary,
                new Vector2(-18f, -36f), new Vector2(120f, 38f), TextAnchor.UpperRight);
            var scoreDelta = Text("Score Delta", scoreShell.transform, "+100", font, 20, Accent,
                new Vector2(-140f, -36f), new Vector2(90f, 38f), TextAnchor.UpperRight);
            scoreDelta.fontStyle = FontStyle.Bold;

            var feedbackShell = Shell("Feedback Shell", root.transform, panelSprite,
                new Vector2(0f, 16f), new Vector2(820f, 118f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            var icon = Image("State Icon", feedbackShell.transform, iconSprite, Accent,
                new Vector2(24f, -24f), new Vector2(26f, 26f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            var state = Text("State Label", feedbackShell.transform, "Mission Brief", bodyFont, 13, Accent,
                new Vector2(58f, -20f), new Vector2(260f, 18f), TextAnchor.UpperLeft);
            state.fontStyle = FontStyle.Bold;
            var feedback = Text("Feedback", feedbackShell.transform,
                "Inspect each site. Select only conditions you believe are hazardous.", bodyFont, 21, Primary,
                new Vector2(24f, -44f), new Vector2(772f, 44f), TextAnchor.UpperLeft);

            Image("Progress Track", feedbackShell.transform, null, Raised,
                new Vector2(24f, 30f), new Vector2(520f, 7f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            var progress = Image("Progress Fill", feedbackShell.transform, barSprite, Accent,
                new Vector2(24f, 30f), new Vector2(520f, 7f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            progress.rectTransform.pivot = new Vector2(0f, 0.5f);
            progress.rectTransform.localScale = new Vector3(0.02f, 1f, 1f);
            var progressLabel = Text("Progress Label", feedbackShell.transform, "SESSION 00:00 / 20:00", font, 14, Muted,
                new Vector2(-24f, 94f), new Vector2(240f, 20f), TextAnchor.LowerRight);
            var hints = Text("Control Hints", feedbackShell.transform,
                "<color=#F2F6FA><b>[LMB]</b> Inspect</color>    <size=11><color=#9FB3C8>[C] Coach    [H] Toggle Brief    [Esc] Menu</color></size>",
                bodyFont, 14, Muted,
                new Vector2(24f, 8f), new Vector2(520f, 20f), TextAnchor.LowerLeft);
            hints.supportRichText = true;

            // Reticle Dot & Contextual Interaction Prompt
            var reticle = Image("Interaction Reticle", root.transform, null, new Color(1f, 1f, 1f, 0.4f),
                Vector2.zero, new Vector2(4f, 4f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            var promptShell = Shell("Interaction Prompt Pill", root.transform, panelSprite,
                new Vector2(0f, -65f), new Vector2(380f, 48f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var promptGroup = promptShell.AddComponent<CanvasGroup>();
            promptGroup.alpha = 0f;
            promptGroup.blocksRaycasts = false;
            promptGroup.interactable = false;

            var promptIcon = Image("Prompt Icon", promptShell.transform, iconSprite, Accent,
                new Vector2(16f, -13f), new Vector2(22f, 22f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            var promptAction = Text("Prompt Action", promptShell.transform, "[LMB] INSPECT", font, 13, Accent,
                new Vector2(46f, -8f), new Vector2(310f, 16f), TextAnchor.UpperLeft);
            promptAction.fontStyle = FontStyle.Bold;
            var promptTitle = Text("Prompt Title", promptShell.transform, "Target Condition", bodyFont, 14, Primary,
                new Vector2(46f, -24f), new Vector2(310f, 18f), TextAnchor.UpperLeft);

            var promptHud = root.AddComponent<InteractionPromptHud>();
            promptHud.Bind(promptGroup, promptAction, promptTitle, promptIcon, reticle);

            root.AddComponent<TrainingHud>().Configure(new TrainingHud.Bindings
            {
                CanvasGroup = canvasGroup,
                SiteDisplay = site,
                ScoreDisplay = score,
                StateDisplay = state,
                FeedbackDisplay = feedback,
                ProgressDisplay = progressLabel,
                ProgressFill = progress.rectTransform,
                StateIcon = icon,
                ScoreDeltaDisplay = scoreDelta,
                FeedbackShell = feedbackShell.GetComponent<RectTransform>()
            });
        }

        static GameObject Shell(string name, Transform parent, Sprite sprite, Vector2 position,
            Vector2 size, Vector2 anchor, Vector2 pivot)
        {
            var shell = Image(name, parent, sprite, Panel, position, size, anchor, pivot).gameObject;
            shell.GetComponent<UnityEngine.UI.Image>().type = UnityEngine.UI.Image.Type.Sliced;
            var shadow = shell.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.035f, 0.055f, 0.65f);
            shadow.effectDistance = new Vector2(7f, -9f);
            return shell;
        }

        static void AddRail(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            Image("Status Rail", parent, null, color, position, size,
                new Vector2(0f, 1f), new Vector2(0f, 1f));
        }

        static Image Image(string name, Transform parent, Sprite sprite, Color color, Vector2 position,
            Vector2 size, Vector2 anchor, Vector2 pivot)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            var rect = item.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = item.AddComponent<Image>();
            image.sprite = sprite;
            image.material = uiMaterial;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text Text(string name, Transform parent, string value, Font font, int size, Color color,
            Vector2 position, Vector2 dimensions, TextAnchor alignment)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            var rect = item.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(alignment is TextAnchor.UpperRight or TextAnchor.LowerRight ? 1f : 0f,
                alignment is TextAnchor.LowerLeft or TextAnchor.LowerRight ? 0f : 1f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = rect.anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = item.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static void ConfigureSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
        }

        static Material LoadOrCreateUiMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
                return existing;
            var material = new Material(Shader.Find("UI/Default")) { name = "Safety HUD UI" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
