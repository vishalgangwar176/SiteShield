using SafetyTraining.Core;
using SafetyTraining.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Editor
{
    internal static class SafetyScenePrimitives
    {
        const string MaterialFolder = "Assets/SafetyTraining/GeneratedMaterials";

        public static GameObject Primitive(
            PrimitiveType type,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Color color)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = Material(color);
            return item;
        }

        public static TextMesh PillLabel(
            string text,
            Transform parent,
            Vector3 position,
            float size,
            string icon = "⛑",
            float width = 1.35f,
            float height = 0.36f)
        {
            var pillRoot = new GameObject($"PillLabel - {text}");
            pillRoot.transform.SetParent(parent, false);
            pillRoot.transform.localPosition = position;
            pillRoot.transform.localRotation = Quaternion.identity;

            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "Pill Background";
            bg.transform.SetParent(pillRoot.transform, false);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localScale = new Vector3(width, height, 0.02f);
            bg.GetComponent<Renderer>().sharedMaterial = Material(new Color(0.043f, 0.086f, 0.141f, 0.88f));
            Object.DestroyImmediate(bg.GetComponent<Collider>());

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = "Pill Border";
            rim.transform.SetParent(pillRoot.transform, false);
            rim.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            rim.transform.localScale = new Vector3(width + 0.04f, height + 0.04f, 0.015f);
            rim.GetComponent<Renderer>().sharedMaterial = Material(new Color(0.118f, 0.227f, 0.322f));
            Object.DestroyImmediate(rim.GetComponent<Collider>());

            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shadow.name = "Pill Shadow";
            shadow.transform.SetParent(pillRoot.transform, false);
            shadow.transform.localPosition = new Vector3(0.025f, -0.025f, 0.01f);
            shadow.transform.localScale = new Vector3(width + 0.05f, height + 0.05f, 0.01f);
            shadow.GetComponent<Renderer>().sharedMaterial = Material(new Color(0.01f, 0.02f, 0.03f, 0.45f));
            Object.DestroyImmediate(shadow.GetComponent<Collider>());

            var labelObj = new GameObject("Text");
            labelObj.transform.SetParent(pillRoot.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0f, -0.025f);
            var mesh = labelObj.AddComponent<TextMesh>();
            mesh.text = string.IsNullOrEmpty(icon) ? text : $"{icon}  {text}";
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(0.949f, 0.965f, 0.980f);
            mesh.fontStyle = FontStyle.Bold;
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (font != null)
            {
                mesh.font = font;
                labelObj.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            return mesh;
        }

        public static TextMesh Label(string text, Transform parent, Vector3 position, float size)
        {
            var label = new GameObject($"Label - {text}");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            label.transform.localRotation = Quaternion.identity;
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.color = Color.white;
            return mesh;
        }

        public static InspectionTarget Target(
            GameObject item,
            TrainingSiteId site,
            string id,
            bool hazard,
            string title,
            string rationale,
            string action)
        {
            if (item.GetComponent<XRSimpleInteractable>() == null)
                item.AddComponent<XRSimpleInteractable>();
            if (item.GetComponent<InteractiveHoverFeedback>() == null)
                item.AddComponent<InteractiveHoverFeedback>();
            var target = item.AddComponent<InspectionTarget>();
            target.Configure(site, id, hazard, title, rationale, action);
            return target;
        }

        public static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }

        public static Material Material(Color color)
        {
            EnsureFolder(MaterialFolder);
            var path = $"{MaterialFolder}/{ColorUtility.ToHtmlStringRGB(color)}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_EMISSION");
            material.SetFloat("_Glossiness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
