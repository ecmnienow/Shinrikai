<#
.SYNOPSIS
  Aponta a API para um banco Firebird, sem alterar código. Grava a connection string:
  - no perfil "Shinrikai" do Properties\launchSettings.json (variável ConnectionStrings__ShinrikaiDB),
    que o Visual Studio e o "dotnet run" aplicam ao processo; e
  - em user-secrets (ConnectionStrings:ShinrikaiDB), para quem roda sem perfil de execução.

.EXAMPLE
  .\database\configure-connection.ps1
  .\database\configure-connection.ps1 -Path 'D:\Dados\SHINRIKAI.FDB' -Password 'outraSenha'
#>
param(
    [string]$Path = (Join-Path $PSScriptRoot 'data\SHINRIKAI.FDB'),
    [string]$User = 'SYSDBA',
    [string]$Password = 'masterkey',
    [string]$Server = 'localhost',
    [int]$Port = 3050
)

$ErrorActionPreference = 'Stop'
if (-not (Test-Path $Path)) { throw "Banco '$Path' não encontrado. Crie com .\database\create-database.ps1" }
$Path = (Resolve-Path $Path).Path
$connectionString = "character set=UTF8;initial catalog=$Path;data source=$Server;port=$Port;dialect=3;user id=$User;password=$Password"

$project = Join-Path (Split-Path $PSScriptRoot -Parent) 'Nienow.Shinrikai'
$launchSettings = Join-Path $project 'Properties\launchSettings.json'

# launchSettings.json (perfil Shinrikai)
$raw = Get-Content $launchSettings -Raw -Encoding UTF8
$escaped = $connectionString.Replace('\', '\\').Replace('"', '\"')
$pattern = '("ConnectionStrings__ShinrikaiDB"\s*:\s*)"(?:[^"\\]|\\.)*"'
if ($raw -match $pattern) {
    # Troca só o valor, preservando a formatação do arquivo.
    $text = [regex]::Replace($raw, $pattern, { param($m) $m.Groups[1].Value + '"' + $escaped + '"' }).TrimEnd()
}
else {
    $json = $raw | ConvertFrom-Json
    $profile = $json.profiles.Shinrikai
    if (-not $profile) { throw "Perfil 'Shinrikai' não encontrado em $launchSettings" }
    if (-not $profile.environmentVariables) {
        $profile | Add-Member -NotePropertyName environmentVariables -NotePropertyValue ([pscustomobject]@{})
    }
    $profile.environmentVariables | Add-Member -NotePropertyName 'ConnectionStrings__ShinrikaiDB' -NotePropertyValue $connectionString -Force
    $text = $json | ConvertTo-Json -Depth 10
    # ConvertTo-Json do Windows PowerShell escapa caracteres e indenta com 4+ espaços; normaliza o básico.
    $text = [regex]::Replace($text, '\\u(?<h>[0-9a-fA-F]{4})', { param($m) [char][Convert]::ToInt32($m.Groups['h'].Value, 16) })
}
[IO.File]::WriteAllText($launchSettings, $text + [Environment]::NewLine, (New-Object Text.UTF8Encoding($false)))
Write-Host "Connection string gravada no perfil Shinrikai de $launchSettings"

# user-secrets
& dotnet user-secrets --project $project set 'ConnectionStrings:ShinrikaiDB' $connectionString | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Falha ao gravar a connection string em user-secrets.' }
Write-Host 'Connection string gravada em user-secrets (ConnectionStrings:ShinrikaiDB).'
Write-Host 'Pare e inicie a API de novo (no Visual Studio: Shift+F5 e F5).'
