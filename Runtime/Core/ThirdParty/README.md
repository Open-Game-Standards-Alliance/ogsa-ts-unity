# ThirdParty DLLs (netstandard2.0)

Vendored binaries for Unity (which cannot restore NuGet packages inside a
UPM package). All MIT-licensed.

| DLL | Version | Why |
|---|---|---|
| Capnp.Net.Runtime | 1.3.118 | Cap'n Proto runtime (must match the package ref in ogsa-ts-dotnet) |
| Microsoft.Extensions.Logging (+ .Abstractions) | 2.2.0 | direct dependency of Capnp.Net.Runtime |
| Microsoft.Extensions.DependencyInjection.Abstractions | 2.2.0 | assembly ref of Logging/Options |
| Microsoft.Extensions.Options | 2.2.0 | assembly ref of Logging |
| Microsoft.Extensions.Primitives | 2.2.0 | assembly ref of Options |
| System.Runtime.CompilerServices.Unsafe | 4.x | assembly ref of Primitives (Unity ships System.Memory but not this) |

Refresh from the NuGet cache when bumping Capnp.Net.Runtime in
ogsa-ts-dotnet — see sync-core.sh notes. Closure verified by assembly
references (nuspec deps like Configuration.Binder are not assembly refs
and are not needed).
