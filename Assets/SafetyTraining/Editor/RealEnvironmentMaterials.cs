using UnityEditor;
using UnityEngine;

namespace SafetyTraining.Editor
{
    internal static class RealEnvironmentMaterials
    {
        const string TextureRoot = "Assets/ThirdParty/PolyHaven/Environment/Textures/";
        const string MaterialRoot = "Assets/SafetyTraining/GeneratedMaterials/RealEnvironment";

        public static Material Asphalt => Pbr("asphalt_03", 0.03f, 0.12f, 8f);
        public static Material DamagedConcrete => Pbr("concrete_floor_damaged_01", 0f, 0.18f, 3.5f);
        public static Material ConcreteWall => Pbr("concrete_block_wall_03", 0f, 0.16f, 3f);
        public static Material MetalSheet => Pbr("box_profile_metal_sheet", 0.68f, 0.32f, 2.2f);
        public static Material ConcretePanel => Pbr("concrete_panels", 0f, 0.2f, 2.5f);

        public static TerrainLayer AsphaltTerrainLayer()
        {
            SafetyScenePrimitives.EnsureFolder(MaterialRoot);
            var path = $"{MaterialRoot}/Asphalt Terrain.terrainlayer";
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }

            layer.diffuseTexture = LoadTexture("asphalt_03_diff_1k.jpg");
            layer.normalMapTexture = LoadNormal("asphalt_03_nor_gl_1k.jpg");
            layer.tileSize = new Vector2(5.5f, 5.5f);
            layer.normalScale = 0.75f;
            layer.metallic = 0.02f;
            layer.smoothness = 0.12f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        static Material Pbr(string id, float metallic, float smoothness, float tiling)
        {
            SafetyScenePrimitives.EnsureFolder(MaterialRoot);
            var path = $"{MaterialRoot}/{id}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = $"Real PBR - {id}";
            material.SetTexture("_MainTex", LoadTexture($"{id}_diff_1k.jpg"));
            material.SetTexture("_BumpMap", LoadNormal($"{id}_nor_gl_1k.jpg"));
            material.SetTexture("_OcclusionMap", LoadTexture($"{id}_ao_1k.jpg"));
            material.SetTextureScale("_MainTex", Vector2.one * tiling);
            material.SetTextureScale("_BumpMap", Vector2.one * tiling);
            material.SetTextureScale("_OcclusionMap", Vector2.one * tiling);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_BumpScale", 0.72f);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        static Texture2D LoadTexture(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot + fileName);
        }

        static Texture2D LoadNormal(string fileName)
        {
            var path = TextureRoot + fileName;
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
