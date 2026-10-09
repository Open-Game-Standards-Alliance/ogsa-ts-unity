# ogsa-ts-unity

Unity package for the [OGSA Telemetry Standard (OGSA-TS)](../OGSA-Telemetry-Standard)
— broadcast live motion and event telemetry from a Unity game to motion
platforms, haptic hardware, dashboards, and loggers on the player's local
network, over UDP multicast. Nothing leaves the LAN.

Status: **bootstrap, validated** — end-to-end against the reference receiver in Unity 2022.3 (discovery, motion, channels). `TelemetryPublisher` component, vendored
`Ogsa.Telemetry` core (netstandard2.1 / C# 9, the same source tested in
ogsa-ts-dotnet), and a generated demo scene. Validated against the reference
receiver (`ogsa-ts-dotnet`: `dotnet run --project src/TelemetryReceive`).

## Install

Unity 2022.3+ / Unity 6. Via Package Manager (git URL) once published, or
copy this folder into `Packages/` for local development. Render-pipeline
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

Menu: **GameObject > OGSA-TS > Create Demo Scene** — builds a wired demo
vehicle (kinematic cube on a circle, rpm/speed/headlights channels). Press
Play and run the reference receiver to watch discovery, motion, and
channels on the wire.

## Layout

| Path | Contents |
|---|---|
| `Runtime/TelemetryPublisherBehaviour.cs` | Unity-facing component: Inspector config, Unity-typed bindings, Rigidbody auto-bind |
| `Runtime/Core/` | Vendored `Ogsa.Telemetry` core + generated Cap'n Proto bindings (`sync-core.sh` refreshes from `../ogsa-ts-dotnet`) |
| `Runtime/Core/ThirdParty/` | `Capnp.Net.Runtime` (netstandard2.0, MIT) |
| `Editor/` | Demo scene builder + demo vehicle driver |

## Vendored core discipline

The core source is copied from `ogsa-ts-dotnet` (single source of truth;
its `netstandard2.1` target + test suite are the compatibility gate). After
pulling upstream: `./sync-core.sh` — existing `.meta` files are preserved
so GUIDs stay stable.
