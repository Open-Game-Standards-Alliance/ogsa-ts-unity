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
    /// The sender+publisher are constructed lazily on first use (this
    /// component's Awake, or your first API call if your script's Awake ran
    /// earlier — script execution order safe). Unity's world frame
    /// (left-handed, Z-forward, Y-up) matches the OGSA-TS frame natively —
    /// pose values pass straight through with no axis remap.
    ///
    /// Pause (standard CONVENTIONS C-11): the publisher ticks on unscaled
    /// real time, so it keeps streaming while the game is paused and
    /// reports the pause on <c>session.paused</c>. Time.timeScale == 0 is
    /// detected automatically; games that pause another way call
    /// <see cref="SetPaused"/>.
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

        [Tooltip("Emit the bodyDynamics variant (specific force + angular " +
                 "rates — the motion-cueing inputs) derived per tick from " +
                 "the bound body's pose and engine-measured velocity, via " +
                 "the core's BodyDynamicsDeriver (accelerometer-style: " +
                 "heave reads +9.80665 at rest)")]
        public bool sendBodyDynamics = true;

        [Header("Pause")]
        [Tooltip("Report the game as paused (session.paused) while " +
                 "Time.timeScale is 0. Games that pause another way call " +
                 "SetPaused; both sources combine.")]
        public bool autoDetectPause = true;

        Core.TelemetryPublisher? _publisher;
        bool _manualPaused, _wasPaused;
        Core.TelemetrySender? _sender;
        bool _tornDown;
        readonly Core.BodyDynamicsDeriver _deriver = new();

        /// <summary>The pose→dynamics deriver behind sendBodyDynamics — for
        /// manual binders: sample it per tick (Unity frame is OGSA-TS
        /// native, no conversion) then Publisher.BindBodyDynamics(() =>
        /// (deriver.SpecificForce, deriver.AngularVelocity)).</summary>
        public Core.BodyDynamicsDeriver BodyDeriver => _deriver;

        /// <summary>
        /// The engine-agnostic publisher, constructed on first use from the
        /// serialized configuration (also the backing for all convenience
        /// methods here). Throws after this component is destroyed.
        /// </summary>
        public Core.TelemetryPublisher Publisher
        {
            get
            {
                if (_publisher == null)
                {
                    if (_tornDown)
                        throw new ObjectDisposedException(
                            nameof(TelemetryPublisherBehaviour));
                    _sender = new Core.TelemetrySender(
                        string.IsNullOrEmpty(multicastGroup)
                            ? "239.255.17.17" : multicastGroup,
                        port, ttl, loopback: true);
                    _publisher = new Core.TelemetryPublisher(
                        _sender,
                        string.IsNullOrEmpty(gameName)
                            ? Application.productName : gameName,
                        motionHz, discoveryIntervalSeconds);
                }
                return _publisher;
            }
        }

        void Awake()
        {
            if (!string.IsNullOrEmpty(objectName))
                Publisher.DeclareObject(objectName, typeLabel);
            Publisher.DeclarePause();
        }

        /// <summary>Reports a pause the engine can't see (a game-managed
        /// pause that leaves Time.timeScale running). Combines with
        /// autoDetectPause: the game is paused if either says so.</summary>
        public void SetPaused(bool paused) => _manualPaused = paused;

        /// <summary>Whether the publisher is currently reporting a pause.</summary>
        public bool Paused => _publisher?.Paused ?? false;

        void Start()
        {
            if (autoBindRigidbody != null)
            {
                var rb = autoBindRigidbody;
                Publisher.BindPose(
                    () => ToCore(rb.position),
                    () => ToCore(rb.rotation));
                if (sendBodyDynamics)
                    Publisher.BindBodyDynamics(
                        () => (_deriver.SpecificForce, _deriver.AngularVelocity));
            }
        }

        void LateUpdate()
        {
            var pub = _publisher;
            if (pub == null) return;
            bool paused = _manualPaused || (autoDetectPause && Time.timeScale == 0f);
            if (paused != pub.Paused) pub.SetPaused(paused);
            // derivation runs every tick with a pose (not just at publish
            // cadence): engine-measured velocity, quaternion-delta rates;
            // Unity's world frame is OGSA-TS native — values pass unmapped.
            // Nothing is measured while paused (C-11); resuming starts the
            // derivation fresh rather than differentiating across the pause
            if (sendBodyDynamics && autoBindRigidbody != null && !paused)
            {
                var rb = autoBindRigidbody;
                var t = Time.timeAsDouble;
                if (_wasPaused)
                    _deriver.Reset(ToCore(rb.position), ToCore(rb.rotation), t);
                else
                    _deriver.Sample(t, ToCore(rb.position), ToCore(rb.rotation),
                        hasVelocity: true, engineVelocity: ToCore(rb.velocity));
            }
            _wasPaused = paused;
            // real time, not game time: a paused game keeps streaming
            pub.Tick(Time.realtimeSinceStartupAsDouble);
        }

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
            => Publisher.DeclareChannel(name, unit, description, min, max);

        public Core.ChannelHandle DeclareBooleanChannel(string name,
                                                        string description = "")
            => Publisher.DeclareBooleanChannel(name, description);

        public void AddWheel(string name, Vector3 offset, float radius,
                             bool driven, bool steered,
                             float stiffness = 65000f, float damping = 5000f)
            => Publisher.AddWheel(name, ToCore(offset), radius, driven, steered,
                                  stiffness, damping);

        /// <summary>Declares or updates the ambient environment carried in
        /// discovery (re-sent on change — e.g. entering a space level:
        /// gravity 0, medium vacuum). Callable before the first tick and
        /// afterwards.</summary>
        public void SetEnvironment(float gravity, float airDensity, float temperature,
                                   float pressure, Ogsa.Telemetry.Schema.ContactMedium medium)
            => Publisher.SetEnvironment(gravity, airDensity, temperature, pressure, medium);

        // ---- bindings (pull model) ----

        /// <summary>Binds pose getters in Unity's world frame (direct map —
        /// Unity is already left-handed, Z-forward, Y-up).</summary>
        public void BindPose(Func<Vector3>? position, Func<Quaternion>? orientation)
            => Publisher.BindPose(
                position is null ? null : () => ToCore(position()),
                orientation is null ? null : () => ToCore(orientation()));

        /// <summary>Binds a numeric channel; the getter returns values in
        /// sourceUnit (what the game natively measures) and the factor to
        /// the channel's target unit is precomputed at bind.</summary>
        public void BindChannel(Core.ChannelHandle channel, Func<double> source,
                                Core.Unit sourceUnit)
            => Publisher.BindChannel(channel, source, sourceUnit);

        public void BindChannelRaw(Core.ChannelHandle channel, Func<double> source)
            => Publisher.BindChannelRaw(channel, source);

        public void BindBooleanChannel(Core.ChannelHandle channel, Func<bool> source)
            => Publisher.BindBooleanChannel(channel, source);

        // ---- push model ----

        public void SetChannel(Core.ChannelHandle channel, double value)
            => Publisher.SetChannel(channel, value);

        public void SetChannel(Core.ChannelHandle channel, bool value)
            => Publisher.SetChannel(channel, value);

        public void FireEvent(string name, float intensity = 1f, float duration = 0f,
                              Vector3 direction = default)
            => Publisher.FireEvent(name, intensity, duration, ToCore(direction));

        // Unity's numeric types map 1:1 onto the core's System.Numerics —
        // same axes, same order; this is a plain component copy.
        static CoreVector3 ToCore(Vector3 v) =>
            new(v.x, v.y, v.z);
        static CoreQuaternion ToCore(Quaternion q) =>
            new(q.x, q.y, q.z, q.w);
    }
}
