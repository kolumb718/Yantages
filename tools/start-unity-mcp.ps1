# Run from any working directory. Server is local-only; no Windows autostart.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$env:DISABLE_TELEMETRY = '1'
$localServer = Join-Path $projectRoot '.local/mcp-env/Scripts/mcp-for-unity.exe'
if (Test-Path -LiteralPath $localServer) {
    & $localServer --transport http --http-host 127.0.0.1 --http-port 8080
} else {
    $uvx = Get-Command uvx -ErrorAction SilentlyContinue
    if (-not $uvx) { throw 'Install uv from https://docs.astral.sh/uv/getting-started/installation/ then run this script again.' }
    & $uvx.Source --from 'mcpforunityserver==10.3.0' mcp-for-unity --transport http --http-host 127.0.0.1 --http-port 8080
}
if ($LASTEXITCODE -ne 0) { throw "MCP server exited with code $LASTEXITCODE" }
