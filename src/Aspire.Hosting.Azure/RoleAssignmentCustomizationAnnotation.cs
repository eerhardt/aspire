// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// 
/// </summary>
public class RoleAssignmentCustomizationAnnotation(Action<AzureResourceInfrastructure, AzureProvisioningResource> configure) : IResourceAnnotation
{
    /// <summary>
    /// </summary>
    public Action<AzureResourceInfrastructure, AzureProvisioningResource> Configure { get; } = configure;

    /// <summary>
    /// The Azure resource that the current resource will interact with.
    /// </summary>
    /// <remarks>
    /// This is set when the <see cref="RoleAssignmentCustomizationAnnotation"/> is applied to a compute resource (e.g., Project or Container).
    /// </remarks>
    public AzureProvisioningResource? Target { get; set; }
}
