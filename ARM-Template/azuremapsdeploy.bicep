param projectName string = ''

var unique = substring(uniqueString(resourceGroup().id),3)
//var unique = ''

var maps = '${projectName}maps${unique}'
var mapsCreators = '${projectName}mc${unique}'


// create Azure Maps
resource azureMaps 'Microsoft.Maps/accounts@2021-02-01' = {
  name: maps
  location: 'global'
  sku: {
    name: 'G2'
  }
  kind: 'Gen2'
  tags: {}
  properties: {
    disableLocalAuth: false
}
}

// create Creator
resource mapsCreator 'Microsoft.Maps/accounts/creators@2021-02-01' = {
  name: '${maps}/${mapsCreators}'
  location: 'westus2' // for Preview release,value fixes...
  tags: {}
  dependsOn: [
    azureMaps
  ]
  properties: {
      storageUnits: 1
  }
}

