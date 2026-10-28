using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SafetyTraining.Editor
{
    internal static class SafetyEquipmentMeshFactory
    {
        const string MeshFolder = "Assets/SafetyTraining/Generated/EquipmentMeshes";
        const string MaterialFolder = "Assets/SafetyTraining/GeneratedMaterials/SafetyEquipment";

        public static GameObject CreateEyewashBowl(Transform parent, Vector3 position, Color color)
        {
            return CreateMeshObject("Eyewash Bowl", parent, position, EyewashBowlMesh(),
                Material(color, 0.08f, 0.42f));
        }

        public static GameObject CreateCableProtectorShell(Transform parent, Vector3 position, Color color)
        {
            return CreateMeshObject("Cable Protector Shell", parent, position, CableProtectorShellMesh(),
                Material(color, 0.02f, 0.24f));
        }

        public static GameObject CreateElectricalCabinetShell(Transform parent, Vector3 position, Color color)
        {
            return CreateMeshObject("Electrical Cabinet Shell", parent, position, ElectricalCabinetShellMesh(),
                Material(color, 0.38f, 0.32f));
        }

        public static Material Material(Color color, float metallic, float smoothness)
        {
            SafetyScenePrimitives.EnsureFolder(MaterialFolder);
            var id = $"{ColorUtility.ToHtmlStringRGB(color)}-{metallic:F2}-{smoothness:F2}";
            var path = $"{MaterialFolder}/{id}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static GameObject CreateMeshObject(string name, Transform parent, Vector3 position, Mesh mesh,
            Material material)
        {
            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = material;
            return item;
        }

        static Mesh EyewashBowlMesh()
        {
            const int segments = 32;
            var profile = new[]
            {
                new Vector2(0.2f, -0.18f),
                new Vector2(0.42f, -0.12f),
                new Vector2(0.56f, -0.02f),
                new Vector2(0.58f, 0f),
                new Vector2(0.49f, 0.025f),
                new Vector2(0.34f, -0.04f),
                new Vector2(0.16f, -0.12f),
                new Vector2(0.045f, -0.14f)
            };
            var vertices = new List<Vector3>(segments * profile.Length);
            var uvs = new List<Vector2>(segments * profile.Length);
            for (var ring = 0; ring < profile.Length; ring++)
            for (var segment = 0; segment < segments; segment++)
            {
                var angle = segment * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * profile[ring].x, profile[ring].y,
                    Mathf.Sin(angle) * profile[ring].x));
                uvs.Add(new Vector2(segment / (float)segments, ring / (float)(profile.Length - 1)));
            }

            var triangles = new List<int>((profile.Length - 1) * segments * 6);
            for (var ring = 0; ring < profile.Length - 1; ring++)
            for (var segment = 0; segment < segments; segment++)
            {
                var next = (segment + 1) % segments;
                var currentRing = ring * segments;
                var nextRing = (ring + 1) * segments;
                triangles.Add(currentRing + segment);
                triangles.Add(nextRing + segment);
                triangles.Add(currentRing + next);
                triangles.Add(currentRing + next);
                triangles.Add(nextRing + segment);
                triangles.Add(nextRing + next);
            }

            return SaveMesh("Eyewash Bowl", "Eyewash Bowl Mesh", vertices, uvs, triangles);
        }

        static Mesh CableProtectorShellMesh()
        {
            const float halfLength = 2.35f;
            var profile = new[]
            {
                new Vector2(-0.46f, 0f), new Vector2(-0.45f, 0.035f),
                new Vector2(-0.39f, 0.09f), new Vector2(-0.31f, 0.16f),
                new Vector2(-0.21f, 0.22f), new Vector2(-0.12f, 0.245f),
                new Vector2(0.12f, 0.245f), new Vector2(0.21f, 0.22f),
                new Vector2(0.31f, 0.16f), new Vector2(0.39f, 0.09f),
                new Vector2(0.45f, 0.035f), new Vector2(0.46f, 0f)
            };
            var vertices = new List<Vector3>(profile.Length * 2);
            var uvs = new List<Vector2>(profile.Length * 2);
            foreach (var x in new[] { -halfLength, halfLength })
            for (var index = 0; index < profile.Length; index++)
            {
                vertices.Add(new Vector3(x, profile[index].y, profile[index].x));
                uvs.Add(new Vector2((x + halfLength) / (halfLength * 2f), index / (float)(profile.Length - 1)));
            }

            var triangles = new List<int>(profile.Length * 6);
            for (var index = 0; index < profile.Length - 1; index++)
            {
                var opposite = profile.Length + index;
                triangles.Add(index);
                triangles.Add(opposite);
                triangles.Add(index + 1);
                triangles.Add(index + 1);
                triangles.Add(opposite);
                triangles.Add(opposite + 1);
            }
            for (var index = 1; index < profile.Length - 1; index++)
            {
                triangles.Add(0);
                triangles.Add(index + 1);
                triangles.Add(index);
                triangles.Add(profile.Length);
                triangles.Add(profile.Length + index);
                triangles.Add(profile.Length + index + 1);
            }

            return SaveMesh("Cable Protector Shell", "Cable Protector Shell Mesh", vertices, uvs, triangles);
        }

        static Mesh ElectricalCabinetShellMesh()
        {
            const float halfWidth = 0.7f;
            const float halfHeight = 1.16f;
            const float halfDepth = 0.19f;
            const float chamfer = 0.09f;
            var perimeter = new[]
            {
                new Vector2(-halfWidth + chamfer, -halfHeight),
                new Vector2(halfWidth - chamfer, -halfHeight),
                new Vector2(halfWidth, -halfHeight + chamfer),
                new Vector2(halfWidth, halfHeight - chamfer),
                new Vector2(halfWidth - chamfer, halfHeight),
                new Vector2(-halfWidth + chamfer, halfHeight),
                new Vector2(-halfWidth, halfHeight - chamfer),
                new Vector2(-halfWidth, -halfHeight + chamfer)
            };
            var vertices = new List<Vector3>(48);
            var uvs = new List<Vector2>(48);
            var triangles = new List<int>(84);

            for (var edge = 0; edge < perimeter.Length; edge++)
            {
                var next = (edge + 1) % perimeter.Length;
                var first = vertices.Count;
                vertices.Add(new Vector3(perimeter[edge].x, perimeter[edge].y, -halfDepth));
                vertices.Add(new Vector3(perimeter[next].x, perimeter[next].y, -halfDepth));
                vertices.Add(new Vector3(perimeter[next].x, perimeter[next].y, halfDepth));
                vertices.Add(new Vector3(perimeter[edge].x, perimeter[edge].y, halfDepth));
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 3);
            }

            foreach (var z in new[] { -halfDepth, halfDepth })
            {
                var first = vertices.Count;
                for (var index = 0; index < perimeter.Length; index++)
                {
                    vertices.Add(new Vector3(perimeter[index].x, perimeter[index].y, z));
                    uvs.Add(new Vector2(perimeter[index].x / (halfWidth * 2f) + 0.5f,
                        perimeter[index].y / (halfHeight * 2f) + 0.5f));
                }
                for (var index = 1; index < perimeter.Length - 1; index++)
                {
                    if (z < 0f)
                    {
                        triangles.Add(first);
                        triangles.Add(first + index + 1);
                        triangles.Add(first + index);
                    }
                    else
                    {
                        triangles.Add(first);
                        triangles.Add(first + index);
                        triangles.Add(first + index + 1);
                    }
                }
            }

            return SaveMesh("Electrical Cabinet Shell", "Electrical Cabinet Shell Mesh", vertices, uvs, triangles);
        }

        static Mesh SaveMesh(string assetName, string meshName, List<Vector3> vertices, List<Vector2> uvs,
            List<int> triangles)
        {
            SafetyScenePrimitives.EnsureFolder(MeshFolder);
            var path = $"{MeshFolder}/{assetName}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();
            mesh.name = meshName;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

    }
}
