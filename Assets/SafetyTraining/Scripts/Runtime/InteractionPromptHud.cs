using System;
using UnityEngine;
using UnityEngine.UI;

namespace SafetyTraining.Runtime
{
    /// <summary>
    /// Professional contextual interaction prompt HUD.
    /// Displays clear interaction action verbs and input glyphs ([LMB], [Trigger], [C])
    /// when the trainee is in proximity of an interactable hazard, coach, portal, or equipment prop.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptHud : MonoBehaviour
    {
        public static InteractionPromptHud Instance { get; private set; }

        [SerializeField] CanvasGroup promptGroup;
        [SerializeField] Text actionText;
        [SerializeField] Text objectTitleText;
        [SerializeField] Image promptIcon;
        [SerializeField] Image reticleDot;

        Camera viewerCamera;
        Transform currentTarget;
        string currentAction;
        string currentTitle;
        Color currentAccent = SiteShieldTheme.BrightTeal;
        float targetAlpha = 0f;
        const float MaxInteractionDistance = 4.8f;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (viewerCamera == null)
                viewerCamera = Camera.main;

            var menuOpen = SiteShieldTitleScreen.Instance != null && SiteShieldTitleScreen.Instance.IsOpen;
            var chatOpen = NpcChatPanel.Instance != null && NpcChatPanel.Instance.IsVisible;

            if (menuOpen || chatOpen || viewerCamera == null)
            {
                SetVisible(false);
                return;
            }

            // Raycast forward from camera center
            var ray = new Ray(viewerCamera.transform.position, viewerCamera.transform.forward);
            var hits = Physics.RaycastAll(ray, MaxInteractionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Transform foundTarget = null;
            string action = null;
            string title = null;
            Color accent = SiteShieldTheme.BrightTeal;

            foreach (var hit in hits)
            {
                var trans = hit.collider.transform;

                var inspection = trans.GetComponentInParent<InspectionTarget>();
                if (inspection != null)
                {
                    foundTarget = inspection.transform;
                    action = "[LMB] INSPECT CONDITION";
                    title = inspection.DisplayName;
                    accent = inspection.IsHazard ? SiteShieldTheme.AmberWarning : SiteShieldTheme.BrandTeal;
                    break;
                }

                var placement = trans.GetComponentInParent<HandsOnPlacementInteractable>();
                if (placement != null && placement.isActiveAndEnabled)
                {
                    foundTarget = placement.transform;
                    action = "[HOLD LMB & DRAG] POSITION CONTROL";
                    title = placement.gameObject.name.Replace("HandsOn - ", "");
                    accent = SiteShieldTheme.BrightTeal;
                    break;
                }

                var portal = trans.GetComponentInParent<SitePortal>();
                if (portal != null)
                {
                    foundTarget = portal.transform;
                    action = portal.ReturnsToHub ? "[LMB] RETURN TO HUB" : "[LMB] ENTER WORK ZONE";
                    title = portal.DestinationSite.ToString();
                    accent = portal.ReturnsToHub ? SiteShieldTheme.ActionBlue : SiteShieldTheme.BrandTeal;
                    break;
                }

                var coach = trans.GetComponentInParent<NpcTalkInteractable>();
                if (coach != null)
                {
                    foundTarget = coach.transform;
                    action = "[C] or [LMB] SAFETY COACH";
                    title = "Consult Safety Specialist";
                    accent = SiteShieldTheme.BrightTeal;
                    break;
                }
            }

            if (foundTarget != null)
            {
                currentTarget = foundTarget;
                currentAction = action;
                currentTitle = title;
                currentAccent = accent;
                SetVisible(true);
            }
            else
            {
                currentTarget = null;
                SetVisible(false);
            }

            // Smooth alpha animation
            if (promptGroup != null)
            {
                promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 8f);
            }
        }

        void SetVisible(bool visible)
        {
            targetAlpha = visible ? 1f : 0f;
            if (visible)
            {
                if (actionText != null)
                {
                    actionText.text = currentAction;
                    actionText.color = currentAccent;
                }
                if (objectTitleText != null)
                {
                    objectTitleText.text = currentTitle;
                }
                if (promptIcon != null)
                {
                    promptIcon.color = currentAccent;
                }
            }
        }

        public void Bind(CanvasGroup group, Text action, Text objTitle, Image icon, Image reticle)
        {
            promptGroup = group;
            actionText = action;
            objectTitleText = objTitle;
            promptIcon = icon;
            reticleDot = reticle;
        }
    }
}
