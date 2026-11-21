// Container Apps web (público) e api (interno), como no compose.yaml, e a VM do Firebird, ligados por
// uma VNet em que a porta 3050 da VM só aceita a sub-rede dos apps.
targetScope = 'resourceGroup'

param location string
param vmSize string
param sshPublicKey string
param vmExists bool
param adminUsername string = 'shinrikai'

@secure()
param firebirdRootPassword string

@secure()
param firebirdPassword string

@secure()
param jwtKey string

param apiImage string
param webImage string
param githubRepository string

var prefix = 'shinrikai'
var deployApps = !empty(apiImage) && !empty(webImage)
var appsSubnetPrefix = '10.10.0.0/23'
var dbPrivateIp = '10.10.2.4'
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'

// ------------------------------------------------------------------ Logs e registry

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${prefix}'
  location: location
  properties: { sku: { name: 'PerGB2018' }, retentionInDays: 30 }
}

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'cr${prefix}${uniqueString(resourceGroup().id)}'
  location: location
  sku: { name: 'Basic' }
}

// Identidade dos Container Apps para baixar as imagens do registry, sem senha.
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${prefix}'
  location: location
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, acrPullRoleId)
  scope: registry
  properties: {
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
  }
}

// Identidade do workflow: só aceita tokens do repositório no master. O Contributor no grupo cobre enviar
// imagens ao registry (modo LegacyRegistryPermissions) e atualizar os apps.
resource githubIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${prefix}-github'
  location: location
}

resource githubCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: githubIdentity
  name: 'github-master'
  properties: {
    issuer: 'https://token.actions.githubusercontent.com'
    subject: 'repo:${githubRepository}:ref:refs/heads/master'
    audiences: [ 'api://AzureADTokenExchange' ]
  }
}

resource githubContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, githubIdentity.id, contributorRoleId)
  properties: {
    principalId: githubIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
  }
}

// ------------------------------------------------------------------ Rede

resource dbNsg 'Microsoft.Network/networkSecurityGroups@2024-05-01' = {
  name: 'nsg-${prefix}-db'
  location: location
  properties: {
    securityRules: [
      {
        name: 'firebird-from-apps'
        properties: {
          priority: 100
          direction: 'Inbound'
          access: 'Allow'
          protocol: 'Tcp'
          sourceAddressPrefix: appsSubnetPrefix
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '3050'
        }
      }
      {
        // Nada mais entra na VM: nem SSH, nem a internet, nem o resto da VNet.
        name: 'deny-all-inbound'
        properties: {
          priority: 4096
          direction: 'Inbound'
          access: 'Deny'
          protocol: '*'
          sourceAddressPrefix: '*'
          sourcePortRange: '*'
          destinationAddressPrefix: '*'
          destinationPortRange: '*'
        }
      }
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: 'vnet-${prefix}'
  location: location
  properties: {
    addressSpace: { addressPrefixes: [ '10.10.0.0/16' ] }
    subnets: [
      {
        name: 'snet-apps'
        properties: {
          addressPrefix: appsSubnetPrefix
          delegations: [
            {
              name: 'container-apps'
              properties: { serviceName: 'Microsoft.App/environments' }
            }
          ]
        }
      }
      {
        name: 'snet-db'
        properties: {
          addressPrefix: '10.10.2.0/24'
          networkSecurityGroup: { id: dbNsg.id }
        }
      }
    ]
  }
}

// ------------------------------------------------------------------ VM do Firebird

// IP público só para a saída (apt e Docker Hub): o NSG não deixa nada entrar por ele.
resource dbPublicIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: 'pip-${prefix}-db'
  location: location
  sku: { name: 'Standard' }
  properties: { publicIPAllocationMethod: 'Static' }
}

resource dbNic 'Microsoft.Network/networkInterfaces@2024-05-01' = {
  name: 'nic-${prefix}-db'
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'ipconfig'
        properties: {
          subnet: { id: vnet.properties.subnets[1].id }
          privateIPAllocationMethod: 'Static'
          privateIPAddress: dbPrivateIp
          publicIPAddress: { id: dbPublicIp.id }
        }
      }
    ]
  }
}

// cloud-init: instala o Docker, prepara o disco de dados e sobe o Firebird com schema + seed.
var cloudInit = replace(replace(replace(replace(replace(loadTextContent('cloud-init.yaml'),
  '__FIREBIRD_ROOT_PASSWORD__', firebirdRootPassword),
  '__FIREBIRD_PASSWORD__', firebirdPassword),
  '__SCHEMA_B64__', loadFileAsBase64('../database/schema.sql')),
  '__SEED_B64__', loadFileAsBase64('../database/seed.sql')),
  '__INIT_B64__', loadFileAsBase64('../database/docker/init-shinrikai.sh'))

resource vm 'Microsoft.Compute/virtualMachines@2024-07-01' = if (!vmExists) {
  name: 'vm-${prefix}-db'
  location: location
  properties: {
    hardwareProfile: { vmSize: vmSize }
    osProfile: {
      computerName: '${prefix}-db'
      adminUsername: adminUsername
      customData: base64(cloudInit)
      linuxConfiguration: {
        disablePasswordAuthentication: true
        ssh: {
          publicKeys: [
            {
              path: '/home/${adminUsername}/.ssh/authorized_keys'
              keyData: sshPublicKey
            }
          ]
        }
      }
    }
    storageProfile: {
      imageReference: {
        publisher: 'Canonical'
        offer: 'ubuntu-24_04-lts'
        sku: 'server'
        version: 'latest'
      }
      osDisk: {
        createOption: 'FromImage'
        managedDisk: { storageAccountType: 'StandardSSD_LRS' }
        deleteOption: 'Delete'
      }
      dataDisks: [
        {
          // O banco fica aqui. Apagar a VM não apaga este disco.
          lun: 0
          name: 'disk-${prefix}-firebird'
          createOption: 'Empty'
          diskSizeGB: 8
          managedDisk: { storageAccountType: 'StandardSSD_LRS' }
          deleteOption: 'Detach'
        }
      ]
    }
    networkProfile: { networkInterfaces: [ { id: dbNic.id } ] }
    // Log do console (cloud-init) visível no portal e no "az vm boot-diagnostics".
    diagnosticsProfile: { bootDiagnostics: { enabled: true } }
  }
}

// ------------------------------------------------------------------ Container Apps

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${prefix}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: vnet.properties.subnets[0].id
      internal: false
    }
    workloadProfiles: [ { name: 'Consumption', workloadProfileType: 'Consumption' } ]
  }
}

var registries = [ { server: registry.properties.loginServer, identity: identity.id } ]
var appIdentity = {
  type: 'UserAssigned'
  userAssignedIdentities: { '${identity.id}': {} }
}
// Escala a zero sem acesso: a primeira requisição depois de um tempo parado demora alguns segundos.
var scale = { minReplicas: 0, maxReplicas: 1 }
var containerResources = { cpu: json('0.25'), memory: '0.5Gi' }

resource api 'Microsoft.App/containerApps@2024-03-01' = if (deployApps) {
  name: '${prefix}-api'
  location: location
  identity: appIdentity
  dependsOn: [ acrPull ]
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      // Só a interface (mesmo ambiente) chega na API, em http://shinrikai-api.
      ingress: { external: false, targetPort: 8080, transport: 'http', allowInsecure: true }
      registries: registries
      secrets: [
        {
          name: 'db-connection'
          // Montada a partir de firebirdPassword, que já é @secure.
          #disable-next-line use-secure-value-for-secure-inputs
          value:'character set=UTF8;initial catalog=/var/lib/firebird/data/shinrikai.fdb;data source=${dbPrivateIp};port=3050;dialect=3;user id=shinrikai;password=${firebirdPassword}'
        }
        { name: 'jwt-key', value: jwtKey }
      ]
    }
    template: {
      containers: [
        {
          name: 'api'
          image: apiImage
          resources: containerResources
          env: [
            { name: 'ConnectionStrings__ShinrikaiDB', secretRef: 'db-connection' }
            { name: 'Jwt__Key', secretRef: 'jwt-key' }
            { name: 'ReverseProxy__Enabled', value: 'true' }
          ]
        }
      ]
      scale: scale
    }
  }
}

resource web 'Microsoft.App/containerApps@2024-03-01' = if (deployApps) {
  name: '${prefix}-web'
  location: location
  identity: appIdentity
  dependsOn: [ acrPull ]
  properties: {
    environmentId: environment.id
    workloadProfileName: 'Consumption'
    configuration: {
      ingress: { external: true, targetPort: 8080, transport: 'http' }
      registries: registries
    }
    template: {
      containers: [
        {
          name: 'web'
          image: webImage
          resources: containerResources
          env: [
            { name: 'ReverseProxy__Clusters__api__Destinations__shinrikai-api__Address', value: 'http://${prefix}-api/' }
          ]
        }
      ]
      scale: scale
    }
  }
}

output registryName string = registry.name
output registryServer string = registry.properties.loginServer
output githubClientId string = githubIdentity.properties.clientId
output webUrl string =deployApps ? 'https://${web!.properties.configuration.ingress.fqdn}' : ''
