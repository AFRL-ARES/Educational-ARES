param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationName,
    [string]$Configuration = "Debug",
    [string[]]$Providers = @("Sqlite", "Postgres", "SqlServer")
)

try { dotnet | Out-Null }
catch {
    Write-Error "No dotnet installed."
    exit
}

dotnet ef *> $null

if (!$?)
{
    Write-Host "Dotnet tools may not have been installed. They are needed for this script."
    Write-Error $Error[0]
    pause
    exit 1
}

$root = "../../"
$migrationProjectBase = "AresService.Migrations."
$startupProject = "../AresService.csproj"
$solution = Join-Path $root "AresOS.sln"
$contexts = @("AresDbContext", "AresIdentityContext")
$migrationsRoot = "Migrations"

# -----------------------------
# Build once before migrations
# -----------------------------
Write-Host "Building project in $Configuration mode..."
dotnet build $startupProject --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed — aborting migrations."
    exit 1
}

# -----------------------------
# Loop through providers + contexts
# -----------------------------
foreach ($provider in $Providers) {
    foreach ($context in $contexts) {

        $migrationDir = $migrationProjectBase + $provider

        # Construct migration output path
        $outputDir = Join-Path $root $migrationDir
        $outputDir = Resolve-Path $outputDir
        $outputProjFileName = $migrationDir + ".csproj"
        $outputProj = Join-Path $outputDir $outputProjFileName

        # Ensure directory exists
        if (-not (Test-Path $outputDir)) {
            $fullDestination = Resolve-Path $outputDir
            Write-Error "Migration destination not found $fullDestination"
            exit 1
        }

        $contextSpecificMigrationName = $MigrationName + "_$context"

        Write-Host "=== Adding migration '$MigrationName' for $context ($provider) ==="
        #--output-dir $outputDir `        
        dotnet ef migrations add $contextSpecificMigrationName `
            --no-build `
            --project $outputProj `
            --startup-project $startupProject `
            --context $context `
            -- --provider $provider

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Migration failed for $context ($provider)"
            exit 1
        }
    }
}

dotnet build $solution