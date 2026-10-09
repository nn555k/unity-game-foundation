#!/usr/bin/env bash
set -euo pipefail

if [[ "$#" -ne 1 ]]; then
  echo "Usage: $0 Android|iOS|StandaloneOSX|StandaloneLinux64"
  exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "${script_dir}/.." && pwd)"
default_editor="/Applications/Unity/Hub/Editor/2022.3.62f2c1/Unity.app/Contents/MacOS/Unity"
unity_editor="${GAME_FOUNDATION_UNITY_EDITOR:-${default_editor}}"
target="$1"

if [[ ! -x "${unity_editor}" ]]; then
  echo "Unity editor not found: ${unity_editor}"
  exit 1
fi

export GAME_FOUNDATION_BUILD_TARGET="${target}"
export GAME_FOUNDATION_BUILD_OUTPUT="${repository_root}/Builds/Validation"
mkdir -p "${GAME_FOUNDATION_BUILD_OUTPUT}"

"${unity_editor}" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "${repository_root}" \
  -buildTarget "${target}" \
  -executeMethod GameFoundation.Validation.Editor.FoundationValidationProjectTools.BuildFromEnvironment \
  -logFile "${repository_root}/Builds/Validation/${target}.log"
