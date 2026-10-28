using SafetyTraining.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SafetyTraining.Editor
{
    static class PlacementTargetFactory
    {
        const string MarkerMaterialPath = "Assets/SafetyTraining/GeneratedMaterials/2EC7A8.mat";
        const int SegmentCount = 48;

        public static GameObject Create(Transform parent, string actionObjectName, Vector3 localPosition,
            string actionTitle, float acceptanceRadius)
        {
            var target = new GameObject($"Placement Target - {actionObjectName}");
            target.transform.SetParent(parent, false);
            target.transform.localPosition = new Vector3(localPosition.x, 0.035f, localPosition.z);

            var ring = target.AddComponent<LineRenderer>();
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.positionCount = SegmentCount;
            ring.startWidth = 0.075f;
            ring.endWidth = 0.075f;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MarkerMaterialPath);

            // Large visible drop ring
            var visualRadius = Mathf.Max(acceptanceRadius * 0.95f, 0.95f);
            for (var index = 0; index < SegmentCount; index++)
            {
                var angle = index * Mathf.PI * 2f / SegmentCount;
                ring.SetPosition(index, new Vector3(Mathf.Cos(angle) * visualRadius, 0f,
                    Mathf.Sin(angle) * visualRadius));
            }

            // High-visibility billboarded pill backing for the DROP HERE label
            var labelAnchor = new GameObject("Placement Target Label");
            labelAnchor.transform.SetParent(target.transform, false);
            labelAnchor.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            labelAnchor.AddComponent<BillboardLabel>();

            // Doubled-size dark pill backing (#0B1624 with #1E3A52 border)
            var pillBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillBg.name = "Pill Background";
            pillBg.transform.SetParent(labelAnchor.transform, false);
            pillBg.transform.localScale = new Vector3(2.55f, 0.85f, 0.02f);
            pillBg.GetComponent<Renderer>().sharedMaterial = SafetyScenePrimitives.Material(new Color(0.043f, 0.086f, 0.141f, 0.92f));
            Object.DestroyImmediate(pillBg.GetComponent<Collider>());

            var pillRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillRim.name = "Pill Rim";
            pillRim.transform.SetParent(labelAnchor.transform, false);
            pillRim.transform.localPosition = new Vector3(0f, 0f, 0.005f);
            pillRim.transform.localScale = new Vector3(2.64f, 0.94f, 0.015f);
            pillRim.GetComponent<Renderer>().sharedMaterial = SafetyScenePrimitives.Material(new Color(0.118f, 0.227f, 0.322f));
            Object.DestroyImmediate(pillRim.GetComponent<Collider>());

            // Doubled-size label in Inter / clean sans font
            var labelObj = new GameObject("Text");
            labelObj.transform.SetParent(labelAnchor.transform, false);
            labelObj.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            var label = labelObj.AddComponent<TextMesh>();
            label.text = $"◎ DROP HERE\n{actionTitle}";
            label.characterSize = 0.09f; // Doubled size for clear VR readability
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(0.133f, 0.827f, 0.773f); // #22D3C5
            label.fontStyle = FontStyle.Bold;

            var bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (bodyFont != null)
            {
                label.font = bodyFont;
                labelObj.GetComponent<MeshRenderer>().sharedMaterial = bodyFont.material;
            }

            target.SetActive(false);
            return target;
        }
    }
}
