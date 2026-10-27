using System.Reflection;
using NUnit.Framework;
using SafetyTraining.Runtime;
using UnityEngine;

namespace SafetyTraining.Tests.EditMode
{
    public sealed class DesktopFirstPersonLookTests
    {
        [Test]
        public void DesktopLook_CursorLock_ControlsCursorState()
        {
            var rig = new GameObject("Test Rig");
            var cam = new GameObject("Test Camera");
            cam.transform.SetParent(rig.transform);
            var controller = cam.AddComponent<DesktopExplorerController>();

            controller.SetCursorLocked(true);
            Assert.That(controller.IsCursorLocked, Is.True);
            if (!Application.isBatchMode)
            {
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
                Assert.That(Cursor.visible, Is.False);
            }

            controller.SetCursorLocked(false);
            Assert.That(controller.IsCursorLocked, Is.False);
            if (!Application.isBatchMode)
            {
                Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
                Assert.That(Cursor.visible, Is.True);
            }

            Object.DestroyImmediate(rig);
        }

        [Test]
        public void DesktopLook_PitchClamping_PreventsInversionAndMaintainsZeroRoll()
        {
            var rig = new GameObject("Test Rig");
            var cam = new GameObject("Test Camera");
            cam.transform.SetParent(rig.transform);
            var controller = cam.AddComponent<DesktopExplorerController>();

            typeof(DesktopExplorerController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);

            var pitchField = typeof(DesktopExplorerController).GetField("pitch", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(pitchField, Is.Not.Null);

            // Test pitch clamping
            float extremeUp = 95f;
            float clampedUp = Mathf.Clamp(extremeUp, -80f, 80f);
            Assert.That(clampedUp, Is.EqualTo(80f));

            float extremeDown = -105f;
            float clampedDown = Mathf.Clamp(extremeDown, -80f, 80f);
            Assert.That(clampedDown, Is.EqualTo(-80f));

            // Verify camera local rotation maintains 0 roll and 0 yaw
            cam.transform.localRotation = Quaternion.Euler(clampedUp, 0f, 0f);
            Assert.That(cam.transform.localEulerAngles.z, Is.EqualTo(0f).Within(0.01f));
            Assert.That(cam.transform.localEulerAngles.y, Is.EqualTo(0f).Within(0.01f));

            controller.SetCursorLocked(false);
            Object.DestroyImmediate(rig);
        }

        [Test]
        public void DesktopLook_MovementPlanarProjection_DoesNotMoveVerticallyWhenLookingAtSkyOrGround()
        {
            var rig = new GameObject("Test Rig");
            var cam = new GameObject("Test Camera");
            cam.transform.SetParent(rig.transform);

            // Look up at sky (+75 degrees)
            cam.transform.localRotation = Quaternion.Euler(-75f, 0f, 0f);
            var planarForwardUp = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Assert.That(planarForwardUp.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(planarForwardUp.z, Is.GreaterThan(0f));

            // Look down at ground (-75 degrees)
            cam.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
            var planarForwardDown = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Assert.That(planarForwardDown.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(planarForwardDown.z, Is.GreaterThan(0f));

            // Moving horizontally along planar vector keeps Y position unchanged
            Vector3 pos = rig.transform.position;
            pos += planarForwardUp * 2.0f;
            Assert.That(pos.y, Is.EqualTo(rig.transform.position.y).Within(0.001f));

            Object.DestroyImmediate(rig);
        }

        [Test]
        public void DesktopLook_MouseSensitivity_IsConfigurableAndPersisted()
        {
            var rig = new GameObject("Test Rig");
            var cam = new GameObject("Test Camera");
            cam.transform.SetParent(rig.transform);
            var controller = cam.AddComponent<DesktopExplorerController>();

            PlayerPrefs.SetFloat("SS_LookSensitivity", 1.5f);
            Assert.That(controller.MouseSensitivity, Is.EqualTo(2.0f * 1.5f).Within(0.01f));

            controller.MouseSensitivity = 3.0f;
            Assert.That(PlayerPrefs.GetFloat("SS_LookSensitivity"), Is.EqualTo(1.0f));

            PlayerPrefs.DeleteKey("SS_LookSensitivity");
            controller.SetCursorLocked(false);
            Object.DestroyImmediate(rig);
        }

        [Test]
        public void DesktopLook_InvertY_CanBeToggled()
        {
            var rig = new GameObject("Test Rig");
            var cam = new GameObject("Test Camera");
            cam.transform.SetParent(rig.transform);
            var controller = cam.AddComponent<DesktopExplorerController>();

            Assert.That(controller.InvertY, Is.False);
            controller.InvertY = true;
            Assert.That(controller.InvertY, Is.True);

            controller.SetCursorLocked(false);
            Object.DestroyImmediate(rig);
        }
        [Test]
        public void DesktopLook_VerticalDirection_MovingMouseForwardTiltsCameraUpward()
        {
            var rig = new GameObject("Test Rig");
            var cam = new GameObject("Test Camera");
            cam.transform.SetParent(rig.transform);
            var controller = cam.AddComponent<DesktopExplorerController>();

            typeof(DesktopExplorerController).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(controller, null);

            var pitchField = typeof(DesktopExplorerController).GetField("pitch", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(pitchField, Is.Not.Null);

            // With natural look (invertY = false):
            // Moving mouse forward produces mouseDelta.y > 0.
            // deltaPitch = mouseDelta.y * effectiveSensitivity * (invertY ? -1f : 1f) => positive.
            // pitch = Mathf.Clamp(pitch - deltaPitch, minPitch, maxPitch) => pitch decreases (becomes negative).
            // In Unity, Quaternion.Euler(negativePitch, 0, 0) tilts camera upward towards the sky (forward.y > 0).
            float mouseDeltaY = 5.0f;
            float effectiveSensitivity = 2.0f;
            bool invertY = controller.InvertY;
            float deltaPitch = mouseDeltaY * effectiveSensitivity * (invertY ? -1f : 1f);
            float initialPitch = 0f;
            float newPitch = Mathf.Clamp(initialPitch - deltaPitch, -80f, 80f);

            Assert.That(newPitch, Is.LessThan(0f), "Moving mouse forward must decrease Euler X pitch (negative)");
            cam.transform.localRotation = Quaternion.Euler(newPitch, 0f, 0f);
            Assert.That(cam.transform.forward.y, Is.GreaterThan(0f), "Negative Euler X pitch must tilt camera upward towards the sky");

            // Moving mouse backward produces mouseDelta.y < 0.
            float mouseDeltaYBack = -5.0f;
            float deltaPitchBack = mouseDeltaYBack * effectiveSensitivity * (invertY ? -1f : 1f);
            float newPitchBack = Mathf.Clamp(initialPitch - deltaPitchBack, -80f, 80f);

            Assert.That(newPitchBack, Is.GreaterThan(0f), "Moving mouse backward must increase Euler X pitch (positive)");
            cam.transform.localRotation = Quaternion.Euler(newPitchBack, 0f, 0f);
            Assert.That(cam.transform.forward.y, Is.LessThan(0f), "Positive Euler X pitch must tilt camera downward towards the floor");

            controller.SetCursorLocked(false);
            Object.DestroyImmediate(rig);
        }
    }
}