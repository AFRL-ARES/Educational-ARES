#!/usr/bin/env zsh

# macOS-friendly port of AddMigration.ps1. Adds EF Core migrations for each provider/context pair.

set -u

usage() {
  cat <<'EOF'
Usage: AddMigration.zsh <MigrationName> [options]

Required:
  MigrationName            Name of the migration. The script will append the DbContext name.

Options:
  -c, --configuration VAL  Build configuration to use (default: Debug).
  -p, --providers LIST     Space-separated list of providers to target (default: Sqlite Postgres SqlServer).
  -h, --help               Show this help.

Example:
  ./AddMigration.zsh InitialCreate -c Release -p Sqlite Postgres
EOF
}

if [[ $# -eq 0 ]]; then
  usage
  exit 1
fi

migration_name=""
configuration="Debug"
typeset -a providers_default providers contexts
providers_default=("Sqlite" "Postgres" "SqlServer")
providers=("${providers_default[@]}")

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    -c|--configuration)
      if [[ $# -lt 2 ]]; then
        echo "Missing value for $1." >&2
        exit 1
      fi
      configuration="$2"
      shift 2
      ;;
    -p|--providers)
      providers=()
      shift
      if [[ $# -eq 0 ]]; then
        echo "No providers supplied after --providers." >&2
        exit 1
      fi
      while [[ $# -gt 0 ]]; do
        case "$1" in
          -*) break ;;
          *)
            providers+=("$1")
            shift
            ;;
        esac
      done
      if [[ ${#providers[@]} -eq 0 ]]; then
        echo "No providers supplied after --providers." >&2
        exit 1
      fi
      ;;
    -*)
      echo "Unknown option: $1" >&2
      usage
      exit 1
      ;;
    *)
      if [[ -z "$migration_name" ]]; then
        migration_name="$1"
        shift
      else
        echo "Unexpected argument: $1" >&2
        usage
        exit 1
      fi
      ;;
  esac
done

if [[ -z "$migration_name" ]]; then
  echo "Migration name is required." >&2
  usage
  exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "No dotnet installed." >&2
  exit 1
fi

if ! dotnet ef --help >/dev/null 2>&1; then
  echo "Dotnet tools may not have been installed. They are needed for this script." >&2
  exit 1
fi

script_dir="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$script_dir/../../" && pwd)"
migration_project_base="AresService.Migrations."
startup_project="$script_dir/../AresService.csproj"
solution="$root/AresOS.sln"
contexts=("AresDbContext" "AresIdentityContext")

echo "Building project in $configuration mode..."
if ! dotnet build "$startup_project" --configuration "$configuration"; then
  echo "Build failed — aborting migrations." >&2
  exit 1
fi

for provider in "${providers[@]}"; do
  migration_dir="${migration_project_base}${provider}"
  output_dir="$root/$migration_dir"

  if [[ ! -d "$output_dir" ]]; then
    echo "Migration destination not found: $output_dir" >&2
    exit 1
  fi

  output_proj="${output_dir}/${migration_dir}.csproj"

  for context in "${contexts[@]}"; do
    context_specific_migration="${migration_name}_${context}"
    echo "=== Adding migration '$migration_name' for $context ($provider) ==="

    if ! dotnet ef migrations add "$context_specific_migration" \
      --no-build \
      --project "$output_proj" \
      --startup-project "$startup_project" \
      --context "$context" \
      -- \
      --provider "$provider"; then
        echo "Migration failed for $context ($provider)." >&2
        exit 1
    fi
  done
done

dotnet build "$solution"
