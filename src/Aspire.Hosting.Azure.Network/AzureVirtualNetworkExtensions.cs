// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Network;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure Virtual Network resources to the application model.
/// </summary>
public static class AzureVirtualNetworkExtensions
{
    /// <summary>
    /// Adds an Azure Virtual Network resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure Virtual Network resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureVirtualNetworkResource}"/>.</returns>
    public static IResourceBuilder<AzureVirtualNetworkResource> AddAzureVirtualNetwork(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name)
    {
        return builder.AddAzureVirtualNetwork(name, null);
    }

    /// <summary>
    /// Adds an Azure Virtual Network resource to the application model with a specified address prefix.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure Virtual Network resource.</param>
    /// <param name="addressPrefix">The address prefix for the virtual network (e.g., "10.0.0.0/16"). If null, defaults to "10.0.0.0/16".</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureVirtualNetworkResource}"/>.</returns>
    public static IResourceBuilder<AzureVirtualNetworkResource> AddAzureVirtualNetwork(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        string? addressPrefix)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        AzureVirtualNetworkResource resource = new(name, ConfigureVirtualNetwork);
        return builder.AddResource(resource);

        void ConfigureVirtualNetwork(AzureResourceInfrastructure infra)
        {
            var vnet = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infra,
                (identifier, name) =>
                {
                    var resource = VirtualNetwork.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) =>
                {
                    var vnet = new VirtualNetwork(infrastructure.AspireResource.GetBicepIdentifier())
                    {
                        AddressSpace = new VirtualNetworkAddressSpace()
                        {
                            AddressPrefixes = { addressPrefix ?? "10.0.0.0/16" }
                        },
                        Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                    };

                    return vnet;
                });

            var azureResource = (AzureVirtualNetworkResource)infra.AspireResource;

            // Add subnets
            if (azureResource.Subnets.Count > 0)
            {
                foreach (var subnet in azureResource.Subnets)
                {
                    var cdkSubnet = subnet.ToProvisioningEntity(infra);
                    cdkSubnet.Parent = vnet;
                    infra.Add(cdkSubnet);
                }
            }

            // Output the VNet ID for references
            infra.Add(new ProvisioningOutput("id", typeof(string))
            {
                Value = vnet.Id
            });

            // We need to output name so it can be referenced by others.
            infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = vnet.Name });
        }
    }

    /// <summary>
    /// Adds an Azure Subnet to the Virtual Network.
    /// </summary>
    /// <param name="builder">The Virtual Network resource builder.</param>
    /// <param name="name">The name of the subnet resource.</param>
    /// <param name="addressPrefix">The address prefix for the subnet (e.g., "10.0.1.0/24").</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/>.</returns>
    public static IResourceBuilder<AzureSubnetResource> AddSubnet(
        this IResourceBuilder<AzureVirtualNetworkResource> builder,
        [ResourceName] string name,
        string addressPrefix)
    {
        return builder.AddSubnet(name, null, addressPrefix);
    }

    /// <summary>
    /// Adds an Azure Subnet to the Virtual Network.
    /// </summary>
    /// <param name="builder">The Virtual Network resource builder.</param>
    /// <param name="name">The name of the subnet resource.</param>
    /// <param name="subnetName">The subnet name in Azure. If null, the resource name is used.</param>
    /// <param name="addressPrefix">The address prefix for the subnet (e.g., "10.0.1.0/24").</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/>.</returns>
    public static IResourceBuilder<AzureSubnetResource> AddSubnet(
        this IResourceBuilder<AzureVirtualNetworkResource> builder,
        [ResourceName] string name,
        string? subnetName,
        string addressPrefix)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(addressPrefix);

        subnetName ??= name;

        var subnet = new AzureSubnetResource(name, subnetName, addressPrefix, builder.Resource);

        builder.Resource.Subnets.Add(subnet);
        return builder.ApplicationBuilder.AddResource(subnet)
            .ExcludeFromManifest();
    }

    ///// <summary>
    ///// Adds an Azure Public IP Address resource to the application model.
    ///// </summary>
    ///// <param name="builder">The builder for the distributed application.</param>
    ///// <param name="name">The name of the Azure Public IP Address resource.</param>
    ///// <returns>A reference to the <see cref="IResourceBuilder{AzurePublicIpResource}"/>.</returns>
    ///// <remarks>
    ///// By default references to the Azure Public IP Address resource will be assigned the following roles:
    ///// 
    ///// - <see cref="NetworkBuiltInRole.NetworkContributor"/>
    /////
    ///// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzurePublicIpResource}, NetworkBuiltInRole[])"/>.
    ///// </remarks>
    //public static IResourceBuilder<AzurePublicIpResource> AddAzurePublicIP(
    //    this IDistributedApplicationBuilder builder,
    //    [ResourceName] string name)
    //{
    //    ArgumentNullException.ThrowIfNull(builder);
    //    ArgumentException.ThrowIfNullOrEmpty(name);

    //    builder.AddAzureProvisioning();

    //    AzurePublicIpResource resource = new(name, ConfigurePublicIp);
    //    return builder.AddResource(resource)
    //        .WithDefaultRoleAssignments(NetworkBuiltInRole.GetBuiltInRoleName,
    //            NetworkBuiltInRole.NetworkContributor);

    //    void ConfigurePublicIp(AzureResourceInfrastructure infra)
    //    {
    //        var publicIp = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infra,
    //            (identifier, name) =>
    //            {
    //                var resource = PublicIPAddress.FromExisting(identifier);
    //                resource.Name = name;
    //                return resource;
    //            },
    //            (infra) =>
    //            {
    //                var azureResource = (AzurePublicIpResource)infra.AspireResource;
    //                var publicIp = new PublicIPAddress(infra.AspireResource.GetBicepIdentifier())
    //                {
    //                    PublicIPAllocationMethod = azureResource.AllocationMethod != null
    //                        ? BicepValue<NetworkIPAllocationMethod>.DefineProperty(publicIp, nameof(PublicIPAddress.PublicIPAllocationMethod), ["properties", "publicIPAllocationMethod"], defaultValue: new BicepString(azureResource.AllocationMethod))
    //                        : BicepValue<NetworkIPAllocationMethod>.DefineProperty(publicIp, nameof(PublicIPAddress.PublicIPAllocationMethod), ["properties", "publicIPAllocationMethod"], defaultValue: NetworkIPAllocationMethod.Static),
    //                    Sku = azureResource.Sku != null
    //                        ? new PublicIPAddressSku { Name = new BicepString(azureResource.Sku) }
    //                        : new PublicIPAddressSku { Name = PublicIPAddressSkuName.Standard },
    //                    Tags = { { "aspire-resource-name", infra.AspireResource.Name } }
    //                };

    //                if (azureResource.DnsName != null)
    //                {
    //                    publicIp.DnsSettings = new PublicIPAddressDnsSettings
    //                    {
    //                        DomainNameLabel = azureResource.DnsName
    //                    };
    //                }

    //                return publicIp;
    //            });

    //        // Output the Public IP ID and IP Address for references
    //        infra.Add(new ProvisioningOutput("id", typeof(string))
    //        {
    //            Value = publicIp.Id
    //        });

    //        infra.Add(new ProvisioningOutput("ipAddress", typeof(string))
    //        {
    //            Value = publicIp.IPAddress
    //        });

    //        // We need to output name to externalize role assignments.
    //        infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = publicIp.Name });
    //    }
    //}

    ///// <summary>
    ///// Adds an Azure NAT Gateway resource to the application model.
    ///// </summary>
    ///// <param name="builder">The builder for the distributed application.</param>
    ///// <param name="name">The name of the Azure NAT Gateway resource.</param>
    ///// <returns>A reference to the <see cref="IResourceBuilder{AzureNatGatewayResource}"/>.</returns>
    //public static IResourceBuilder<AzureNatGatewayResource> AddAzureNatGateway(
    //    this IDistributedApplicationBuilder builder,
    //    [ResourceName] string name)
    //{
    //    ArgumentNullException.ThrowIfNull(builder);
    //    ArgumentException.ThrowIfNullOrEmpty(name);

    //    builder.AddAzureProvisioning();

    //    AzureNatGatewayResource resource = new(name, ConfigureNatGateway);
    //    return builder.AddResource(resource)
    //        .WithDefaultRoleAssignments(NetworkBuiltInRole.GetBuiltInRoleName,
    //            NetworkBuiltInRole.NetworkContributor);

    //    void ConfigureNatGateway(AzureResourceInfrastructure infra)
    //    {
    //        var natGateway = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infra,
    //            (identifier, name) =>
    //            {
    //                var resource = NatGateway.FromExisting(identifier);
    //                resource.Name = name;
    //                return resource;
    //            },
    //            (infra) =>
    //            {
    //                var azureResource = (AzureNatGatewayResource)infra.AspireResource;
    //                var natGw = new NatGateway(infra.AspireResource.GetBicepIdentifier())
    //                {
    //                    Sku = new NatGatewaySku { Name = NatGatewaySkuName.Standard },
    //                    Tags = { { "aspire-resource-name", infra.AspireResource.Name } }
    //                };

    //                if (azureResource.IdleTimeoutInMinutes.HasValue)
    //                {
    //                    natGw.IdleTimeoutInMinutes = azureResource.IdleTimeoutInMinutes.Value;
    //                }

    //                // Add public IP addresses if configured
    //                if (azureResource.PublicIpAddresses.Count > 0)
    //                {
    //                    foreach (var publicIp in azureResource.PublicIpAddresses)
    //                    {
    //                        natGw.PublicIPAddresses.Add(new WritableSubResource
    //                        {
    //                            Id = publicIp.Id
    //                        });
    //                    }
    //                }

    //                return natGw;
    //            });

    //        // Output the NAT Gateway ID for references
    //        infra.Add(new ProvisioningOutput("id", typeof(string))
    //        {
    //            Value = natGateway.Id
    //        });

    //        // We need to output name to externalize role assignments.
    //        infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = natGateway.Name });
    //    }
    //}

    /// <summary>
    /// Associates a NAT Gateway with the subnet.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="natGateway">The NAT Gateway resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/>.</returns>
    public static IResourceBuilder<AzureSubnetResource> WithNatGateway(
        this IResourceBuilder<AzureSubnetResource> builder,
        IResourceBuilder<AzureNatGatewayResource> natGateway)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(natGateway);

        builder.Resource.NatGateway = natGateway.Resource;
        return builder;
    }

    /// <summary>
    /// Associates a Network Security Group with the subnet.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="networkSecurityGroup">The Network Security Group resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/>.</returns>
    public static IResourceBuilder<AzureSubnetResource> WithNetworkSecurityGroup(
        this IResourceBuilder<AzureSubnetResource> builder,
        IResourceBuilder<AzureNetworkSecurityGroupResource> networkSecurityGroup)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(networkSecurityGroup);

        builder.WithAnnotation(new AzureNetworkSecurityGroupReferenceAnnotation(networkSecurityGroup.Resource));
        return builder;
    }

    /// <summary>
    /// Adds a service endpoint to the subnet.
    /// </summary>
    /// <param name="builder">The subnet resource builder.</param>
    /// <param name="service">The service name (e.g., "Microsoft.Storage", "Microsoft.Sql", "Microsoft.KeyVault").</param>
    /// <param name="locations">The locations where the service endpoint is enabled. If not specified, defaults to all locations ("*").</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSubnetResource}"/>.</returns>
    public static IResourceBuilder<AzureSubnetResource> WithServiceEndpoint(
        this IResourceBuilder<AzureSubnetResource> builder,
        string service,
        params string[] locations)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(service);

        locations = locations.Length == 0 ? ["*"] : locations;

        builder.Resource.ServiceEndpoints.Add(new ServiceEndpointConfig(service, locations));
        return builder;
    }

    /// <summary>
    /// Associates a Public IP Address with the NAT Gateway.
    /// </summary>
    /// <param name="builder">The NAT Gateway resource builder.</param>
    /// <param name="publicIp">The Public IP Address resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureNatGatewayResource}"/>.</returns>
    public static IResourceBuilder<AzureNatGatewayResource> WithPublicIP(
        this IResourceBuilder<AzureNatGatewayResource> builder,
        IResourceBuilder<AzurePublicIpResource> publicIp)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(publicIp);

        builder.Resource.PublicIpAddresses.Add(publicIp.Resource);
        return builder;
    }

    /// <summary>
    /// Adds an Azure Network Security Group resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the Azure Network Security Group resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureNetworkSecurityGroupResource}"/>.</returns>
    public static IResourceBuilder<AzureNetworkSecurityGroupResource> AddAzureNetworkSecurityGroup(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        builder.AddAzureProvisioning();

        AzureNetworkSecurityGroupResource resource = new(name, ConfigureNetworkSecurityGroup);
        return builder.AddResource(resource);

        static void ConfigureNetworkSecurityGroup(AzureResourceInfrastructure infra)
        {
            var nsg = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infra,
                (identifier, name) =>
                {
                    var resource = NetworkSecurityGroup.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) =>
                {
                    var azureResource = (AzureNetworkSecurityGroupResource)infrastructure.AspireResource;

                    var nsg = new NetworkSecurityGroup(infrastructure.AspireResource.GetBicepIdentifier())
                    {
                        Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                    };

                    // Add security rules
                    foreach (var rule in azureResource._securityRules)
                    {
                        var securityRule = new SecurityRule(Infrastructure.NormalizeBicepIdentifier(rule.Name))
                        {
                            Name = rule.Name,
                            Direction = rule.Direction,
                            Priority = rule.Priority,
                            Access = rule.Access,
                            Protocol = ParseProtocol(rule.Protocol),
                            SourceAddressPrefix = rule.SourceAddressPrefix,
                            SourcePortRange = rule.SourcePortRange,
                            DestinationAddressPrefix = rule.DestinationAddressPrefix,
                            DestinationPortRange = rule.DestinationPortRange,
                        };

                        if (!string.IsNullOrEmpty(rule.Description))
                        {
                            securityRule.Description = rule.Description;
                        }

                        // Handle multiple source address prefixes
                        if (rule.SourceAddressPrefixes is { Count: > 0 })
                        {
                            securityRule.SourceAddressPrefixes = new BicepList<string>(
                                rule.SourceAddressPrefixes.Select(p => new BicepValue<string>(p)).ToList());
                        }

                        // Handle multiple source port ranges
                        if (rule.SourcePortRanges is { Count: > 0 })
                        {
                            securityRule.SourcePortRanges = new BicepList<string>(
                                rule.SourcePortRanges.Select(p => new BicepValue<string>(p)).ToList());
                        }

                        // Handle multiple destination address prefixes
                        if (rule.DestinationAddressPrefixes is { Count: > 0 })
                        {
                            securityRule.DestinationAddressPrefixes = new BicepList<string>(
                                rule.DestinationAddressPrefixes.Select(p => new BicepValue<string>(p)).ToList());
                        }

                        // Handle multiple destination port ranges
                        if (rule.DestinationPortRanges is { Count: > 0 })
                        {
                            securityRule.DestinationPortRanges = new BicepList<string>(
                                rule.DestinationPortRanges.Select(p => new BicepValue<string>(p)).ToList());
                        }

                        nsg.SecurityRules.Add(securityRule);
                    }

                    return nsg;
                });

            // Output the NSG ID for references
            infra.Add(new ProvisioningOutput("id", typeof(string))
            {
                Value = nsg.Id
            });

            // We need to output name so it can be referenced by others.
            infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = nsg.Name });
        }
    }

    /// <summary>
    /// Adds an inbound security rule to the Network Security Group.
    /// </summary>
    /// <param name="builder">The NSG resource builder.</param>
    /// <param name="name">The name of the security rule.</param>
    /// <param name="priority">The priority of the rule (100-4096).</param>
    /// <param name="sourceAddressPrefix">The source address prefix (IP address, CIDR, or tag like "Internet", "VirtualNetwork").</param>
    /// <param name="sourcePortRange">The source port or range (e.g., "80", "80-100", or "*").</param>
    /// <param name="destinationAddressPrefix">The destination address prefix.</param>
    /// <param name="destinationPortRange">The destination port or range.</param>
    /// <param name="protocol">The protocol (Tcp, Udp, Icmp, Esp, Ah, or "*").</param>
    /// <param name="access">Allow or Deny.</param>
    /// <param name="description">Optional description for the rule.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureNetworkSecurityGroupResource}"/>.</returns>
    public static IResourceBuilder<AzureNetworkSecurityGroupResource> WithInboundSecurityRule(
        this IResourceBuilder<AzureNetworkSecurityGroupResource> builder,
        string name,
        int priority,
        string sourceAddressPrefix = "*",
        string sourcePortRange = "*",
        string destinationAddressPrefix = "*",
        string destinationPortRange = "*",
        string protocol = "*",
        SecurityRuleAccess access = SecurityRuleAccess.Allow,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(priority, 100);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(priority, 4096);

        var rule = new SecurityRuleConfig
        {
            Name = name,
            Direction = SecurityRuleDirection.Inbound,
            Priority = priority,
            Access = access,
            Protocol = protocol,
            SourceAddressPrefix = sourceAddressPrefix,
            SourcePortRange = sourcePortRange,
            DestinationAddressPrefix = destinationAddressPrefix,
            DestinationPortRange = destinationPortRange,
            Description = description
        };

        builder.Resource._securityRules.Add(rule);
        return builder;
    }

    /// <summary>
    /// Adds an outbound security rule to the Network Security Group.
    /// </summary>
    /// <param name="builder">The NSG resource builder.</param>
    /// <param name="name">The name of the security rule.</param>
    /// <param name="priority">The priority of the rule (100-4096).</param>
    /// <param name="sourceAddressPrefix">The source address prefix (IP address, CIDR, or tag like "Internet", "VirtualNetwork").</param>
    /// <param name="sourcePortRange">The source port or range (e.g., "80", "80-100", or "*").</param>
    /// <param name="destinationAddressPrefix">The destination address prefix.</param>
    /// <param name="destinationPortRange">The destination port or range.</param>
    /// <param name="protocol">The protocol (Tcp, Udp, Icmp, Esp, Ah, or "*").</param>
    /// <param name="access">Allow or Deny.</param>
    /// <param name="description">Optional description for the rule.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureNetworkSecurityGroupResource}"/>.</returns>
    public static IResourceBuilder<AzureNetworkSecurityGroupResource> WithOutboundSecurityRule(
        this IResourceBuilder<AzureNetworkSecurityGroupResource> builder,
        string name,
        int priority,
        string sourceAddressPrefix = "*",
        string sourcePortRange = "*",
        string destinationAddressPrefix = "*",
        string destinationPortRange = "*",
        string protocol = "*",
        SecurityRuleAccess access = SecurityRuleAccess.Allow,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(priority, 100);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(priority, 4096);

        var rule = new SecurityRuleConfig
        {
            Name = name,
            Direction = SecurityRuleDirection.Outbound,
            Priority = priority,
            Access = access,
            Protocol = protocol,
            SourceAddressPrefix = sourceAddressPrefix,
            SourcePortRange = sourcePortRange,
            DestinationAddressPrefix = destinationAddressPrefix,
            DestinationPortRange = destinationPortRange,
            Description = description
        };

        builder.Resource._securityRules.Add(rule);
        return builder;
    }

    /// <summary>
    /// Adds an inbound security rule with multiple source address prefixes to the Network Security Group.
    /// </summary>
    /// <param name="builder">The NSG resource builder.</param>
    /// <param name="name">The name of the security rule.</param>
    /// <param name="priority">The priority of the rule (100-4096).</param>
    /// <param name="sourceAddressPrefixes">The list of source address prefixes.</param>
    /// <param name="sourcePortRange">The source port or range (e.g., "80", "80-100", or "*").</param>
    /// <param name="destinationAddressPrefix">The destination address prefix.</param>
    /// <param name="destinationPortRange">The destination port or range.</param>
    /// <param name="protocol">The protocol (Tcp, Udp, Icmp, Esp, Ah, or "*").</param>
    /// <param name="access">Allow or Deny.</param>
    /// <param name="description">Optional description for the rule.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureNetworkSecurityGroupResource}"/>.</returns>
    public static IResourceBuilder<AzureNetworkSecurityGroupResource> WithInboundSecurityRule(
        this IResourceBuilder<AzureNetworkSecurityGroupResource> builder,
        string name,
        int priority,
        List<string> sourceAddressPrefixes,
        string sourcePortRange = "*",
        string destinationAddressPrefix = "*",
        string destinationPortRange = "*",
        string protocol = "*",
        SecurityRuleAccess access = SecurityRuleAccess.Allow,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(sourceAddressPrefixes);
        ArgumentOutOfRangeException.ThrowIfLessThan(priority, 100);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(priority, 4096);

        var rule = new SecurityRuleConfig
        {
            Name = name,
            Direction = SecurityRuleDirection.Inbound,
            Priority = priority,
            Access = access,
            Protocol = protocol,
            SourceAddressPrefixes = sourceAddressPrefixes,
            SourcePortRange = sourcePortRange,
            DestinationAddressPrefix = destinationAddressPrefix,
            DestinationPortRange = destinationPortRange,
            Description = description
        };

        builder.Resource._securityRules.Add(rule);
        return builder;
    }

    /// <summary>
    /// Adds an outbound security rule with multiple destination address prefixes to the Network Security Group.
    /// </summary>
    /// <param name="builder">The NSG resource builder.</param>
    /// <param name="name">The name of the security rule.</param>
    /// <param name="priority">The priority of the rule (100-4096).</param>
    /// <param name="sourceAddressPrefix">The source address prefix.</param>
    /// <param name="sourcePortRange">The source port or range.</param>
    /// <param name="destinationAddressPrefixes">The list of destination address prefixes.</param>
    /// <param name="destinationPortRange">The destination port or range.</param>
    /// <param name="protocol">The protocol (Tcp, Udp, Icmp, Esp, Ah, or "*").</param>
    /// <param name="access">Allow or Deny.</param>
    /// <param name="description">Optional description for the rule.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureNetworkSecurityGroupResource}"/>.</returns>
    public static IResourceBuilder<AzureNetworkSecurityGroupResource> WithOutboundSecurityRule(
        this IResourceBuilder<AzureNetworkSecurityGroupResource> builder,
        string name,
        int priority,
        string sourceAddressPrefix,
        string sourcePortRange,
        List<string> destinationAddressPrefixes,
        string destinationPortRange = "*",
        string protocol = "*",
        SecurityRuleAccess access = SecurityRuleAccess.Allow,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(destinationAddressPrefixes);
        ArgumentOutOfRangeException.ThrowIfLessThan(priority, 100);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(priority, 4096);

        var rule = new SecurityRuleConfig
        {
            Name = name,
            Direction = SecurityRuleDirection.Outbound,
            Priority = priority,
            Access = access,
            Protocol = protocol,
            SourceAddressPrefix = sourceAddressPrefix,
            SourcePortRange = sourcePortRange,
            DestinationAddressPrefixes = destinationAddressPrefixes,
            DestinationPortRange = destinationPortRange,
            Description = description
        };

        builder.Resource._securityRules.Add(rule);
        return builder;
    }

    /// <summary>
    /// Parses protocol string to SecurityRuleProtocol enum.
    /// </summary>
    private static SecurityRuleProtocol ParseProtocol(string protocol)
    {
        return protocol.ToUpperInvariant() switch
        {
            "*" => SecurityRuleProtocol.Asterisk,
            "TCP" => SecurityRuleProtocol.Tcp,
            "UDP" => SecurityRuleProtocol.Udp,
            "ICMP" => SecurityRuleProtocol.Icmp,
            "ESP" => SecurityRuleProtocol.Esp,
            "AH" => SecurityRuleProtocol.Ah,
            _ => SecurityRuleProtocol.Asterisk
        };
    }
}
