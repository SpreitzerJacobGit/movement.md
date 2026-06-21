using UnityEngine;
using Simulation.Core;
using Simulation.Math;

namespace Presentation.Input
{
    /// <summary>
    /// Captures Unity input each frame and forwards it to the simulation via the
    /// bridge's single sanctioned write path (ApplyInput). All float->fixed
    /// conversion happens here at the boundary, so no float ever enters the sim.
    /// </summary>
    public sealed class InputCapture : MonoBehaviour
    {
        [SerializeField] private SimulationBridge _simulationBridge;
        [SerializeField] private Camera _camera;

        private void Update()
        {
            if (_simulationBridge == null)
            {
                return;
            }

            PlayerInputs inputs = CaptureInputs();
            _simulationBridge.SendPlayerInputs(inputs);
        }

        private PlayerInputs CaptureInputs()
        {
            Fixed moveX = ToFixed(Input.GetAxis("Horizontal"));
            Fixed moveZ = ToFixed(Input.GetAxis("Vertical"));
            Fixed aimYaw = ToFixed(Input.GetAxis("Mouse X"));
            Fixed aimPitch = ToFixed(Input.GetAxis("Mouse Y"));

            bool jump = Input.GetButton("Jump");
            bool slide = Input.GetButton("Slide");
            bool grapple = Input.GetButton("Grapple");
            bool spinRecord = Input.GetButton("SpinRecord");
            bool spinDischarge = Input.GetButton("SpinDischarge");
            bool fire = Input.GetButton("Fire");

            FixedVec3 aimPoint = CaptureAimPoint();

            return new PlayerInputs(
                moveX, moveZ, aimYaw, aimPitch,
                jump, slide, grapple,
                aimPoint,
                spinRecord, spinDischarge, fire);
        }

        private FixedVec3 CaptureAimPoint()
        {
            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
            {
                return default;
            }

            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return ToFixedVec3(hit.point);
            }

            return default;
        }

        private static Fixed ToFixed(float f) => Fixed.FromDouble(f);

        private static FixedVec3 ToFixedVec3(Vector3 v)
            => new FixedVec3(ToFixed(v.x), ToFixed(v.y), ToFixed(v.z));
    }
}
