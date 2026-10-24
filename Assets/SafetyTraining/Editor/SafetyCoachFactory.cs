using SafetyTraining.Core;
using SafetyTraining.Runtime;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Editor
{
    internal static class SafetyCoachFactory
    {
        public const string ConstructionCoachPath =
            "Assets/ThirdParty/MicrosoftRocketbox/Construction_Female_01/Export/Construction_Female_01.fbx";
        public const string FireCoachPath =
            "Assets/ThirdParty/MicrosoftRocketbox/Fire_Female_01/Export/Fire_Female_01.fbx";
        const string ControllerPath = "Assets/SafetyTraining/Generated/NpcLocomotion.controller";
        const string IdlePath =
            "Assets/ThirdParty/UnityPeopleSansPeople/Animations/Stand--Idle.anim.fbx";
        const string WalkPath =
            "Assets/ThirdParty/UnityPeopleSansPeople/Animations/Locomotion--Walk_N.anim.fbx";

        public static void Create(Transform parent, TrainingSiteId siteId, string role, string facts,
            string coachPath)
        {
            var coach = InstantiateModel(parent, coachPath, out var animator);
            CoachPpeBuilder.Apply(siteId, coach, animator);
            ConfigureCollider(coach);
            coach.AddComponent<XRSimpleInteractable>();
            var speakGlow = coach.AddComponent<NpcSpeakGlow>();
            speakGlow.parts = coach.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.GetComponent<TextMesh>() == null && r.GetComponent<BillboardLabel>() == null).ToArray();
            foreach (var r in speakGlow.parts)
            {
                var mat = r.sharedMaterial;
                if (mat != null)
                {
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }
            coach.AddComponent<InteractiveHoverFeedback>();
            coach.AddComponent<NpcRelaxedPose>();
            var agent = coach.AddComponent<NpcConversationAgent>();
            agent.Configure(siteId, role, facts);
            var speechBubble = ProfessionalSpeechBubbleBuilder.Create(coach.transform);
            var siteTitle = parent.GetComponentsInChildren<TextMesh>()
                .FirstOrDefault(label => label.text == parent.name);
            coach.AddComponent<NpcTalkInteractable>().Configure(agent, speechBubble, siteTitle);
            coach.AddComponent<NpcSiteCompanion>().Configure(siteId);
        }

        static GameObject InstantiateModel(Transform parent, string coachPath, out Animator animator)
        {
            var coachAsset = AssetDatabase.LoadAssetAtPath<GameObject>(coachPath);
            if (coachAsset == null)
            {
                animator = null;
                return SafetyScenePrimitives.Primitive(PrimitiveType.Capsule, "Coach Placeholder", parent,
                    new Vector3(0f, 1f, 2.8f), new Vector3(0.7f, 1f, 0.7f), Color.gray);
            }

            var coach = (GameObject)PrefabUtility.InstantiatePrefab(coachAsset, parent);
            coach.name = "Rocketbox Safety Coach";
            coach.transform.localPosition = new Vector3(0f, 0f, -3.7f);
            coach.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var primaryModel = coach.GetComponentInChildren<SkinnedMeshRenderer>(true);
            for (var current = primaryModel?.transform; current != null; current = current.parent)
            {
                current.gameObject.SetActive(true);
                if (current == coach.transform)
                    break;
            }

            animator = coach.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.runtimeAnimatorController = CreateLocomotionController();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            return coach;
        }

        static void ConfigureCollider(GameObject coach)
        {
            var collider = coach.GetComponent<Collider>();
            if (collider == null)
                collider = coach.AddComponent<CapsuleCollider>();
            if (collider is CapsuleCollider capsule)
            {
                var renderers = coach.GetComponentsInChildren<Renderer>(true)
                    .Where(item => item.enabled).ToArray();
                if (renderers.Length > 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var item in renderers.Skip(1))
                        bounds.Encapsulate(item.bounds);
                    var scale = coach.transform.lossyScale;
                    capsule.direction = 1;
                    capsule.center = coach.transform.InverseTransformPoint(bounds.center);
                    capsule.height = Mathf.Max(1.8f, bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)));
                    var horizontalScale = Mathf.Max(0.001f,
                        Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)));
                    capsule.radius = Mathf.Clamp(
                        Mathf.Max(bounds.size.x, bounds.size.z) / (2f * horizontalScale), 0.35f, 0.65f);
                }
            }
            collider.isTrigger = false;
        }

        static RuntimeAnimatorController CreateLocomotionController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null)
                return existing;

            var idle = LoadClip(IdlePath, "Idle");
            var walk = LoadClip(WalkPath, "Walk");
            if (idle == null || walk == null)
            {
                Debug.LogError("PeopleSansPeople locomotion clips were not imported.");
                return null;
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            var stateMachine = controller.layers[0].stateMachine;
            var idleState = stateMachine.AddState("Natural Idle");
            idleState.motion = idle;
            var walkState = stateMachine.AddState("Natural Walk");
            walkState.motion = walk;
            stateMachine.defaultState = idleState;
            var startWalking = idleState.AddTransition(walkState);
            startWalking.hasExitTime = false;
            startWalking.duration = 0.22f;
            startWalking.AddCondition(AnimatorConditionMode.If, 0f, "Moving");
            var stopWalking = walkState.AddTransition(idleState);
            stopWalking.hasExitTime = false;
            stopWalking.duration = 0.2f;
            stopWalking.AddCondition(AnimatorConditionMode.IfNot, 0f, "Moving");
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        static AnimationClip LoadClip(string path, string preferredName)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name.Contains(preferredName) &&
                    !clip.name.StartsWith("__preview__")) ??
                AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
        }
    }
}
