#nullable enable
using UnityEngine;
using NUnit.Framework;
using Ogsa.Telemetry.Unity;

namespace Ogsa.Telemetry.Unity.Tests
{
    /// <summary>EditMode tests for the Unity wrapper — no network sends
    /// (Tick is never called; sockets are created lazily and disposed on
    /// destroy). These run in-editor via TestProject~/ and become the CI
    /// EditMode suite under game-ci (see .github/workflows/ci.yml).</summary>
    public class TelemetryPublisherBehaviourTests
    {
        GameObject MakePublisher(out TelemetryPublisherBehaviour behaviour)
        {
            var go = new GameObject("ogsa_test_publisher");
            behaviour = go.AddComponent<TelemetryPublisherBehaviour>();
            return go;
        }

        [Test]
        public void Publisher_Is_Created_Lazily_From_Inspector_Config()
        {
            var go = MakePublisher(out var behaviour);
            Assert.IsNotNull(behaviour.Publisher);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Channels_Declare_And_Lock_After_First_Tick()
        {
            var go = MakePublisher(out var behaviour);
            var rpm = behaviour.DeclareChannel("engine.rpm", Ogsa.Telemetry.Unit.Rpm,
                                               "Engine speed", 0, 9000);
            Assert.AreEqual(0, rpm.Id);
            behaviour.Publisher.Tick(0.0);
            Assert.Throws<System.InvalidOperationException>(
                () => behaviour.DeclareChannel("late.channel", Ogsa.Telemetry.Unit.Scalar));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Boolean_Channel_Round_Trips_Handle()
        {
            var go = MakePublisher(out var behaviour);
            var lights = behaviour.DeclareBooleanChannel("lights.headlights");
            behaviour.SetChannel(lights, true);
            behaviour.Publisher.Dispose(); // no send happened; just release state
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Wheel_Declaration_Accepts_Unity_Native_Frame()
        {
            // Unity's frame is OGSA-TS native — offsets pass straight through
            var go = MakePublisher(out var behaviour);
            behaviour.AddWheel("wheel_fl", new Vector3(1.2f, 0f, 0.8f),
                               radius: 0.34f, driven: false, steered: true);
            behaviour.AddWheel("wheel_rl", new Vector3(-1.2f, 0f, 0.8f),
                               radius: 0.34f, driven: true, steered: false);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void FireEvent_Queues_Without_Network()
        {
            var go = MakePublisher(out var behaviour);
            behaviour.FireEvent("body.impact", 0.8f, 0.25f, new Vector3(-1f, 0f, 0f));
            Object.DestroyImmediate(go); // no Tick: nothing was sent
        }
    }
}
