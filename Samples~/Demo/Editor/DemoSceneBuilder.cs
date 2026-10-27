#nullable enable
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Ogs.Telemetry.Unity.Demo;

namespace Ogs.Telemetry.Unity.Demo.Editor
{
    /// <summary>Builds a self-contained demo scene: a wired cube "vehicle"
    /// with a TelemetryPublisher, kinematic driver, ground, and camera.
    /// Menu: GameObject > OGS-TS > Create Demo Scene.</summary>
    public static class DemoSceneBuilder
    {
        [MenuItem("GameObject/OGS-TS/Create Demo Scene")]
        public static void Create()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("OGS-TS Demo",
                    "Exit Play mode first — the demo scene is generated in edit mode.", "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);

            // vehicle: cube with rigidbody (kinematic — the driver moves it)
            var car = GameObject.CreatePrimitive(PrimitiveType.Cube);
            car.name = "OGS-TS Demo Car";
            car.transform.position = new Vector3(0f, 0.5f, 10f);
            car.transform.localScale = new Vector3(4f, 1f, 1.8f);
            var rb = car.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var telemetry = car.AddComponent<TelemetryPublisherBehaviour>();
            telemetry.gameName = Application.productName;
            telemetry.objectName = "demo_car";
            telemetry.typeLabel = "OGS-TS Unity demo vehicle";
            telemetry.autoBindRigidbody = rb;

            car.AddComponent<DemoVehicleDriver>();

            // camera framing the circle
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(0f, 18f, -22f);
            camGo.transform.rotation =
                Quaternion.Euler(45f, 0f, 0f);
            camGo.AddComponent<AudioListener>();

            const string path = "Assets/Scenes/OGS-TS-Demo.unity";
            System.IO.Directory.CreateDirectory(
                System.IO.Path.GetDirectoryName(path)!);
            EditorSceneManager.SaveScene(scene, path);
            EditorUtility.DisplayDialog("OGS-TS Demo",
                $"Demo scene saved to {path}.\n\nPress Play, then run the " +
                "reference receiver (ogs-ts-dotnet: dotnet run --project " +
                "src/TelemetryReceive) to see discovery, motion, and channels.",
                "OK");
            Debug.Log($"OGS-TS demo scene created at {path}. " +
                      "Pair it with ogs-ts-dotnet's TelemetryReceive.");
        }
    }
}
