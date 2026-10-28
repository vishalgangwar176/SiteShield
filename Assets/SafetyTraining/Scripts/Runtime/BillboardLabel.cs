using UnityEngine;

namespace SafetyTraining.Runtime
{
    public sealed class BillboardLabel : MonoBehaviour
    {
        void LateUpdate()
        {
            var viewer = Camera.main;
            if (viewer != null)
                transform.rotation = viewer.transform.rotation;
        }
    }
}
