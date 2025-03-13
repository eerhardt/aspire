@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param storage_outputs_name string

param account_outputs_name string

resource api_identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: take('api_identity-${uniqueString(resourceGroup().id)}', 128)
  location: location
}

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storage_outputs_name
}

resource storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, api_identity.id, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
  properties: {
    principalId: api_identity.properties.principalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalType: 'ServicePrincipal'
  }
  scope: storage
}

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-08-15' existing = {
  name: account_outputs_name
}

resource account_roleDefinition 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-08-15' existing = {
  name: '00000000-0000-0000-0000-000000000002'
  parent: account
}

resource account_roleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2024-08-15' = {
  name: guid(api_identity.id, account_roleDefinition.id, account.id)
  properties: {
    principalId: api_identity.properties.principalId
    roleDefinitionId: account_roleDefinition.id
    scope: account.id
  }
  parent: account
}

output id string = api_identity.id

output clientId string = api_identity.properties.clientId

output principalId string = api_identity.properties.principalId

output principalName string = api_identity.name