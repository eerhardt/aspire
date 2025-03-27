@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param keyVaultName string

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' = {
  name: take('cosmos-${uniqueString(resourceGroup().id)}', 44)
  location: location
  properties: {
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    disableLocalAuth: false
  }
  kind: 'GlobalDocumentDB'
  tags: {
    'aspire-resource-name': 'cosmos'
  }
}

resource db 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-08-15' = {
  name: 'db'
  location: location
  properties: {
    resource: {
      id: 'db'
    }
  }
  parent: cosmos
}

resource entries 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'staging-entries'
  location: location
  properties: {
    resource: {
      id: 'staging-entries'
      partitionKey: {
        paths: [
          '/id'
        ]
      }
    }
  }
  parent: db
}

resource users 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-08-15' = {
  name: 'users'
  location: location
  properties: {
    resource: {
      id: 'users'
      partitionKey: {
        paths: [
          '/id'
        ]
      }
    }
  }
  parent: db
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--cosmos'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey}'
  }
  parent: keyVault
}

resource db_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--db'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=db'
  }
  parent: keyVault
}

resource entries_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--entries'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=db;Container=staging-entries'
  }
  parent: keyVault
}

resource users_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  name: 'connectionstrings--users'
  properties: {
    value: 'AccountEndpoint=${cosmos.properties.documentEndpoint};AccountKey=${cosmos.listKeys().primaryMasterKey};Database=db;Container=users'
  }
  parent: keyVault
}

output name string = cosmos.name