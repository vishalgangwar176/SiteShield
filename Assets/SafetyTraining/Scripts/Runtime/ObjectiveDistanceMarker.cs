using UnityEngine;

namespace SafetyTraining.Runtime
{
    public class ObjectiveDistanceMarker : MonoBehaviour
    {
        Camera mainCamera;
        TextMesh labelMesh;

        void Awake()
        {
            mainCamera = Camera.main;
            labelMesh = GetComponentInChildren<TextMesh>(true);
        }

        void Update()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            var dist = Vector3.Distance(transform.position, mainCamera.transform.position);

            // Billboard towards camera
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up);

            // Shrink as learner gets close (1.5m -> 0.42 scale, 8m -> 1.15 scale)
            var scale = Mathf.Clamp(dist * 0.16f, 0.42f, 1.15f);
            transform.localScale = Vector3.one * scale;

            if (labelMesh == null)
                labelMesh = GetComponentInChildren<TextMesh>(true);

            if (labelMesh != null)
            {
                labelMesh.text = dist <= 1.3f ? "▼ READY" : $"▼ {dist:F1}m";
            }
        }
    }
}
