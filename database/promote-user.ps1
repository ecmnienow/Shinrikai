<#
.SYNOPSIS
  Define o papel de um usuário (USERS.USR_TYPE). O cadastro pela tela cria sempre alunos; o
  sistema original também não tinha tela para isso (o menu "Controle de usuários" estava desativado).

.EXAMPLE
  .\database\promote-user.ps1 -Login everton            # administrador
  .\database\promote-user.ps1 -Login maria -Role Teacher
#>
param(
    [Parameter(Mandatory)] [string]$Login,
    [ValidateSet('Administrator', 'Teacher', 'Student')] [string]$Role = 'Administrator',
    [string]$Path = (Join-Path $PSScriptRoot 'data\SHINRIKAI.FDB'),
    [string]$User = 'SYSDBA',
    [string]$Password = 'masterkey',
    [string]$Server = 'localhost',
    [int]$Port = 3050,
    # isql da versão mais nova do Firebird instalada (Firebird_5_0, Firebird_4_0...).
    [string]$Isql = (Get-ChildItem 'C:\Program Files\Firebird\Firebird_*\isql.exe' -ErrorAction SilentlyContinue |
        Sort-Object { [int]($_.Directory.Name -replace '\D', '') } | Select-Object -Last 1).FullName
)

$ErrorActionPreference = 'Stop'
if (-not $Isql -or -not (Test-Path $Isql)) { throw "isql do Firebird não encontrado ('$Isql'). Informe -Isql." }
$type = @{ Administrator = "'A'"; Teacher = "'T'"; Student = 'NULL' }[$Role]
$safeLogin = $Login.Replace("'", "''")
$Path = (Resolve-Path $Path).Path

$sql = @"
SET HEADING OFF;
UPDATE USERS SET USR_TYPE = $type WHERE UPPER(USR_LOGIN) = UPPER('$safeLogin');
COMMIT;
SELECT COUNT(*) FROM USERS WHERE UPPER(USR_LOGIN) = UPPER('$safeLogin');
"@
$script = Join-Path ([IO.Path]::GetTempPath()) "shinrikai-promote-$PID.sql"
[IO.File]::WriteAllText($script, $sql, (New-Object Text.UTF8Encoding($false)))
try {
    $count = (& $Isql -q -b -ch UTF8 -user $User -password $Password -i $script "${Server}/${Port}:$Path" | Where-Object { $_.Trim() }) -join ''
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao atualizar o usuário.' }
}
finally {
    Remove-Item $script -ErrorAction SilentlyContinue
}
if ($count.Trim() -ne '1') { throw "Usuário '$Login' não encontrado." }
Write-Host "'$Login' agora é $Role. Saia e entre de novo no sistema para o menu atualizar."
