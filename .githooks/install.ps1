param(
    [string]$root
)

$src  = Join-Path $root ".githooks\pre-commit"
$dest = Join-Path $root ".git\hooks\pre-commit"

if (-not (Test-Path $dest)) {
    if (Test-Path $src) {
        Copy-Item -Path $src -Destination $dest -Force
    }
}
