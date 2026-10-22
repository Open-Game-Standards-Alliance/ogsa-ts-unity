# ogsa-ts-unity

Unity package for the [OGSA Telemetry Standard (OGSA-TS)](../OGSA-Telemetry-Standard)
— broadcast live motion and event telemetry from a Unity game to motion
platforms, haptic hardware, dashboards, and loggers on the player's local
network, over UDP multicast. Nothing leaves the LAN.

Community & support: [Discord](https://discord.gg/tseF4Q6U99)

Status: **bootstrap, validated** — end-to-end against the reference receiver in Unity 2022.3 (discovery, motion, channels). `TelemetryPublisher` component, vendored
`Ogsa.Telemetry` core (netstandard2.1 / C# 9, the same source tested in
ogsa-ts-dotnet; plain C# source, no DLLs — it writes Cap'n Proto frames
itself, with no reflection, so it runs under IL2CPP on every platform),
and a generated demo scene. Validated against the reference
receiver (`ogsa-ts-dotnet`: `dotnet run --project src/TelemetryReceive`).

**Built with AI assistance.** Significant portions of this package
(including the vendored core and the Unity-facing wrapper) were written
with AI coding assistance under human direction and
review. Review and test before relying on it, especially around
networking and the unit-conversion math.

## Install

Unity 2022.3+ / Unity 6. Add it to `Packages/manifest.json` as a git
dependency pinned to a release tag (or a full commit hash):

```json
"com.ogsa.telemetry.unity": "https://github.com/Open-Game-Standards-Alliance/ogsa-ts-unity.git#v0.2.0"
```

or copy this folder into `Packages/` for local development. The package
is plain C# source — nothing precompiled. Render-pipeline
agnostic — nothing here touches rendering, so Built-in, URP, or any future
pipeline is fine (note: Built-in is deprecated for new projects in
Unity 6.5+; URP is the safe choice there).

## Quick start

Add **OGSA-TS/Telemetry Publisher** to your vehicle, then wire your game's
values in code (`Awake`/`Start`; declarations lock at the first tick):

```csharp
var telemetry = GetComponent<TelemetryPublisherBehaviour>();

// channels carry REQUIRED units — declare in target units
var rpm   = telemetry.DeclareChannel("engine.rpm", Unit.Rpm, min: 0, max: 9000);
var speed = telemetry.DeclareChannel("vehicle.speed", Unit.MetrePerSecond);

// bind what the game natively measures — conversion is precomputed at bind
telemetry.BindChannel(speed, () => hud.SpeedKmh, Unit.KilometrePerHour);
telemetry.BindChannelRaw(rpm, () => engine.Rpm);
telemetry.BindBooleanChannel(
    telemetry.DeclareBooleanChannel("engine.running"),
    () => engine.IsRunning);

// discrete stimuli for haptics (replayed ~100 ms, id-deduped)
telemetry.FireEvent("body.impact", intensity: 0.8f, duration: 0.25f);
```

Pose: assign a `Rigidbody` to **Auto-bind** (zero code), or call
`BindPose(() => transform.position, () => transform.rotation)`. Unity's
world frame (left-handed, Z-forward, Y-up) matches OGSA-TS natively — no
axis remap on this engine.

Push model for exact timing: `telemetry.SetChannel(rpm, 3200)` from your
own tick. Cadence is publisher-owned: motion goes out at the configured
rate regardless of your frame rate.

## Demo scene

Import the **Demo** sample (Package Manager > OGSA Telemetry Standard >
Samples), then use the menu **GameObject > OGSA-TS > Create Demo Scene** —
it builds a wired demo
vehicle (kinematic cube on a circle, rpm/speed/headlights channels). Press
Play and run the reference receiver to watch discovery, motion, and
channels on the wire.

## Layout

| Path | Contents |
|---|---|
| `Runtime/TelemetryPublisherBehaviour.cs` | Unity-facing component: Inspector config, Unity-typed bindings, Rigidbody auto-bind |
| `Runtime/Core/` | Vendored `Ogsa.Telemetry` core + generated schema writers (`sync-core.sh` refreshes from `../ogsa-ts-dotnet`) |
| `Samples~/Demo/` | Demo vehicle driver + demo scene builder (an importable sample, so it never compiles into games that use the package) |

## Vendored core discipline

The core source is copied from `ogsa-ts-dotnet` (single source of truth;
its `netstandard2.1` target + test suite are the compatibility gate). After
pulling upstream: `./sync-core.sh` — existing `.meta` files are preserved
so GUIDs stay stable.

## Wiring your game's values

Declare channels once in setup (they lock at the first tick), bind getters to
whatever your sim already writes, and fire events from gameplay code. Pose
needs nothing — assign the `Rigidbody` to **Auto-bind**.

```csharp
// your existing vehicle controller; sim writes these every physics tick
public float SpeedKmh, EngineRpm; public int Gear; public bool HeadlightsOn;

TelemetryPublisherBehaviour telemetry;
ChannelHandle rpm, speed, gear, headlights;

void Awake()
{
    telemetry = GetComponent<TelemetryPublisherBehaviour>();
    rpm        = telemetry.DeclareChannel("engine.rpm", Unit.Rpm, min: 0, max: 9000);
    speed      = telemetry.DeclareChannel("vehicle.speed", Unit.MetrePerSecond);
    gear       = telemetry.DeclareChannel("transmission.gear", Unit.Scalar);
    headlights = telemetry.DeclareBooleanChannel("lights.headlights");

    telemetry.BindChannelRaw(rpm,   () => EngineRpm);
    telemetry.BindChannel(speed,    () => SpeedKmh, Unit.KilometrePerHour); // factor at bind
    telemetry.BindChannelRaw(gear,  () => Gear);
    telemetry.BindBooleanChannel(headlights, () => HeadlightsOn);
}

void OnCollisionEnter(Collision c) // events come from gameplay, not polling
{
    telemetry.FireEvent("body.impact",
        intensity: Mathf.Clamp01(c.relativeVelocity.magnitude / 20f),
        duration: 0.25f,
        direction: transform.InverseTransformDirection(-c.relativeVelocity.normalized));
}
```

Pull bindings are sampled at the publisher's own rate — you never call
anything per-frame for them. For exact timing (e.g. a value computed inside
your physics step), use `SetChannel` from there instead.
