// Reconcile — Azure deployment (App Service + Azure SQL), infrastructure as code.
//
// Provisions the smallest footprint that runs the app for real:
//   - App Service plan (Linux, B1) + Web App running .NET 10
//   - Azure SQL logical server + a Basic-tier database
//   - The connection string injected as an app setting, so the app needs no
//     config change between local Docker SQL Server and Azure SQL.
//
// Deploy (from repo root, after `az login`):
//   az group create -n rg-reconcile -l westus2
//   az deployment group create -g rg-reconcile -f infra/main.bicep \
//       -p sqlAdminPassword='<strong password>'
//   dotnet publish src/Reconcile.Web -c Release -o publish
//   cd publish && zip -r ../app.zip . && cd ..
//   az webapp deploy -g rg-reconcile -n <webAppName output> --src-path app.zip
//   dotnet ef database update --project src/Reconcile.Web \
//       --connection "<connection string output>"
//
// Status: compiles clean with `bicep build` (Bicep CLI 0.46); not yet
// exercised against a live subscription. See BUILD-LOG.md.

@description('Short name used to derive resource names. Lowercase letters and digits.')
@minLength(3)
@maxLength(12)
param baseName string = 'reconcile'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('SQL administrator login.')
param sqlAdminLogin string = 'reconcileadmin'

@description('SQL administrator password. Pass at deploy time; never commit.')
@secure()
param sqlAdminPassword string

var suffix = uniqueString(resourceGroup().id)
var planName = 'plan-${baseName}-${suffix}'
var webAppName = 'app-${baseName}-${suffix}'
var sqlServerName = 'sql-${baseName}-${suffix}'
var sqlDbName = 'Reconcile'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  kind: 'linux'
  sku: { name: 'B1', tier: 'Basic' }
  properties: { reserved: true }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDbName
  location: location
  sku: { name: 'Basic', tier: 'Basic' }
}

// Let the App Service reach the database over Azure's backbone.
resource allowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

var connectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDbName};User Id=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30'

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false
      ftpsState: 'Disabled'
      connectionStrings: [
        { name: 'Reconcile', connectionString: connectionString, type: 'SQLAzure' }
      ]
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
      ]
    }
  }
}

output webAppName string = web.name
output webAppUrl string = 'https://${web.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
