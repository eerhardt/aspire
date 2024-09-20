// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Azure.Provisioning.Storage;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Provisioning versions for <see cref="AzureStorageResource"/>.
/// </summary>
/// <remarks>
/// You can select a specific version of Azure Storage defaults to maintain a consistent
/// set of provisioning defaults.
/// </remarks>
public static class AzureStorageResourceVersions
{
    /// <summary>
    /// The first version of Azure Storage provisioning.
    /// </summary>
    /// <remarks>
    /// Produces the following bicep code:
    /// 
    /// <code lang="bicep">
    /// @description('The location for the resource(s) to be deployed.')
    /// param location string = resourceGroup().location
    /// 
    /// param principalId string
    /// 
    /// param principalType string
    /// 
    /// resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
    ///   name: toLower(take('storage${uniqueString(resourceGroup().id)}', 24))
    ///   kind: 'StorageV2'
    ///   location: location
    ///   sku: {
    ///     name: 'Standard_GRS'
    ///   }
    ///   properties: {
    ///     accessTier: 'Hot'
    ///     allowSharedKeyAccess: false
    ///     minimumTlsVersion: 'TLS1_2'
    ///     networkAcls: {
    ///       defaultAction: 'Allow'
    ///     }
    ///   }
    ///   tags: {
    ///     'aspire-resource-name': 'storage'
    ///   }
    /// }
    /// 
    /// resource blobs 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
    ///   name: 'default'
    ///   parent: storage
    /// }
    /// 
    /// resource storage_StorageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    ///   name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'))
    ///   properties: {
    ///     principalId: principalId
    ///     roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    ///     principalType: principalType
    ///   }
    ///   scope: storage
    /// }
    /// 
    /// resource storage_StorageTableDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    ///   name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'))
    ///   properties: {
    ///     principalId: principalId
    ///     roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
    ///     principalType: principalType
    ///   }
    ///   scope: storage
    /// }
    /// 
    /// resource storage_StorageQueueDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    ///   name: guid(storage.id, principalId, subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88'))
    ///   properties: {
    ///     principalId: principalId
    ///     roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '974c5e8b-45b9-4653-ba55-5f855dd0fb88')
    ///     principalType: principalType
    ///   }
    ///   scope: storage
    /// }
    /// 
    /// output blobEndpoint string = storage.properties.primaryEndpoints.blob
    /// 
    /// output queueEndpoint string = storage.properties.primaryEndpoints.queue
    /// 
    /// output tableEndpoint string = storage.properties.primaryEndpoints.table
    /// </code>
    /// </remarks>
    public static Action<ResourceModuleConstruct> Version1 { get; } = (construct) =>
    {
        var storageAccount = new StorageAccount(construct.Resource.Name)
        {
            Kind = StorageKind.StorageV2,
            AccessTier = StorageAccountAccessTier.Hot,
            Sku = new StorageSku() { Name = StorageSkuName.StandardGrs },
            NetworkRuleSet = new StorageAccountNetworkRuleSet()
            {
                // Unfortunately Azure Storage does not list ACA as one of the resource types in which
                // the AzureServices firewall policy works. This means that we need this Azure Storage
                // account to have its default action set to Allow.
                DefaultAction = StorageNetworkDefaultAction.Allow
            },
            // Set the minimum TLS version to 1.2 to ensure resources provisioned are compliant
            // with the pending deprecation of TLS 1.0 and 1.1.
            MinimumTlsVersion = StorageMinimumTlsVersion.Tls1_2,
            // Disable shared key access to the storage account as managed identity is configured
            // to access the storage account by default.
            AllowSharedKeyAccess = false,
            Tags = { { "aspire-resource-name", construct.Resource.Name } }
        };
        construct.Add(storageAccount);

        var blobs = new BlobService("blobs")
        {
            Parent = storageAccount
        };
        construct.Add(blobs);

        construct.Add(storageAccount.AssignRole(StorageBuiltInRole.StorageBlobDataContributor, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));
        construct.Add(storageAccount.AssignRole(StorageBuiltInRole.StorageTableDataContributor, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));
        construct.Add(storageAccount.AssignRole(StorageBuiltInRole.StorageQueueDataContributor, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));

        construct.Add(new BicepOutput("blobEndpoint", typeof(string)) { Value = storageAccount.PrimaryEndpoints.Value!.BlobUri });
        construct.Add(new BicepOutput("queueEndpoint", typeof(string)) { Value = storageAccount.PrimaryEndpoints.Value!.QueueUri });
        construct.Add(new BicepOutput("tableEndpoint", typeof(string)) { Value = storageAccount.PrimaryEndpoints.Value!.TableUri });
    };
}

