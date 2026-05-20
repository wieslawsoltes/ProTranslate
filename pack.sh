#!/usr/bin/env bash
set -euo pipefail

VERSION="${1:-0.1.0-preview.local}"
OUTPUT="${2:-artifacts/packages}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "${ROOT}"

rm -rf "${OUTPUT}"
mkdir -p "${OUTPUT}"

package_projects=()
while IFS= read -r project; do
  if grep -q "<IsPackable>false</IsPackable>" "${project}"; then
    continue
  fi

  package_projects+=("${project}")
done < <(find src -mindepth 2 -maxdepth 2 -name '*.csproj' | sort)

if [ "${#package_projects[@]}" -eq 0 ]; then
  echo "No packable projects were discovered under src/."
  exit 1
fi

get_xml_value() {
  local file="$1"
  local element="$2"
  sed -n "s:.*<${element}>\\(.*\\)</${element}>.*:\\1:p" "${file}" | head -n 1
}

get_package_id() {
  local project="$1"
  local package_id
  package_id="$(get_xml_value "${project}" "PackageId")"

  if [ -z "${package_id}" ]; then
    package_id="$(basename "${project}" .csproj)"
  fi

  printf '%s\n' "${package_id}"
}

get_assembly_name() {
  local project="$1"
  local assembly_name
  assembly_name="$(get_xml_value "${project}" "AssemblyName")"

  if [ -z "${assembly_name}" ]; then
    assembly_name="$(basename "${project}" .csproj)"
  fi

  printf '%s\n' "${assembly_name}"
}

is_analyzer_package() {
  local project="$1"
  local package_id="$2"

  grep -q "<IsRoslynComponent>true</IsRoslynComponent>" "${project}" \
    || [[ "${package_id}" == *Analyzer* ]] \
    || [[ "${package_id}" == *Analyzers* ]] \
    || [[ "${package_id}" == *SourceGenerator* ]]
}

require_file() {
  local file="$1"
  if [ ! -f "${file}" ]; then
    echo "Expected package artifact was not produced: ${file}"
    exit 1
  fi
}

require_package_entry() {
  local package="$1"
  local entry="$2"
  local entries
  entries="$(unzip -Z1 "${package}")"

  if ! grep -Fxq "${entry}" <<< "${entries}"; then
    echo "Package ${package} is missing required entry: ${entry}"
    printf '%s\n' "${entries}"
    exit 1
  fi
}

reject_package_entry_pattern() {
  local package="$1"
  local pattern="$2"
  local entries
  entries="$(unzip -Z1 "${package}")"

  if grep -Eq "${pattern}" <<< "${entries}"; then
    echo "Package ${package} contains unexpected entries matching: ${pattern}"
    grep -E "${pattern}" <<< "${entries}"
    exit 1
  fi
}

validate_analyzer_package() {
  local project="$1"
  local package_id="$2"
  local nupkg="$3"
  local assembly_name
  assembly_name="$(get_assembly_name "${project}")"

  require_package_entry "${nupkg}" "analyzers/dotnet/cs/${assembly_name}.dll"
  reject_package_entry_pattern "${nupkg}" "^lib/.*/${assembly_name}\\.dll$"

  local nuspec
  nuspec="$(unzip -p "${nupkg}" "${package_id}.nuspec")"

  if grep -q "<dependency " <<< "${nuspec}"; then
    echo "Analyzer package ${nupkg} must not declare package dependencies."
    printf '%s\n' "${nuspec}"
    exit 1
  fi
}

echo "Packing version ${VERSION}"
echo "Discovered package projects:"
for project in "${package_projects[@]}"; do
  echo "  - ${project} ($(get_package_id "${project}"))"
done

for project in "${package_projects[@]}"; do
  package_id="$(get_package_id "${project}")"
  dotnet restore "${project}"

  if is_analyzer_package "${project}" "${package_id}"; then
    dotnet pack "${project}" -c Release --no-restore -o "${OUTPUT}" -p:Version="${VERSION}" -p:IncludeSymbols=false
  else
    dotnet pack "${project}" -c Release --no-restore -o "${OUTPUT}" -p:Version="${VERSION}"
  fi
done

echo "Validating package artifacts..."
for project in "${package_projects[@]}"; do
  package_id="$(get_package_id "${project}")"
  nupkg="${OUTPUT}/${package_id}.${VERSION}.nupkg"
  snupkg="${OUTPUT}/${package_id}.${VERSION}.snupkg"

  require_file "${nupkg}"
  require_package_entry "${nupkg}" "README.md"

  if is_analyzer_package "${project}" "${package_id}"; then
    validate_analyzer_package "${project}" "${package_id}" "${nupkg}"
  else
    require_file "${snupkg}"
  fi
done

echo "Produced package artifacts:"
find "${OUTPUT}" -maxdepth 1 \( -name '*.nupkg' -o -name '*.snupkg' \) -print | sort
