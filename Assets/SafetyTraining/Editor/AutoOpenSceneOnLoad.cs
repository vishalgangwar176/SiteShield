using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SafetyTraining.Editor
{
    [InitializeOnLoad]
    internal static class AutoOpenSceneOnLoad
    {
        const string TargetScene = "Assets/SafetyTraining/Scenes/SafetyTrainingExplorer.unity";

        static AutoOpenSceneOnLoad()
        {
            EditorApplication.delayCall += EnsureSceneLoaded;
        }

        static void EnsureSceneLoaded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode)
                return;

            var current = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(current.path) || current.name == "Untitled")
            {
                if (System.IO.File.Exists(TargetScene))
                {
                    EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
                }
            }
        }
    }
}
