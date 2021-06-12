param projectName string = ''
param statesetid string = ''
param azuremapskey string = ''
param datasetid string =''
param utcValue string = utcNow()

var location = resourceGroup().location
var unique = substring(uniqueString(resourceGroup().id),3)
//var unique = ''

var adtName = '${projectName}adt${unique}'
var storageName = '${projectName}4maps${unique}'
var funcAppName = '${projectName}funcapp4maps${unique}'
var serverFarmName = '${projectName}farm4maps${unique}'
var appInightsName = '${projectName}appinsight${unique}'
var eventGridCLTopicName = '${projectName}cls4maps${unique}'
var identityName = '${projectName}scriptidentity'

// referrence appInsights
resource appInsights 'Microsoft.Insights/components@2020-02-02-preview' existing = {
  name: appInightsName
}

// referrence identity
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing = {
  name: identityName
}

// reference ADT instance
resource adt 'Microsoft.DigitalTwins/digitalTwinsInstances@2020-12-01' existing = {
  name: adtName
}

//create storage account (used by the azure function app)
resource storage 'Microsoft.Storage/storageAccounts@2018-02-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    isHnsEnabled: false
  }
}

// create App Plan - "server farm"
resource appserver 'Microsoft.Web/serverfarms@2018-02-01' = {
  name: serverFarmName
  location: location
  kind: 'functionapp'
  sku: {
    tier: 'Dynamic'
    name: 'B1'
  }
}

// create Function app for hosting the IoTHub ingress and SignalR egress
resource funcApp 'Microsoft.Web/sites@2018-11-01' = {
  name: funcAppName
  kind: 'functionapp'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'GEOGRAPHY'
          value: 'us'
        }
        {
          name: 'STATE_SET_ID'
          value: '${statesetid}'
        }        
        {
          name: 'DATA_SET_ID'
          value: '${datasetid}'
        }
        {
          name: 'AZURE_MAPS_SUBSCRIPTION_KEY'
          value: '${azuremapskey}'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageName};AccountKey=${listKeys(storageName, '2019-06-01').keys[0].value}'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }                
      ]
    }
    clientAffinityEnabled: false
  }
}

// Deploy function code from zip
resource ingestfunction 'Microsoft.Web/sites/extensions@2021-01-01' = {
  name: '${funcApp.name}/MSDeploy'
  properties: {
    packageUri: 'https://github.com/TakahiroMiyaura/AzureMapsIndoorDataSamples/raw/main/ARM-Template/functions/zipfiles/update-maps-featurestate.zip'
    dbType: 'None'
    connectionString: ''
  }
  dependsOn: [
    funcApp
  ]
}

resource eventGridChangeLogTopic 'Microsoft.EventGrid/topics@2020-10-15-preview' = {
  name: eventGridCLTopicName
  location: location
  sku: {
    name: 'Basic'
  }
  kind: 'Azure'
  identity: {
    type: 'None'
  }
  properties: {
    inputSchema: 'EventGridSchema'
    publicNetworkAccess: 'Enabled'
  }  
  dependsOn: [
    ingestfunction
    funcApp
  ]
}


// execute post deployment script
resource PostDeploymentscript 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'PostDeploymentscript'
  location: resourceGroup().location
  kind: 'AzureCLI'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
    properties: {
    forceUpdateTag: utcValue
    azCliVersion: '2.15.0'
    arguments: '${adt.name} ${resourceGroup().name} ${eventGridChangeLogTopic.name} ${eventGridChangeLogTopic.id} ${funcApp.id}'
    primaryScriptUri: 'https://raw.githubusercontent.com/TakahiroMiyaura/AzureMapsIndoorDataSamples/main/ARM-Template/postdeploy.sh'
    supportingScriptUris: []
    timeout: 'PT30M'
    cleanupPreference: 'OnExpiration'
    retentionInterval: 'P1D'
  }
  dependsOn: [
    identity
    eventGridChangeLogTopic
  ]
}
