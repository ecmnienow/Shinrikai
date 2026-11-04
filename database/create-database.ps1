<#
.SYNOPSIS
  Cria o banco do Shinrikai no Firebird local (schema.sql + seed.sql) e configura a connection
  string da API via user-secrets. Não altera nenhum código.

.EXAMPLE
  .\database\create-database.ps1
  .\database\create-database.ps1 -Password 'minhaSenha' -Path 'D:\Dados\SHINRIKAI.FDB'
#>
param(
    # Arquivo do banco. O serviço do Firebird precisa ter acesso a esta pasta.
    [string]$Path = (Join-Path $PSScriptRoot 'data\SHINRIKAI.FDB'),
    [string]$User = 'SYSDBA',
    # Senha do usuário no servidor Firebird (a mesma do Web.config do sistema original, por padrão).
    [string]$Password = 'masterkey',
    [string]$Server = 'localhost',
    [int]$Port = 3050,
    # isql da versão mais nova do Firebird instalada (Firebird_5_0, Firebird_4_0...).
    [string]$Isql = (Get-ChildItem 'C:\Program Files\Firebird\Firebird_*\isql.exe' -ErrorAction SilentlyContinue |
        Sort-Object { [int]($_.Directory.Name -replace '\D', '') } | Select-Object -Last 1).FullName,
    # Não grava a connection string em user-secrets.
    [switch]$SkipUserSecrets
)

$ErrorActionPreference = 'Stop'

if (-not $Isql -or -not (Test-Path $Isql)) { throw "isql do Firebird não encontrado ('$Isql'). Informe -Isql." }
Write-Host "Usando $Isql"
if (Test-Path $Path) { throw "O banco '$Path' já existe. Apague-o ou informe outro -Path." }

$dir = Split-Path $Path -Parent
New-Item -ItemType Directory -Force $dir | Out-Null
$Path = (Resolve-Path $dir).Path + '\' + (Split-Path $Path -Leaf)
$dsn = "${Server}/${Port}:$Path"

Write-Host "Criando $dsn ..."
# Via arquivo (-i): o isql ignora o CREATE DATABASE recebido pelo pipe do Windows PowerShell.
$createScript = Join-Path ([IO.Path]::GetTempPath()) "shinrikai-create-$PID.sql"
$create = "CREATE DATABASE '$dsn' USER '$User' PASSWORD '$Password' PAGE_SIZE 16384 DEFAULT CHARACTER SET UTF8;"
[IO.File]::WriteAllText($createScript, $create, (New-Object Text.UTF8Encoding($false)))
try {
    & $Isql -q -b -ch UTF8 -i $createScript
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $Path)) { throw 'Falha ao criar o banco.' }
}
finally {
    Remove-Item $createScript -ErrorAction SilentlyContinue
}

foreach ($script in 'schema.sql', 'seed.sql') {
    Write-Host "Executando $script ..."
    & $Isql -q -b -ch UTF8 -user $User -password $Password -i (Join-Path $PSScriptRoot $script) $dsn
    if ($LASTEXITCODE -ne 0) { throw "Falha ao executar $script." }
}

if (-not $SkipUserSecrets) {
    & (Join-Path $PSScriptRoot 'configure-connection.ps1') -Path $Path -User $User -Password $Password -Server $Server -Port $Port
}

Write-Host "Banco criado: $Path"
Write-Host 'Reinicie a API para usar o banco. Para tornar um usuário administrador (menu Professor):'
Write-Host "  .\database\promote-user.ps1 -Login <login>"
