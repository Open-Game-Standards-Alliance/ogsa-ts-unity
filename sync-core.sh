#!/usr/bin/env bash
# sync-core.sh — vendor the engine-agnostic core from ../ogsa-ts-dotnet.
# Existing .meta files are preserved (stable GUIDs => stable references);
# new files get generated metas. Run from the repo root after pulling
# upstream changes:  ./sync-core.sh
set -euo pipefail
SRC="../ogsa-ts-dotnet/src/Ogsa.Telemetry"
DST="Runtime/Core"

guid() { powershell -NoProfile -Command "[guid]::NewGuid().ToString('N')" | tr -d '\r\n'; }
cs_meta() { # $1 = cs path
  local m="$1.meta"
  [ -f "$m" ] && return
  printf 'fileFormatVersion: 2\nguid: %s\nMonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' "$(guid)" > "$m"
  echo "meta: $m"
}

changed=0
for f in "$SRC"/*.cs; do
  if ! cmp -s "$f" "$DST/$(basename "$f")"; then
    cp "$f" "$DST/"
    echo "updated: $DST/$(basename "$f")"
    changed=1
  fi
  cs_meta "$DST/$(basename "$f")"
done
for f in "$SRC"/Schema/*.cs; do
  if ! cmp -s "$f" "$DST/Schema/$(basename "$f")"; then
    cp "$f" "$DST/Schema/"
    echo "updated: $DST/Schema/$(basename "$f")"
    changed=1
  fi
  cs_meta "$DST/Schema/$(basename "$f")"
done

# Capnp.Net.Runtime (netstandard2.0 build — Unity-compatible)
if ! cmp -s "$SRC/../../capnp/Capnp.Net.Runtime.dll" "Runtime/Core/ThirdParty/Capnp.Net.Runtime.dll" 2>/dev/null; then
  echo "note: refresh Runtime/Core/ThirdParty/Capnp.Net.Runtime.dll from the"
  echo "      NuGet cache (capnp.net.runtime/<ver>/lib/netstandard2.0/) after"
  echo "      bumping the dependency in ogsa-ts-dotnet."
fi

[ "$changed" -eq 0 ] && echo "core up to date"
