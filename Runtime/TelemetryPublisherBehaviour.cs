#nullable enable
using System;
using UnityEngine;
using Core = Ogsa.Telemetry;
using CoreVector3 = System.Numerics.Vector3;
using CoreQuaternion = System.Numerics.Quaternion;

namespace Ogsa.Telemetry.Unity
{
    /// <summary>
    /// OGSA-TS publisher for one vehicle/object. Configure transport and
    /// cadence in the Inspector, declare channels in Awake/Start, then bind
    /// game values (pull) or push per tick — declarations lock at the first
    /// tick (start of the first LateUpdate after Start).
    ///
    /// Unity's world frame (left-handed, Z-forward, Y-up) matches the
    /// OGSA-TS frame natively — pose values pass straight through with no
    /// axis remap.
    /// </summary>
    [AddComponentMenu("OGSA-TS/Telemetry Publisher")]
    [DisallowMultipleComponent]
    public sealed class TelemetryPublisherBehaviour : MonoBehaviour
    {
        [Header("Transport")]
        [Tooltip("Multicast group (OGSA-TS default 239.255.17.17)")]
        public string multicastGroup = "239.255.17.17";
        [Tooltip("Multicast port (OGSA-TS default 40123)")]
        public int port = 40123;
        [Tooltip("Multicast TTL — 1 keeps traffic on the local subnet")]
        public int ttl = 1;

        [Header("Identity")]
        [Tooltip("Sender name shown in discovery; defaults to the product name")]
        public string gameName = "";
        [Tooltip("Object name in discovery (e.g. player_car)")]
        public string objectName = "player";
        public string typeLabel = "";

        [Header("Cadence")]
        [Range(1, 240)] public double motionHz = 60.0;
        [Min(0.5f)] public double discoveryIntervalSeconds = 2.0;

        [Header("Auto-bind")]
        [Tooltip("If set, the object's world pose is bound automatically")]
        public Rigidbody? autoBindRigidbody;

        Core.TelemetryPublisher? _publisher;
        Core.TelemetrySender? _sender;
        bool _tornDown;

        /// <summary>The engine-agnostic publisher (advanced use).</summary>
        public Core.TelemetryPublisher? Publisher => _publisher;

        void Awake()
        {
            _sender = new Core.TelemetrySender(
                string.IsNullOrEmpty(multicastGroup) ? "239.255.17.17" : multicastGroup,
                port, ttl, loopback: true);
            _publisher = new Core.TelemetryPublisher(
                _sender,
                string.IsNullOrEmpty(gameName) ? Application.productName : gameName,
                motionHz, discoveryIntervalSeconds);
            if (!string.IsNullOrEmpty(objectName))
                _publisher.DeclareObject(objectName, typeLabel);
        }

        void Start()
        {
            if (autoBindRigidbody != null && _publisher != null)
            {
                var rb = autoBindRigidbody;
                _publisher.BindPose(
                    () => ToCore(rb.position),
                    () => ToCore(rb.rotation));
            }
        }

        void LateUpdate() => _publisher?.Tick(Time.timeAsDouble);

        void OnDestroy()
        {
            if (_tornDown) return;
            _tornDown = true;
            _publisher?.Dispose();
            _sender?.Dispose();
        }

        // ---- declarations (Awake/Start only; locked at first tick) ----

        public Core.ChannelHandle DeclareChannel(string name, Core.Unit unit,
                                                 string description = "",
                                                 double min = double.NaN,
                                                 double max = double.NaN)
            => _publisher.DeclareChannel(name, unit, description, min, max);

        public Core.ChannelHandle DeclareBooleanChannel(string name,
                                                        string description = "")
            => _publisher.DeclareBooleanChannel(name, description);

        public void AddWheel(string name, Vector3 offset, float radius,
                             bool driven, bool steered,
                             float stiffness = 65000f, float damping = 5000f)
            => _publisher.AddWheel(name, ToCore(offset), radius, driven, steered,
                                   stiffness, damping);

        // ---- bindings (pull model) ----

        /// <summary>Binds pose getters in Unity's world frame (direct map —
        /// Unity is already left-handed, Z-forward, Y-up).</summary>
        public void BindPose(Func<Vector3>? position, Func<Quaternion>? orientation)
            => _publisher.BindPose(
                position is null ? null : () => ToCore(position()),
                orientation is null ? null : () => ToCore(orientation()));

        /// <summary>Binds a numeric channel; the getter returns values in
        /// sourceUnit (what the game natively measures) and the factor to
        /// the channel's target unit is precomputed at bind.</summary>
        public void BindChannel(Core.ChannelHandle channel, Func<double> source,
                                Core.Unit sourceUnit)
            => _publisher.BindChannel(channel, source, sourceUnit);

        public void BindChannelRaw(Core.ChannelHandle channel, Func<double> source)
            => _publisher.BindChannelRaw(channel, source);

        public void BindBooleanChannel(Core.ChannelHandle channel, Func<bool> source)
            => _publisher.BindBooleanChannel(channel, source);

        // ---- push model ----

        public void SetChannel(Core.ChannelHandle channel, double value)
            => _publisher.SetChannel(channel, value);

        public void SetChannel(Core.ChannelHandle channel, bool value)
            => _publisher.SetChannel(channel, value);

        public void FireEvent(string name, float intensity = 1f, float duration = 0f,
                              Vector3 direction = default)
            => _publisher.FireEvent(name, intensity, duration, ToCore(direction));

        // Unity's numeric types map 1:1 onto the core's System.Numerics —
        // same axes, same order; this is a plain component copy.
        static CoreVector3 ToCore(Vector3 v) =>
            new(v.x, v.y, v.z);
        static CoreQuaternion ToCore(Quaternion q) =>
            new(q.x, q.y, q.z, q.w);
    }
}
