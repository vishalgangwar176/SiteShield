using SafetyTraining.Core;
using UnityEngine;

namespace SafetyTraining.Runtime
{
    [RequireComponent(typeof(NpcTalkInteractable))]
    public sealed class NpcSiteCompanion : MonoBehaviour
    {
        [SerializeField] TrainingSiteId siteId;
        [SerializeField, Min(0.5f)] float sideOffset = 1.35f;
        [SerializeField, Min(0f)] float frontOffset = 2f;
        [SerializeField, Min(0.1f)] float walkSpeed = 3.2f;
        [SerializeField, Min(0.1f)] float catchUpSpeed = 6.25f;
        [SerializeField, Min(0.5f)] float catchUpDistance = 1.8f;
        [SerializeField, Min(1f)] float recoveryDistance = 8f;
        [SerializeField, Min(1f)] float chatRange = 3.2f;
        [SerializeField, Min(0.1f)] float followStartDistance = 1.35f;
        [SerializeField, Min(0.05f)] float followStopDistance = 0.42f;
        [SerializeField, Min(0.05f)] float playerMotionThreshold = 0.18f;

        Camera viewer;
        NpcTalkInteractable talk;
        NpcRelaxedPose pose;
        SitePortal returnPortal;
        Vector3 homePosition;
        Quaternion homeRotation;
        bool accompanying;
        bool moving;
        Vector3 safeTarget;
        Vector3 lastPlayerPosition;
        float nextSafetySample;
        readonly RaycastHit[] movementHits = new RaycastHit[16];

        static readonly Vector3[] SafetyOffsets =
        {
            Vector3.zero,
            new Vector3(0.9f, 0f, 0f), new Vector3(-0.9f, 0f, 0f),
            new Vector3(0f, 0f, 0.9f), new Vector3(0f, 0f, -0.9f),
            new Vector3(0.9f, 0f, 0.9f), new Vector3(-0.9f, 0f, 0.9f),
            new Vector3(0.9f, 0f, -0.9f), new Vector3(-0.9f, 0f, -0.9f),
            new Vector3(1.8f, 0f, 0f), new Vector3(-1.8f, 0f, 0f),
            new Vector3(0f, 0f, 1.8f), new Vector3(0f, 0f, -1.8f),
            new Vector3(3.2f, 0f, 0f), new Vector3(-3.2f, 0f, 0f),
            new Vector3(3.2f, 0f, -1.2f), new Vector3(-3.2f, 0f, -1.2f),
            new Vector3(0f, 0f, -3.2f)
        };

        public TrainingSiteId SiteId => siteId;

        void Awake()
        {
            viewer = Camera.main;
            talk = GetComponent<NpcTalkInteractable>();
            pose = GetComponent<NpcRelaxedPose>();
            homePosition = transform.position;
            homeRotation = transform.rotation;
            homePosition = FindClearPosition(homePosition, homePosition);
            transform.position = homePosition;
            safeTarget = homePosition;
            if (viewer != null)
                lastPlayerPosition = Flatten(viewer.transform.position);
            var zone = GetComponentInParent<SiteExperienceZone>();
            zone?.BindCompanion(this);
            ResolveReturnPortal(zone);
        }

        void OnEnable()
        {
            if (TrainingCoordinator.Instance != null)
                TrainingCoordinator.Instance.InspectionCompleted += ReactToInspection;
        }

        void OnDisable()
        {
            if (TrainingCoordinator.Instance != null)
                TrainingCoordinator.Instance.InspectionCompleted -= ReactToInspection;
        }

        void Update()
        {
            if (viewer == null)
                viewer = Camera.main;
            if (viewer == null)
                return;

            if (!accompanying)
            {
                MoveToward(homePosition, walkSpeed);
                return;
            }

            if (talk != null && talk.ConversationActive)
            {
                moving = false;
                pose?.SetMoving(false);
                FaceViewer();
                return;
            }

            var forward = Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up).normalized;
            var right = Vector3.ProjectOnPlane(viewer.transform.right, Vector3.up).normalized;
            var desired = viewer.transform.position + right * sideOffset + forward * frontOffset;
            desired.y = homePosition.y;
            var playerPosition = Flatten(viewer.transform.position);
            var playerMoved = Vector3.Distance(playerPosition, lastPlayerPosition) >= playerMotionThreshold;
            if (Time.unscaledTime >= nextSafetySample && (playerMoved ||
                Vector3.Distance(transform.position, desired) > recoveryDistance))
            {
                nextSafetySample = Time.unscaledTime + 0.18f;
                safeTarget = FindClearVisiblePosition(desired, transform.position, playerPosition, forward, right);
                lastPlayerPosition = playerPosition;
            }
            var separation = Vector3.Distance(transform.position, safeTarget);
            if (separation > recoveryDistance)
            {
                moving = true;
                MoveToward(safeTarget, catchUpSpeed);
                return;
            }

            if (!moving && separation >= followStartDistance)
                moving = true;
            else if (moving && separation <= followStopDistance)
                moving = false;

            if (moving)
                MoveToward(safeTarget, separation >= catchUpDistance ? catchUpSpeed : walkSpeed);
            else
            {
                pose?.SetMoving(false);
                FaceViewer();
            }
        }

        public void Configure(TrainingSiteId value)
        {
            siteId = value;
            homePosition = FindClearPosition(transform.position, transform.position);
            transform.position = homePosition;
            safeTarget = homePosition;
        }

        public void SetAccompanying(bool value)
        {
            accompanying = value;
            if (viewer != null)
            {
                lastPlayerPosition = Flatten(viewer.transform.position);
                if (value)
                {
                    ResolveReturnPortal(GetComponentInParent<SiteExperienceZone>());
                    var forward = Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up).normalized;
                    var right = Vector3.ProjectOnPlane(viewer.transform.right, Vector3.up).normalized;
                    var desired = viewer.transform.position + right * sideOffset + forward * frontOffset;
                    desired.y = homePosition.y;
                    safeTarget = FindClearVisiblePosition(desired, transform.position,
                        Flatten(viewer.transform.position), forward, right);
                    transform.position = safeTarget;
                    FaceViewer();
                }
            }
            if (!value || viewer == null)
                safeTarget = transform.position;
            moving = false;
            if (!value)
                pose?.SetMoving(false);
        }

        public void TryTalk()
        {
            if (!accompanying || viewer == null)
                return;
            if (Vector3.Distance(transform.position, viewer.transform.position) <= chatRange)
                talk.Ask();
            else
                TrainingCoordinator.Instance?.SetContextFeedback("Your field mentor is moving into conversation range.");
        }

        public void Encourage(bool siteComplete)
        {
            pose?.PlayEncouragement(siteComplete);
        }

        void ReactToInspection(TrainingSiteId inspectedSite, InspectionResult result)
        {
            if (inspectedSite == siteId && result.Outcome == InspectionOutcome.CorrectHazard)
                pose?.PlayEncouragement(result.IsComplete);
        }

        void MoveToward(Vector3 target, float speed)
        {
            var delta = target - transform.position;
            delta.y = 0f;
            if (!accompanying)
                moving = delta.sqrMagnitude > 0.12f;
            pose?.SetMoving(moving);
            if (!moving)
            {
                FaceViewer();
                return;
            }

            var step = Mathf.Min(delta.magnitude, speed * Time.deltaTime);
            var direction = delta.normalized;
            step = LimitStepByCollision(direction, step);
            transform.position += direction * step;
            if (step <= 0.001f)
            {
                moving = false;
                pose?.SetMoving(false);
                return;
            }
            var targetRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 7f);
        }

        float LimitStepByCollision(Vector3 direction, float requestedStep)
        {
            const float radius = 0.38f;
            const float skin = 0.04f;
            var bottom = transform.position + Vector3.up * 0.48f;
            var top = transform.position + Vector3.up * 1.55f;
            var allowedStep = requestedStep;
            var hitCount = Physics.CapsuleCastNonAlloc(bottom, top, radius, direction, movementHits,
                requestedStep + skin, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (var index = 0; index < hitCount; index++)
            {
                var hit = movementHits[index];
                if (hit.collider.transform.IsChildOf(transform) ||
                    hit.collider.GetType().Name == "TerrainCollider" ||
                    hit.collider.name.Contains("Floor") || hit.collider.name.Contains("Ground"))
                    continue;
                allowedStep = Mathf.Min(allowedStep, Mathf.Max(0f, hit.distance - skin));
            }
            return allowedStep;
        }

        void FaceViewer()
        {
            if (!accompanying)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, homeRotation, Time.deltaTime * 3f);
                return;
            }
            var direction = viewer.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up), Time.deltaTime * 4f);
        }

        public bool IsPlacementClear(Vector3 position)
        {
            var bottom = position + Vector3.up * 0.48f;
            var top = position + Vector3.up * 1.55f;
            foreach (var collider in Physics.OverlapCapsule(bottom, top, 0.38f,
                         Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                if (collider.transform.IsChildOf(transform))
                    continue;
                if (collider.GetType().Name == "TerrainCollider" || collider.name.Contains("Floor") ||
                    collider.name.Contains("Ground"))
                    continue;
                return false;
            }
            return true;
        }

        Vector3 FindClearPosition(Vector3 preferred, Vector3 fallback)
        {
            foreach (var offset in SafetyOffsets)
            {
                var candidate = preferred + offset;
                candidate.y = homePosition == Vector3.zero ? preferred.y : homePosition.y;
                if (IsPlacementClear(candidate))
                    return candidate;
            }
            return fallback;
        }

        Vector3 FindClearVisiblePosition(Vector3 preferred, Vector3 fallback,
            Vector3 viewerPosition, Vector3 viewerForward, Vector3 viewerRight)
        {
            for (var forwardStep = 0; forwardStep < 8; forwardStep++)
            {
                for (var sideStep = 0; sideStep < 6; sideStep++)
                {
                    var candidate = viewerPosition + viewerRight * (sideOffset + sideStep * 0.6f) +
                        viewerForward * (frontOffset + forwardStep * 0.7f);
                    candidate.y = homePosition == Vector3.zero ? preferred.y : homePosition.y;
                    var viewerDelta = Flatten(candidate - viewerPosition);
                    var direction = viewerDelta.normalized;
                    if (Vector3.Dot(viewerDelta, viewerRight) < 0.55f ||
                        Vector3.Dot(viewerForward, direction) < 0.85f)
                        continue;
                    if (!IsSeparatedFromReturnPortal(candidate))
                        continue;
                    if (IsPlacementClear(candidate))
                        return candidate;
                }
            }
            return preferred;
        }

        bool IsSeparatedFromReturnPortal(Vector3 candidate)
        {
            if (viewer == null || returnPortal == null ||
                !returnPortal.TryGetComponent<Collider>(out var portalCollider))
                return true;
            if (Vector3.Distance(candidate, portalCollider.bounds.ClosestPoint(candidate)) < 1.8f)
                return false;
            var coachViewport = viewer.WorldToViewportPoint(candidate + Vector3.up);
            var portalViewport = viewer.WorldToViewportPoint(portalCollider.bounds.center);
            return coachViewport.z <= 0f || portalViewport.z <= 0f ||
                Vector2.Distance(coachViewport, portalViewport) >= 0.18f;
        }

        void ResolveReturnPortal(SiteExperienceZone zone)
        {
            if (returnPortal != null || zone == null)
                return;
            foreach (var portal in zone.GetComponentsInChildren<SitePortal>(true))
            {
                if (!portal.ReturnsToHub)
                    continue;
                returnPortal = portal;
                return;
            }
        }

        static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value;
        }
    }
}
