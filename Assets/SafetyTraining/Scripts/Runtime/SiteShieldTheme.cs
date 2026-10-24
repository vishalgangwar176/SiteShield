using UnityEngine;

namespace SafetyTraining.Runtime
{
    /// <summary>
    /// Master SiteShield Design System tokens.
    /// Provides unified colors, typography metrics, and component styling rules.
    /// </summary>
    public static class SiteShieldTheme
    {
        // Surfaces & Backgrounds
        public static readonly Color DeepNavy = new Color32(0x10, 0x1C, 0x28, 0xFF);         // #101C28 Primary surfaces
        public static readonly Color DeepNavyGlass = new Color32(0x10, 0x1C, 0x28, 0xDC);    // #101C28 at 86% opacity
        public static readonly Color SecondaryNavy = new Color32(0x17, 0x2A, 0x39, 0xFF);   // #172A39 Elevated chips/cards
        public static readonly Color SlateBlue = new Color32(0x24, 0x37, 0x46, 0xFF);       // #243746 Borders & dividers

        // Brand Identity
        public static readonly Color BrandTeal = new Color32(0x12, 0x9C, 0x96, 0xFF);       // #129C96 Core brand teal
        public static readonly Color BrightTeal = new Color32(0x39, 0xD6, 0xC5, 0xFF);      // #39D6C5 Vibrant interactive accent
        public static readonly Color WarmOffWhite = new Color32(0xF5, 0xF3, 0xED, 0xFF);    // #F5F3ED Hero / Title canvas

        // Typography
        public static readonly Color TextPrimary = new Color32(0xF2, 0xF6, 0xFA, 0xFF);     // #F2F6FA High-contrast white
        public static readonly Color TextSecondary = new Color32(0xA5, 0xB5, 0xC5, 0xFF);   // #A5B5C5 Muted supporting text
        public static readonly Color TextDarkNavy = new Color32(0x10, 0x1C, 0x28, 0xFF);    // #101C28 Title screen brand typography

        // Semantic Status
        public static readonly Color AmberWarning = new Color32(0xFF, 0xB5, 0x47, 0xFF);    // #FFB547 Hazard / Caution
        public static readonly Color SuccessGreen = new Color32(0x51, 0xD8, 0x8A, 0xFF);    // #51D88A Verified control / Success
        public static readonly Color CriticalRed = new Color32(0xE2, 0x55, 0x55, 0xFF);     // #E25555 Critical safety violation
        public static readonly Color ActionBlue = new Color32(0x4F, 0x98, 0xFF, 0xFF);      // #4F98FF Mandatory action / Informational

        // Shadow
        public static readonly Color PanelShadow = new Color(0.02f, 0.05f, 0.08f, 0.65f);

        // Standard metrics
        public const float BorderThickness = 2f;
        public const float CornerRadius = 8f;
        public const float TransitionDuration = 0.20f;
    }
}
