# ogsa-ts-unity working conventions

- Core is vendored under `Runtime/Core/` from `../ogsa-ts-dotnet`
  (single source of truth). Never hand-edit vendored files or the generated
  Cap'n Proto bindings; refresh with `./sync-core.sh`, which preserves
  existing .meta files (stable GUIDs).
- The core must stay C# 9 / netstandard2.1-clean (Unity's compiler
  ceiling); its multi-target build + tests in ogsa-ts-dotnet are the gate.
- Unity wrapper code is the only Unity-facing surface: Inspector config,
  Unity-typed overloads, Rigidbody auto-bind, LateUpdate tick with
  Time.timeAsDouble. Unity's world frame matches OGSA-TS natively —
  no axis remap on this engine (UE/Godot wrappers differ; their frame
  conversions are theirs to own).
- Product docs describe current state only; history lives in the
  Open-Game-Standards-Alliance knowledge base (knowledge/README.md bridge).
- The Capnp.Net.Runtime.dll under Runtime/Core/ThirdParty is refreshed
  manually from the NuGet cache when the dependency version bumps.
- CI (`.github/workflows/ci.yml`), license-free: vendored-core drift vs
  ogsa-ts-dotnet, compile smoke (`CI~/ci-smoke.csproj` compiles Runtime
  against stub UnityEngine — CI~/ is invisible to Unity via the ~ suffix;
  extend stubs only with what Runtime actually references), package.json
  sanity. Tier-2 game-ci EditMode testing is a commented job pending a
  Unity license secret.
