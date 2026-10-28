using UnityEngine;

namespace SafetyTraining.Runtime
{
    [DefaultExecutionOrder(-950)]
    public sealed class StartupGroundingGuard : MonoBehaviour
    {
        [SerializeField] float safeSpawnHeight = 0.02f;
        [SerializeField] float fallRecoveryHeight = -0.25f;
        Vector3 safeSpawn;

        void Awake()
        {
            safeSpawn = transform.position;
            safeSpawn.y = Mathf.Max(safeSpawn.y, safeSpawnHeight);
            transform.position = safeSpawn;
        }

        void LateUpdate()
        {
            if (transform.position.y >= fallRecoveryHeight)
                return;

            var recovery = transform.position;
            recovery.y = safeSpawn.y;
            transform.position = recovery;
        }

        public void SetSafeSpawn(Vector3 position)
        {
            safeSpawn = position;
            safeSpawn.y = Mathf.Max(safeSpawn.y, safeSpawnHeight);
        }
    }
}
