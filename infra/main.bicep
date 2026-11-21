// Shinrikai no Azure: grupo de recursos + tudo o que roda dentro dele (resources.bicep).
// Implantado pelo infra/deploy.ps1, que gera os segredos e envia as imagens.
targetScope = 'subscription'

@description('Região de todos os recursos. Precisa oferecer o tamanho de VM escolhido para a assinatura.')
param location string = 'northcentralus'

@description('Grupo de recursos criado (ou atualizado) pela implantação.')
param resourceGroupName string = 'rg-shinrikai'

@description('Tamanho da VM do Firebird.')
param vmSize string = 'Standard_B2ats_v2'

@description('Chave pública SSH da VM (o acesso do dia a dia é por "az vm run-command", sem SSH aberto).')
param sshPublicKey string

@description('true quando a VM já existe: o customData (cloud-init) não pode mudar depois da criação.')
param vmExists bool = false

@secure()
@description('Senha do SYSDBA dentro do container do Firebird.')
param firebirdRootPassword string

@secure()
@description('Senha do usuário "shinrikai" do Firebird, usado pela API.')
param firebirdPassword string

@secure()
@description('Chave HMAC do JWT (mínimo de 32 bytes).')
param jwtKey string

@description('Imagem da API no registry. Vazia: os Container Apps não são criados nem alterados.')
param apiImage string = ''

@description('Imagem da interface no registry. Vazia: os Container Apps não são criados nem alterados.')
param webImage string = ''

@description('Repositório (dono/nome) cujo workflow de deploy, no master, pode entrar no Azure por OIDC.')
param githubRepository string = 'ecmnienow/Shinrikai'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
}

module resources 'resources.bicep' = {
  name: 'shinrikai-resources'
  scope: rg
  params: {
    location: location
    vmSize: vmSize
    sshPublicKey: sshPublicKey
    vmExists: vmExists
    firebirdRootPassword: firebirdRootPassword
    firebirdPassword: firebirdPassword
    jwtKey: jwtKey
    apiImage: apiImage
    webImage: webImage
    githubRepository: githubRepository
  }
}

output resourceGroup string = rg.name
output registryName string = resources.outputs.registryName
output registryServer string = resources.outputs.registryServer
output githubClientId string = resources.outputs.githubClientId
output webUrl string = resources.outputs.webUrl
