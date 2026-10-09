#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "${script_dir}/.." && pwd)"
default_editor="/Applications/Unity/Hub/Editor/2022.3.62f2c1/Unity.app/Contents/MacOS/Unity"
unity_editor="${GAME_FOUNDATION_UNITY_EDITOR:-${default_editor}}"
result_root="${GAME_FOUNDATION_TEST_OUTPUT:-${repository_root}/TestResults}"

if [[ ! -x "${unity_editor}" ]]; then
  echo "Unity editor not found: ${unity_editor}"
  echo "Set GAME_FOUNDATION_UNITY_EDITOR to the Unity executable path."
  exit 1
fi

mkdir -p "${result_root}"

python3 "${script_dir}/run_unity_test_mode.py" \
  --editor "${unity_editor}" \
  --project "${repository_root}" \
  --mode EditMode \
  --result "${result_root}/editmode.xml" \
  --log "${result_root}/editmode.log"

python3 "${script_dir}/run_unity_test_mode.py" \
  --editor "${unity_editor}" \
  --project "${repository_root}" \
  --mode PlayMode \
  --result "${result_root}/playmode.xml" \
  --log "${result_root}/playmode.log"

echo "Unity EditMode and PlayMode tests passed: ${result_root}"
