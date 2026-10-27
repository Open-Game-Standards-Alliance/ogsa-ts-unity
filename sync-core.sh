#!/usr/bin/env bash
# sync-core.sh — vendor the engine-agnostic core from ../ogs-ts-dotnet.
# Existing .meta files are preserved (stable GUIDs => stable references);
# new files get generated metas. Run from the repo root after pulling
# upstream changes:  ./sync-core.sh
set -euo pipefail
SRC="../ogs-ts-dotnet/src/Ogs.Telemetry"
DST="Runtime/Core"

# Portable 32-char lowercase-hex GUID (Unity's .meta format), without
# depending on `powershell`, which doesn't exist off Windows.
guid() {
  if command -v uuidgen >/dev/null 2>&1; then
    uuidgen | tr -d '-' | tr '[:upper:]' '[:lower:]'
  elif command -v python3 >/dev/null 2>&1; then
    python3 -c 'import uuid; print(uuid.uuid4().hex)'
  elif [ -r /proc/sys/kernel/random/uuid ]; then
    tr -d '-' < /proc/sys/kernel/random/uuid
  elif command -v pwsh >/dev/null 2>&1; then
    pwsh -NoProfile -Command "[guid]::NewGuid().ToString('N')" | tr -d '\r\n'
  elif command -v powershell >/dev/null 2>&1; then
    powershell -NoProfile -Command "[guid]::NewGuid().ToString('N')" | tr -d '\r\n'
  else
    echo "guid: no GUID generator found (need uuidgen, python3, /proc/sys/kernel/random/uuid, or pwsh/powershell)" >&2
    exit 1
  fi
}
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

# Mirror deletions: a vendored source whose upstream file is gone (moved
# or renamed in ogs-ts-dotnet) is removed together with its .meta.
for f in "$DST"/*.cs "$DST"/Schema/*.cs; do
  [ -f "$f" ] || continue
  if [ ! -f "$SRC/${f#"$DST"/}" ]; then
    rm -f "$f" "$f.meta"
    echo "removed: $f"
    changed=1
  fi
done

if [ "$changed" -eq 0 ]; then echo "core up to date"; fi
