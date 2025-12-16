// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Annotation for referencing an Azure Network Security Group from a subnet resource.
/// </summary>
/// <param name="nsgResource">The Network Security Group resource being referenced.</param>
public class AzureNetworkSecurityGroupReferenceAnnotation(AzureNetworkSecurityGroupResource nsgResource)
    : IResourceAnnotation
{
    /// <summary>
    /// Gets the Network Security Group resource being referenced.
    /// </summary>
    public AzureNetworkSecurityGroupResource NsgResource { get; } = nsgResource ?? throw new ArgumentNullException(nameof(nsgResource));
}
