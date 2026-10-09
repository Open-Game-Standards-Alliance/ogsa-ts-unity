// UnityEngineStubs.cs — compile-only stubs of the Unity API surface the
// package's Runtime code touches. This exists ONLY for CI (CI~/ is
// invisible to Unity — the ~ suffix hides the folder, like Samples~)
// so `dotnet build ci-smoke.csproj` can type-check the wrapper without
// a Unity install. NOT for editor use; extend only with what the
// Runtime sources actually reference.
//
// Not stubbed: the Editor/ scripts (DemoSceneBuilder) — they pull in a
// much larger UnityEditor surface for demo tooling; the smoke compiles
// the shipped Runtime logic.
#nullable enable
namespace UnityEngine
{
    public class Component
    {
        public bool enabled { get; set; }
        public T GetComponent<T>() => default!;
    }

    public class MonoBehaviour : Component { }

    public class Object
    {
        public static void DestroyImmediate(Object obj) { }
    }

    public sealed class GameObject : Object
    {
        public GameObject(string name) { }
        public T AddComponent<T>() where T : Component, new() => new T();
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z)
        { this.x = x; this.y = y; this.z = z; }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion Euler(float x, float y, float z) => new();
    }

    public class Rigidbody
    {
        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }
        public void MovePosition(Vector3 position) { }
        public void MoveRotation(Quaternion rot) { }
    }

    public static class Mathf
    {
        public const float Rad2Deg = 57.29578f;
        public static float Sin(float f) => 0f;
        public static float Cos(float f) => 0f;
    }

    public static class Time
    {
        public static double timeAsDouble => 0.0;
        public static float fixedDeltaTime => 0f;
    }

    public static class Application
    {
        public static string productName => "";
    }

    public static class Debug
    {
        public static void LogError(object message) { }
    }

    // ---- attributes used by the wrapper's inspector fields ----
    public class HeaderAttribute : System.Attribute
    { public HeaderAttribute(string header) { } }
    public class TooltipAttribute : System.Attribute
    { public TooltipAttribute(string tooltip) { } }
    public class RangeAttribute : System.Attribute
    { public RangeAttribute(float min, float max) { } }
    public class MinAttribute : System.Attribute
    { public MinAttribute(float min) { } }
    public class AddComponentMenuAttribute : System.Attribute
    { public AddComponentMenuAttribute(string menu) { } }
    public class DisallowMultipleComponentAttribute : System.Attribute { }
}
