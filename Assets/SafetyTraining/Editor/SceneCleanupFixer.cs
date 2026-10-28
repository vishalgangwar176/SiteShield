using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SafetyTraining.Runtime;

namespace SafetyTraining.Editor
{
    public static class SceneCleanupFixer
    {
        [InitializeOnLoadMethod]
        static void AutoRunOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                if (!SessionState.GetBool("SiteShield_AutoRebuilt_Done_v2", false))
                {
                    SessionState.SetBool("SiteShield_AutoRebuilt_Done_v2", true);
                    ExecuteCleanup();
                }
            };
        }

        [MenuItem("Safety Training/Fix Scene (Remove Graffiti, NPC Cyan & Polish Signs)")]
        public static void ExecuteCleanup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Debug.Log("[SiteShield] Rebuilding prototype scene with all visual improvements and fixes...");
            SafetyTrainingSceneBuilder.Build();
            Debug.Log("[SiteShield] Prototype scene rebuild complete: All visual polish applied and saved!");
        }
    }
}
