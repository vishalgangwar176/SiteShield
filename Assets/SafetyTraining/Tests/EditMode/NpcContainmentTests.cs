using System.Reflection;
using NUnit.Framework;
using SafetyTraining.Runtime;
using UnityEngine;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class NpcContainmentTests
    {
        [Test]
        public void FollowMovement_WhenSolidWallBlocksTarget_DoesNotCrossWall()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Test Perimeter Wall";
            wall.transform.position = Vector3.zero;
            wall.transform.localScale = new Vector3(0.2f, 3f, 4f);

            var coach = new GameObject("Test Coach");
            coach.AddComponent<CapsuleCollider>().height = 1.9f;
            var companion = coach.AddComponent<NpcSiteCompanion>();
            coach.transform.position = new Vector3(-1f, 0f, 0f);
            Physics.SyncTransforms();
            var moveToward = typeof(NpcSiteCompanion).GetMethod("MoveToward",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(moveToward, Is.Not.Null);
            Time.captureDeltaTime = 1f;
            var finalX = coach.transform.position.x;
            try
            {
                moveToward.Invoke(companion, new object[] { new Vector3(1f, 0f, 0f), 10f });
                finalX = coach.transform.position.x;
            }
            finally
            {
                Time.captureDeltaTime = 0f;
                Object.DestroyImmediate(coach);
                Object.DestroyImmediate(wall);
            }

            Assert.That(finalX, Is.LessThan(-0.38f),
                "The coach crossed a solid perimeter wall while following the learner.");
        }
    }
}
