#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../../../.." && pwd)"
package_root="${repo_root}/Packages"
packages=(
  com.nn555k.gamefoundation.core
  com.nn555k.gamefoundation.save
  com.nn555k.gamefoundation.hotupdate
  com.nn555k.gamefoundation.hotupdate.yooasset
  com.nn555k.gamefoundation.sdk
  com.nn555k.gamefoundation.ui
  com.nn555k.gamefoundation.setup
)

failures=0

for package_name in "${packages[@]}"; do
  package_path="${package_root}/${package_name}"
  if [[ ! -f "${package_path}/package.json" ]]; then
    echo "ERROR missing package manifest: ${package_path}/package.json"
    failures=$((failures + 1))
  fi

  if ! find "${package_path}" -maxdepth 2 -name '*.asmdef' -print -quit | grep -q .; then
    echo "ERROR missing package asmdef: ${package_path}"
    failures=$((failures + 1))
  fi
done

if rg -n --glob '*.cs' '\b(namespace|using)\s+Turngrid\b' "${package_root}"/com.nn555k.gamefoundation.*; then
  echo "ERROR Foundation packages reference the Turngrid namespace."
  failures=$((failures + 1))
fi

if rg -n --glob '*.cs' 'UnityEngine\.Purchasing|\bIAP\b|\bIap[A-Z]|InAppPurchase|WeeklyRank|Economy(Model|System)|\bGameplay\b' "${package_root}"/com.nn555k.gamefoundation.*; then
  echo "ERROR Foundation packages contain excluded gameplay, rank, economy, or purchasing dependencies."
  failures=$((failures + 1))
fi

if rg -n 'com\.nn555k\.gamefoundation\.(save|hotupdate|sdk|ui|setup|designimport)' "${package_root}/com.nn555k.gamefoundation.core"; then
  echo "ERROR Core depends on a sibling Foundation capability package."
  failures=$((failures + 1))
fi

if [[ "${failures}" -ne 0 ]]; then
  echo "Foundation boundary validation failed with ${failures} issue(s)."
  exit 1
fi

echo "Foundation boundary validation passed for ${#packages[@]} packages."
