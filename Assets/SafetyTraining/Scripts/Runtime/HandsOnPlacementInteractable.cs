using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace SafetyTraining.Runtime
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody), typeof(XRGrabInteractable))]
    public sealed class HandsOnPlacementInteractable : MonoBehaviour
    {
        const float TransitionDuration = 0.28f;

        [SerializeField] Vector3 targetLocalPosition;
        [SerializeField] Vector3 targetLocalEulerAngles;
        [SerializeField, Min(0.1f)] float acceptanceRadius = 0.85f;
        [SerializeField] GameObject targetVisual;

        XRGrabInteractable grabInteractable;
        Rigidbody body;
        Vector3 startLocalPosition;
        Quaternion startLocalRotation;
        Plane desktopDragPlane;
        Vector3 desktopDragOffset;
        bool desktopDragging;
        bool configured;

        public Vector3 TargetLocalPosition => targetLocalPosition;
        public float AcceptanceRadius => acceptanceRadius;
        public float DistanceToTarget => Vector3.Distance(
            new Vector3(transform.localPosition.x, 0f, transform.localPosition.z),
            new Vector3(targetLocalPosition.x, 0f, targetLocalPosition.z));
        public bool IsWithinTarget => DistanceToTarget <= acceptanceRadius;
        public GameObject TargetVisual => targetVisual;

        void Awake()
        {
            CacheComponents();
            CaptureStartPose();
            targetVisual?.SetActive(false);
        }

        void OnEnable()
        {
            CacheComponents();
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabbed);
                grabInteractable.selectExited.RemoveListener(OnReleased);
            }
            if (desktopDragging)
                ReturnToStart();
            desktopDragging = false;
        }

        public void Configure(Vector3 destination, Vector3 destinationEulerAngles, float radius,
            GameObject visual)
        {
            targetLocalPosition = destination;
            targetLocalEulerAngles = destinationEulerAngles;
            acceptanceRadius = Mathf.Max(0.1f, radius);
            targetVisual = visual;
            configured = true;
            CaptureStartPose();
            targetVisual?.SetActive(false);
        }

        LineRenderer targetRing;
        TextMesh targetLabel;
        static readonly Color RingNormal = new Color(0.133f, 0.827f, 0.773f); // #22D3C5
        static readonly Color RingInside = new Color(0.180f, 0.800f, 0.443f); // #2ECC71

        void Update()
        {
            if (targetVisual != null && targetVisual.activeSelf)
            {
                if (targetRing == null)
                    targetRing = targetVisual.GetComponent<LineRenderer>();
                if (targetLabel == null)
                    targetLabel = targetVisual.GetComponentInChildren<TextMesh>(true);

                var c = IsWithinTarget ? RingInside : RingNormal;
                if (targetRing != null)
                {
                    targetRing.startColor = c;
                    targetRing.endColor = c;
                }
                if (targetLabel != null)
                {
                    targetLabel.color = c;
                }
            }
        }

        public void SetAvailable(bool available)
        {
            CacheComponents();
            grabInteractable.enabled = available;
            if (targetVisual != null)
                targetVisual.SetActive(available);
            var highlight = GetComponent<ObjectiveHighlight>();
            if (highlight != null)
                highlight.enabled = available;
            var marker = GetComponentInChildren<ObjectiveDistanceMarker>(true);
            if (marker != null)
                marker.gameObject.SetActive(available);
        }

        public bool BeginDesktopDrag(Ray pointerRay)
        {
            if (!CanManipulate())
                return false;

            desktopDragPlane = new Plane(Vector3.up, transform.position);
            if (!desktopDragPlane.Raycast(pointerRay, out var enter))
                return false;

            desktopDragOffset = transform.position - pointerRay.GetPoint(enter);
            desktopDragging = true;
            return true;
        }

        public void UpdateDesktopDrag(Ray pointerRay)
        {
            if (!desktopDragging || !desktopDragPlane.Raycast(pointerRay, out var enter))
                return;

            transform.position = pointerRay.GetPoint(enter) + desktopDragOffset;
        }

        public void EndDesktopDrag()
        {
            if (!desktopDragging)
                return;

            desktopDragging = false;
            SubmitPlacement("DesktopDrag");
        }

        public void CancelDesktopDrag()
        {
            if (!desktopDragging)
                return;
            desktopDragging = false;
            ReturnToStart();
        }

        public void SubmitCurrentPosition(string inputMode = "Automated")
        {
            SubmitPlacement(inputMode);
        }

        public void CompletePlacement()
        {
            targetVisual?.SetActive(false);
            grabInteractable.enabled = false;
            var highlight = GetComponent<ObjectiveHighlight>();
            if (highlight != null)
                highlight.enabled = false;
            var marker = GetComponentInChildren<ObjectiveDistanceMarker>(true);
            if (marker != null)
                marker.gameObject.SetActive(false);
            MoveToPose(targetLocalPosition, Quaternion.Euler(targetLocalEulerAngles));
        }

        void CacheComponents()
        {
            grabInteractable ??= GetComponent<XRGrabInteractable>();
            body ??= GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;
            grabInteractable.throwOnDetach = false;
        }

        void CaptureStartPose()
        {
            if (!configured && startLocalRotation != default)
                return;
            startLocalPosition = transform.localPosition;
            startLocalRotation = transform.localRotation;
        }

        bool CanManipulate()
        {
            var siteAction = GetComponent<SitePracticalAction>();
            if (siteAction != null)
                return siteAction.CanManipulate;
            var constructionAction = GetComponent<ConstructionActionInteractable>();
            return constructionAction != null && constructionAction.CanManipulate;
        }

        void SubmitPlacement(string inputMode)
        {
            var siteAction = GetComponent<SitePracticalAction>();
            if (siteAction != null)
            {
                siteAction.TryPerform(inputMode);
                if (!siteAction.Completed)
                    ReturnToStart();
                return;
            }

            var constructionAction = GetComponent<ConstructionActionInteractable>();
            if (constructionAction == null)
                return;
            constructionAction.TryPerform(inputMode);
            if (!constructionAction.Completed)
                ReturnToStart();
        }

        void OnGrabbed(SelectEnterEventArgs _)
        {
            if (!CanManipulate())
                ReturnToStart();
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (!args.isCanceled)
                SubmitPlacement("XRGrab");
            else
                ReturnToStart();
        }

        void ReturnToStart()
        {
            MoveToPose(startLocalPosition, startLocalRotation);
        }

        void MoveToPose(Vector3 localPosition, Quaternion localRotation)
        {
            StopAllCoroutines();
            if (Application.isPlaying && gameObject.activeInHierarchy)
                StartCoroutine(AnimateToPose(localPosition, localRotation));
            else
                transform.SetLocalPositionAndRotation(localPosition, localRotation);
        }

        IEnumerator AnimateToPose(Vector3 localPosition, Quaternion localRotation)
        {
            var fromPosition = transform.localPosition;
            var fromRotation = transform.localRotation;
            var elapsed = 0f;
            while (elapsed < TransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / TransitionDuration));
                transform.localPosition = Vector3.Lerp(fromPosition, localPosition, progress);
                transform.localRotation = Quaternion.Slerp(fromRotation, localRotation, progress);
                yield return null;
            }
            transform.SetLocalPositionAndRotation(localPosition, localRotation);
        }
    }
}
