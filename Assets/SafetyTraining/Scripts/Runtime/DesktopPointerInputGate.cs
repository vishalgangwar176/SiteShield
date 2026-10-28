using UnityEngine;

namespace SafetyTraining.Runtime
{
    public static class DesktopPointerInputGate
    {
        const float FocusRecoverySeconds = 0.45f;
        static float readyAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            Application.focusChanged -= HandleFocusChanged;
            Application.focusChanged += HandleFocusChanged;
            BlockFor(FocusRecoverySeconds);
        }

        static void HandleFocusChanged(bool hasFocus)
        {
            readyAt = hasFocus
                ? Time.realtimeSinceStartup + FocusRecoverySeconds
                : float.PositiveInfinity;
        }

        public static bool CanUsePointer => Time.realtimeSinceStartup >= readyAt;
        public static bool CanUseWorldPointer =>
            CanUsePointer && !(NpcChatPanel.Instance?.IsVisible ?? false);

        public static void BlockFor(float seconds)
        {
            readyAt = Time.realtimeSinceStartup + Mathf.Max(0f, seconds);
        }
    }
}
