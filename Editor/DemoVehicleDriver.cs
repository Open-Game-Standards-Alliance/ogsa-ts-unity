using UnityEngine;

namespace Ogsa.Telemetry.Unity.Editor
{
    /// <summary>Demo driver: moves a rigidbody cube in a circle and feeds a
    /// fake RPM + speed channel so the demo scene is immediately visible in
    /// a receiver (run ogsa-ts-dotnet's TelemetryReceive beside Unity).</summary>
    public sealed class DemoVehicleDriver : MonoBehaviour
    {
        public float radius = 10f;
        public float angularSpeed = 0.8f; // rad/s
        public float maxRpm = 6500f;

        TelemetryPublisherBehaviour _telemetry;
        Ogsa.Telemetry.ChannelHandle _rpm;
        Ogsa.Telemetry.ChannelHandle _speed;
        Ogsa.Telemetry.ChannelHandle _headlights;
        double _t;
        Rigidbody _rb;

        void Awake()
        {
            _telemetry = GetComponent<TelemetryPublisherBehaviour>();
            _rb = GetComponent<Rigidbody>();
            _rpm = _telemetry.DeclareChannel(
                "engine.rpm", Ogsa.Telemetry.Unit.Rpm,
                "Demo engine speed", 0, 9000);
            _speed = _telemetry.DeclareChannel(
                "vehicle.speed", Ogsa.Telemetry.Unit.MetrePerSecond,
                "Speed over ground", 0, 30);
            _headlights = _telemetry.DeclareBooleanChannel(
                "lights.headlights", "Headlights on/off");

            // game-native units — the publisher converts on the wire:
            // speed measured in km/h here, declared m/s above.
            _telemetry.BindChannel(_speed, () => angularSpeed * radius,
                                   Ogsa.Telemetry.Unit.MetrePerSecond);
            _telemetry.BindChannelRaw(_rpm, () =>
                maxRpm * (0.5 + 0.5 * Mathf.Sin((float)_t * 2f)));
            _telemetry.BindBooleanChannel(_headlights, () => true);
        }

        void FixedUpdate()
        {
            _t += Time.fixedDeltaTime;
            float yaw = (float)(_t * angularSpeed);
            // kinematic demo pose (teleports the rigidbody; fine for wire demo)
            _rb.MovePosition(new Vector3(
                radius * Mathf.Cos(yaw), 0.5f, radius * Mathf.Sin(yaw)));
            _rb.MoveRotation(Quaternion.Euler(0f, -yaw * Mathf.Rad2Deg + 90f, 0f));
        }
    }
}
