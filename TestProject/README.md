# TestProject — host project for the package

Opens the package (`file:../..`) in a real editor with no setup:

1. Open this folder in Unity 2022.3 LTS (Hub → Add → this folder).
2. First open resolves the local package and compiles everything.

What it gives you:

- **Demo scene**: menu `GameObject > OGSA-TS > Create Demo Scene`, then
  Play, then pair with ogsa-ts-dotnet's TelemetryReceive
  (`dotnet run --project src/TelemetryReceive`).
- **EditMode tests**: Window > General > Test Runner (the package's
  `Tests/Editor/` suite is registered via `testables` in the manifest).

CI: this is the project the commented `editmode` job in
`.github/workflows/ci.yml` consumes once a `UNITY_LICENSE` secret is
configured (game-ci/unity-test-runner, projectPath: TestProject).
