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
    [string]$Isql = 'C:\Program Files\Firebird\Firebird_4_0\isql.exe'
)

$ErrorActionPreference = 'Stop'
$type = @{ Administrator = "'A'"; Teacher = "'T'"; Student = 'NULL' }[$Role]
$safeLogin = $Login.Replace("'", "''")
$Path = (Resolve-Path $Path).Path

$sql = @"
SET HEADING OFF;
UPDATE USERS SET USR_TYPE = $type WHERE USR_LOGIN = '$safeLogin';
COMMIT;
SELECT COUNT(*) FROM USERS WHERE USR_LOGIN = '$safeLogin';
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
