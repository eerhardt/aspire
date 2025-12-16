// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.Network;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Network Security Group resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure Network Security Group resource.</param>
public class AzureNetworkSecurityGroupResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure)
{
    /// <summary>
    /// Gets the list of security rules configured for this Network Security Group.
    /// </summary>
    public IReadOnlyList<SecurityRuleConfig> SecurityRules => _securityRules;
    
    internal List<SecurityRuleConfig> _securityRules = [];

    /// <summary>
    /// Gets the "id" output reference from the Azure Network Security Group resource.
    /// </summary>
    public BicepOutputReference Id => new("id", this);

    /// <summary>
    /// Gets the "name" output reference for the resource.
    /// </summary>
    public BicepOutputReference NameOutput => new("name", this);

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var resources = infra.GetProvisionableResources();

        // Check if a NetworkSecurityGroup with the same identifier already exists
        var existingNsg = resources.OfType<NetworkSecurityGroup>().SingleOrDefault(nsg => nsg.BicepIdentifier == bicepIdentifier);

        if (existingNsg is not null)
        {
            return existingNsg;
        }

        // Create and add new resource if it doesn't exist
        var nsg = NetworkSecurityGroup.FromExisting(bicepIdentifier);

        if (!TryApplyExistingResourceAnnotation(
            this,
            infra,
            nsg))
        {
            nsg.Name = NameOutput.AsProvisioningParameter(infra);
        }

        infra.Add(nsg);
        return nsg;
    }
}

/// <summary>
/// Configuration for a Network Security Group security rule.
/// </summary>
public record SecurityRuleConfig
{
    /// <summary>
    /// Gets the name of the security rule.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the direction of the security rule (Inbound or Outbound).
    /// </summary>
    public required SecurityRuleDirection Direction { get; init; }

    /// <summary>
    /// Gets the priority of the rule (100-4096).
    /// </summary>
    public required int Priority { get; init; }

    /// <summary>
    /// Gets the access type (Allow or Deny).
    /// </summary>
    public required SecurityRuleAccess Access { get; init; }

    /// <summary>
    /// Gets the protocol (Tcp, Udp, Icmp, Esp, Ah, or "*").
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Gets the source address prefix (IP address, CIDR, or tag like "Internet", "VirtualNetwork").
    /// </summary>
    public string SourceAddressPrefix { get; init; } = "*";

    /// <summary>
    /// Gets the source port or range (e.g., "80", "80-100", or "*").
    /// </summary>
    public string SourcePortRange { get; init; } = "*";

    /// <summary>
    /// Gets the destination address prefix.
    /// </summary>
    public string DestinationAddressPrefix { get; init; } = "*";

    /// <summary>
    /// Gets the destination port or range.
    /// </summary>
    public string DestinationPortRange { get; init; } = "*";

    /// <summary>
    /// Gets the optional description for the rule.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the list of source address prefixes (used instead of SourceAddressPrefix for multiple values).
    /// </summary>
    public List<string>? SourceAddressPrefixes { get; init; }

    /// <summary>
    /// Gets the list of source port ranges (used instead of SourcePortRange for multiple values).
    /// </summary>
    public List<string>? SourcePortRanges { get; init; }

    /// <summary>
    /// Gets the list of destination address prefixes (used instead of DestinationAddressPrefix for multiple values).
    /// </summary>
    public List<string>? DestinationAddressPrefixes { get; init; }

    /// <summary>
    /// Gets the list of destination port ranges (used instead of DestinationPortRange for multiple values).
    /// </summary>
    public List<string>? DestinationPortRanges { get; init; }
}
