using UnityEditor;
using UnityEngine;

namespace SafetyTraining.Editor
{
    public sealed class RocketboxLowPolyImport : AssetPostprocessor
    {
        bool IsRocketboxAsset => assetPath.Contains("ThirdParty/MicrosoftRocketbox");

        void OnPostprocessMeshHierarchy(GameObject gameObject)
        {
            if (!IsRocketboxAsset)
                return;

            var name = gameObject.name.ToLowerInvariant();
            if (!name.Contains("poly"))
                return;

            gameObject.SetActive(name.Contains("lowpoly") && !name.Contains("ultralowpoly"));
        }

        void OnPreprocessTexture()
        {
            if (!IsRocketboxAsset)
                return;

            var importer = (TextureImporter)assetImporter;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            if (assetPath.ToLowerInvariant().Contains("normal"))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.convertToNormalmap = false;
            }
        }

        void OnPostprocessMaterial(Material material)
        {
            if (!IsRocketboxAsset)
                return;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
        }

        void OnPreprocessModel()
        {
            if (!IsRocketboxAsset)
                return;

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.importAnimation = true;
        }
    }
}
