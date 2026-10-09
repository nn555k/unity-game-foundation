#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository_root="$(cd "${script_dir}/.." && pwd)"

python3 "${script_dir}/validate_repository.py"
python3 "${script_dir}/sync_governance_templates.py" --check
python3 "${script_dir}/test_validate_game_foundation_project.py"
python3 "${script_dir}/test_onboarding_defaults.py"
"${repository_root}/.agents/skills/maintain-game-foundation/scripts/validate-foundation-boundaries.sh"

for manifest in "${repository_root}"/Packages/com.nn555k.gamefoundation.*/package.json; do
  python3 -m json.tool "${manifest}" >/dev/null
done

while IFS= read -r -d '' assembly; do
  python3 -m json.tool "${assembly}" >/dev/null
done < <(find "${repository_root}/Packages" -path '*/com.nn555k.gamefoundation.*/*' -name '*.asmdef' -print0)

echo "Game Foundation static validation passed."
