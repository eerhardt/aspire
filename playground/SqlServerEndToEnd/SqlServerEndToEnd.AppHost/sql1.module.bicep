@description('The location for the resource(s) to be deployed.')
param location string = resourceGroup().location

param principalId string

param principalName string

resource sql1 'Microsoft.Sql/servers@2021-11-01' existing = {
  name: 'sql1-yfs36ajuaysoy'
}

resource sql1_admin 'Microsoft.Sql/servers/administrators@2021-11-01' = {
  name: 'ActiveDirectory'
  properties: {
    login: principalName
    sid: principalId
  }
  parent: sql1
}

resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  name: 'AllowAllAzureIps'
  properties: {
    endIpAddress: '0.0.0.0'
    startIpAddress: '0.0.0.0'
  }
  parent: sql1
}

resource db1 'Microsoft.Sql/servers/databases@2021-11-01' = {
  name: 'db1'
  location: location
  parent: sql1
}

output sqlServerFqdn string = sql1.properties.fullyQualifiedDomainName