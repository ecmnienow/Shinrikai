<#
.SYNOPSIS
  Implanta (ou atualiza) o Shinrikai no Azure: infra/main.bicep + imagens no Azure Container Registry.

.DESCRIPTION
  1. Gera, na primeira vez, os segredos em infra/.azure.env (ignorado pelo git) e a chave SSH da VM
     em ~/.ssh/shinrikai-azure.
  2. Implanta a infraestrutura mantendo as imagens que os Container Apps já usam.
  3. Monta as imagens com o Docker local, envia ao registry e implanta de novo com elas
     (pule com -SkipImages; no dia a dia quem faz isso é o workflow .github/workflows/deploy.yml).
  4. Grava no GitHub as variáveis AZURE_* do login OIDC do workflow.

  Requer: az login, gh auth login, Docker em execução e o repositório como pasta atual ou pai.

.EXAMPLE
  .\infra\deploy.ps1 -Preview        # só mostra o que seria criado ou alterado (what-if)
  .\infra\deploy.ps1
  .\infra\deploy.ps1 -SkipImages
  .\infra\deploy.ps1 -Location chilecentral
#>
param(
    [string]$Location = 'northcentralus',
    [string]$ResourceGroup = 'rg-shinrikai',
    [string]$Repository = 'ecmnienow/Shinrikai',
    [switch]$SkipImages,
    [switch]$Preview
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

function Invoke-Az {
    $output = & az @args
    if ($LASTEXITCODE -ne 0) { throw "Falhou: az $($args -join ' ')" }
    return $output
}

function New-RandomBytes([int]$Count) {
    $bytes = New-Object byte[] $Count
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return $bytes
}

# ------------------------------------------------------------ Segredos e chave SSH

$secretsFile = Join-Path $PSScriptRoot '.azure.env'
if (-not (Test-Path $secretsFile)) {
    # Hexadecimal: seguro dentro do YAML do cloud-init, do .env do container e da connection string.
    $hex = { param($n) -join ((New-RandomBytes $n) | ForEach-Object { $_.ToString('x2') }) }
    $content = "# Segredos do Shinrikai no Azure (gerados pelo deploy.ps1). Não versionar.`n" +
        "FIREBIRD_ROOT_PASSWORD=$(& $hex 16)`nFIREBIRD_PASSWORD=$(& $hex 16)`n" +
        "JWT_KEY=$([Convert]::ToBase64String((New-RandomBytes 48)))`n"
    [IO.File]::WriteAllText($secretsFile, $content, (New-Object Text.UTF8Encoding($false)))
    Write-Host "Segredos gerados em $secretsFile"
}
$secrets = @{}
foreach ($line in Get-Content $secretsFile) {
    if ($line -match '^\s*([A-Z_]+)=(.+)$') { $secrets[$matches[1]] = $matches[2].Trim() }
}

$sshKey = Join-Path $HOME '.ssh\shinrikai-azure'
if (-not (Test-Path "$sshKey.pub")) {
    New-Item -ItemType Directory -Force (Split-Path $sshKey) | Out-Null
    # Via cmd: o PowerShell 5.1 descarta argumentos vazios, e o ssh-keygen precisa de -N "" (sem senha).
    cmd /c "ssh-keygen -q -t rsa -b 4096 -N `"`" -C shinrikai-azure -f `"$sshKey`""
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar a chave SSH.' }
}
$sshPublicKey = (Get-Content "$sshKey.pub" -Raw).Trim()

# ------------------------------------------------------------ Estado atual

$rgExists = (Invoke-Az group exists --name $ResourceGroup) -eq 'true'
$vmExists = $false
$images = @{ api = ''; web = '' }
if ($rgExists) {
    $vmExists = [bool](az vm list -g $ResourceGroup --query "[?name=='vm-shinrikai-db'].name" -o tsv)
    $apps = Invoke-Az resource list -g $ResourceGroup --resource-type Microsoft.App/containerApps --query '[].name' -o tsv
    foreach ($app in 'api', 'web') {
        if ($apps -contains "shinrikai-$app") {
            $images[$app] = Invoke-Az resource show -g $ResourceGroup -n "shinrikai-$app" --resource-type Microsoft.App/containerApps `
                --query 'properties.template.containers[0].image' -o tsv
        }
    }
}

function Deploy-Infra([string]$ApiImage, [string]$WebImage) {
    # Parâmetros num arquivo temporário, para as senhas não aparecerem na linha de comando.
    $parameters = @{
        '$schema'      = 'https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#'
        contentVersion = '1.0.0.0'
        parameters     = @{
            location             = @{ value = $Location }
            resourceGroupName    = @{ value = $ResourceGroup }
            sshPublicKey         = @{ value = $sshPublicKey }
            vmExists             = @{ value = $script:vmExists }
            firebirdRootPassword = @{ value = $secrets['FIREBIRD_ROOT_PASSWORD'] }
            firebirdPassword     = @{ value = $secrets['FIREBIRD_PASSWORD'] }
            jwtKey               = @{ value = $secrets['JWT_KEY'] }
            apiImage             = @{ value = $ApiImage }
            webImage             = @{ value = $WebImage }
            githubRepository     = @{ value = $Repository }
        }
    }
    $file = Join-Path ([IO.Path]::GetTempPath()) "shinrikai-params-$PID.json"
    [IO.File]::WriteAllText($file, ($parameters | ConvertTo-Json -Depth 5), (New-Object Text.UTF8Encoding($false)))
    try {
        if ($Preview) {
            Invoke-Az deployment sub what-if --name shinrikai --location $Location `
                --template-file (Join-Path $PSScriptRoot 'main.bicep') --parameters "@$file" | Out-Host
            return $null
        }
        Write-Host "Implantando infra/main.bicep em $Location ($ResourceGroup)..."
        $json = Invoke-Az deployment sub create --name shinrikai --location $Location `
            --template-file (Join-Path $PSScriptRoot 'main.bicep') --parameters "@$file" --query properties.outputs -o json
        $script:vmExists = $true
        return ($json | Out-String | ConvertFrom-Json)
    }
    finally {
        Remove-Item $file -ErrorAction SilentlyContinue
    }
}

# ------------------------------------------------------------ Implantação

$outputs = Deploy-Infra $images.api $images.web
if ($Preview) { return }

if (-not $SkipImages) {
    $registry = $outputs.registryName.value
    $server = $outputs.registryServer.value
    $tag = (git rev-parse --short HEAD).Trim()
    if (git status --porcelain) { $tag += '-local' }

    Invoke-Az acr login --name $registry | Out-Null
    foreach ($app in 'api', 'web') {
        $image = "$server/shinrikai-${app}:$tag"
        $dockerfile = if ($app -eq 'api') { 'Nienow.Shinrikai.Api/Dockerfile' } else { 'Nienow.Shinrikai.Web/Dockerfile' }
        Write-Host "Montando e enviando $image..."
        docker build -f $dockerfile -t $image .
        if ($LASTEXITCODE -ne 0) { throw "Falha ao montar $image." }
        docker push $image
        if ($LASTEXITCODE -ne 0) { throw "Falha ao enviar $image." }
        $images[$app] = $image
    }
    $outputs = Deploy-Infra $images.api $images.web
}

# Ids para o deploy.yml entrar no Azure por OIDC. Não são segredos: vão como variáveis do repositório.
$account = Invoke-Az account show -o json | Out-String | ConvertFrom-Json
foreach ($variable in @(@('AZURE_CLIENT_ID', $outputs.githubClientId.value), @('AZURE_TENANT_ID', $account.tenantId), @('AZURE_SUBSCRIPTION_ID', $account.id))) {
    gh variable set $variable[0] --repo $Repository --body $variable[1]
    if ($LASTEXITCODE -ne 0) { throw "Falha ao gravar $($variable[0]) em $Repository (gh auth status)." }
}

if ($outputs.webUrl.value) {
    Write-Host "Shinrikai no ar: $($outputs.webUrl.value)"
}
else {
    Write-Host 'Infraestrutura pronta. Os Container Apps são criados quando houver imagens (rode sem -SkipImages).'
}
