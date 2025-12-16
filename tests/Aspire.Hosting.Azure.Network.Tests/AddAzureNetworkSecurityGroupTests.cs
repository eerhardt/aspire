// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning.Network;

namespace Aspire.Hosting.Azure.Network.Tests;

public class AddAzureNetworkSecurityGroupTests
{
    [Fact]
    public void AddAzureNetworkSecurityGroup_ShouldCreateNetworkSecurityGroupResourceWithCorrectName()
    {
        // Arrange
        const string name = "nsg";
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup(name);

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.NotNull(resourceBuilder.Resource);
        Assert.Equal(name, resourceBuilder.Resource.Name);
        Assert.IsType<AzureNetworkSecurityGroupResource>(resourceBuilder.Resource);
    }

    [Fact]
    public void WithInboundSecurityRule_ShouldAddInboundRuleWithDefaultValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        const string ruleName = "AllowHTTP";

        // Act
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg")
            .WithInboundSecurityRule(ruleName, priority: 100);

        // Assert
        var rule = resourceBuilder.Resource.SecurityRules.Single();
        Assert.Equal(ruleName, rule.Name);
        Assert.Equal(SecurityRuleDirection.Inbound, rule.Direction);
        Assert.Equal(100, rule.Priority);
        Assert.Equal(SecurityRuleAccess.Allow, rule.Access);
        Assert.Equal("*", rule.Protocol);
        Assert.Equal("*", rule.SourceAddressPrefix);
        Assert.Equal("*", rule.SourcePortRange);
        Assert.Equal("*", rule.DestinationAddressPrefix);
        Assert.Equal("*", rule.DestinationPortRange);
    }

    [Fact]
    public void WithInboundSecurityRule_ShouldAddInboundRuleWithCustomValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg")
            .WithInboundSecurityRule(
                name: "AllowHTTP",
                priority: 100,
                sourceAddressPrefix: "Internet",
                sourcePortRange: "*",
                destinationAddressPrefix: "VirtualNetwork",
                destinationPortRange: "80",
                protocol: "TCP",
                access: SecurityRuleAccess.Allow,
                description: "Allow HTTP traffic from Internet");

        // Assert
        var rule = resourceBuilder.Resource.SecurityRules.Single();
        Assert.Equal("AllowHTTP", rule.Name);
        Assert.Equal(SecurityRuleDirection.Inbound, rule.Direction);
        Assert.Equal(100, rule.Priority);
        Assert.Equal(SecurityRuleAccess.Allow, rule.Access);
        Assert.Equal("TCP", rule.Protocol);
        Assert.Equal("Internet", rule.SourceAddressPrefix);
        Assert.Equal("*", rule.SourcePortRange);
        Assert.Equal("VirtualNetwork", rule.DestinationAddressPrefix);
        Assert.Equal("80", rule.DestinationPortRange);
        Assert.Equal("Allow HTTP traffic from Internet", rule.Description);
    }

    [Fact]
    public void WithOutboundSecurityRule_ShouldAddOutboundRuleWithDefaultValues()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        const string ruleName = "AllowInternet";

        // Act
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg")
            .WithOutboundSecurityRule(ruleName, priority: 200);

        // Assert
        var rule = resourceBuilder.Resource.SecurityRules.Single();
        Assert.Equal(ruleName, rule.Name);
        Assert.Equal(SecurityRuleDirection.Outbound, rule.Direction);
        Assert.Equal(200, rule.Priority);
        Assert.Equal(SecurityRuleAccess.Allow, rule.Access);
    }

    [Fact]
    public void WithInboundSecurityRule_WithMultipleSourcePrefixes_ShouldAddRule()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var sourcePrefixes = new List<string> { "10.0.1.0/24", "10.0.2.0/24" };

        // Act
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg")
            .WithInboundSecurityRule(
                name: "AllowSpecificSubnets",
                priority: 150,
                sourceAddressPrefixes: sourcePrefixes,
                destinationPortRange: "443",
                protocol: "TCP");

        // Assert
        var rule = resourceBuilder.Resource.SecurityRules.Single();
        Assert.Equal("AllowSpecificSubnets", rule.Name);
        Assert.NotNull(rule.SourceAddressPrefixes);
        Assert.Equal(2, rule.SourceAddressPrefixes.Count);
        Assert.Contains("10.0.1.0/24", rule.SourceAddressPrefixes);
        Assert.Contains("10.0.2.0/24", rule.SourceAddressPrefixes);
    }

    [Fact]
    public void WithOutboundSecurityRule_WithMultipleDestinationPrefixes_ShouldAddRule()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var destinationPrefixes = new List<string> { "20.0.1.0/24", "20.0.2.0/24" };

        // Act
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg")
            .WithOutboundSecurityRule(
                name: "AllowSpecificDestinations",
                priority: 250,
                sourceAddressPrefix: "VirtualNetwork",
                sourcePortRange: "*",
                destinationAddressPrefixes: destinationPrefixes,
                protocol: "UDP");

        // Assert
        var rule = resourceBuilder.Resource.SecurityRules.Single();
        Assert.Equal("AllowSpecificDestinations", rule.Name);
        Assert.Equal(SecurityRuleDirection.Outbound, rule.Direction);
        Assert.NotNull(rule.DestinationAddressPrefixes);
        Assert.Equal(2, rule.DestinationAddressPrefixes.Count);
    }

    [Fact]
    public void WithInboundSecurityRule_MultipleRules_ShouldAddAllRules()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg")
            .WithInboundSecurityRule("AllowHTTP", priority: 100, destinationPortRange: "80")
            .WithInboundSecurityRule("AllowHTTPS", priority: 110, destinationPortRange: "443")
            .WithInboundSecurityRule("DenyAll", priority: 4096, access: SecurityRuleAccess.Deny);

        // Assert
        Assert.Equal(3, resourceBuilder.Resource.SecurityRules.Count);
        var httpRule = resourceBuilder.Resource.SecurityRules.First(r => r.Name == "AllowHTTP");
        Assert.Equal(100, httpRule.Priority);
        Assert.Equal("80", httpRule.DestinationPortRange);

        var httpsRule = resourceBuilder.Resource.SecurityRules.First(r => r.Name == "AllowHTTPS");
        Assert.Equal(110, httpsRule.Priority);
        Assert.Equal("443", httpsRule.DestinationPortRange);

        var denyRule = resourceBuilder.Resource.SecurityRules.First(r => r.Name == "DenyAll");
        Assert.Equal(4096, denyRule.Priority);
        Assert.Equal(SecurityRuleAccess.Deny, denyRule.Access);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(4097)]
    public void WithInboundSecurityRule_InvalidPriority_ShouldThrowException(int invalidPriority)
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            resourceBuilder.WithInboundSecurityRule("TestRule", invalidPriority));
    }

    [Fact]
    public void WithInboundSecurityRule_NullName_ShouldThrowException()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            resourceBuilder.WithInboundSecurityRule(null!, 100));
    }

    [Fact]
    public void WithInboundSecurityRule_EmptyName_ShouldThrowException()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("nsg");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            resourceBuilder.WithInboundSecurityRule(string.Empty, 100));
    }

    [Fact]
    public void AddSubnet_WithNetworkSecurityGroup_ShouldAssociateNsgWithSubnet()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var nsg = builder.AddAzureNetworkSecurityGroup("test-nsg")
            .WithInboundSecurityRule("AllowHTTP", 100, destinationPortRange: "80");

        var vnet = builder.AddAzureVirtualNetwork("test-vnet");
        var subnet = vnet.AddSubnet("test-subnet", "10.0.1.0/24")
            .WithNetworkSecurityGroup(nsg);

        // Assert
        var nsgAnnotation = subnet.Resource.Annotations.OfType<AzureNetworkSecurityGroupReferenceAnnotation>().SingleOrDefault();
        Assert.NotNull(nsgAnnotation);
        Assert.Equal("test-nsg", nsgAnnotation.NsgResource.Name);
    }

    [Fact]
    public void AddAzureNetworkSecurityGroup_HasCorrectOutputReferences()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureNetworkSecurityGroup("test-nsg");

        // Assert
        Assert.NotNull(resourceBuilder.Resource.Id);
        Assert.Equal("id", resourceBuilder.Resource.Id.Name);
        Assert.NotNull(resourceBuilder.Resource.NameOutput);
        Assert.Equal("name", resourceBuilder.Resource.NameOutput.Name);
    }

    [Fact]
    public void WithInboundSecurityRule_NullBuilder_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IResourceBuilder<AzureNetworkSecurityGroupResource>)null!).WithInboundSecurityRule("test", 100));
    }

    [Fact]
    public void WithOutboundSecurityRule_NullBuilder_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IResourceBuilder<AzureNetworkSecurityGroupResource>)null!).WithOutboundSecurityRule("test", 100));
    }

    [Fact]
    public void WithNetworkSecurityGroup_NullBuilder_ShouldThrowException()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var nsg = builder.AddAzureNetworkSecurityGroup("nsg");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IResourceBuilder<AzureSubnetResource>)null!).WithNetworkSecurityGroup(nsg));
    }

    [Fact]
    public void WithNetworkSecurityGroup_NullNsg_ShouldThrowException()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var vnet = builder.AddAzureVirtualNetwork("vnet");
        var subnet = vnet.AddSubnet("subnet", "10.0.1.0/24");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            subnet.WithNetworkSecurityGroup(null!));
    }

    [Fact]
    public void WithServiceEndpoint_ShouldAddServiceEndpoint()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var vnet = builder.AddAzureVirtualNetwork("vnet");

        // Act
        var subnet = vnet.AddSubnet("subnet", "10.0.1.0/24")
            .WithServiceEndpoint("Microsoft.Storage");

        // Assert
        Assert.Single(subnet.Resource.ServiceEndpoints);
        var endpoint = subnet.Resource.ServiceEndpoints[0];
        Assert.Equal("Microsoft.Storage", endpoint.Service);
        Assert.Single(endpoint.Locations);
        Assert.Equal("*", endpoint.Locations[0]);
    }

    [Fact]
    public void WithServiceEndpoint_WithLocations_ShouldAddServiceEndpointWithLocations()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var vnet = builder.AddAzureVirtualNetwork("vnet");

        // Act
        var subnet = vnet.AddSubnet("subnet", "10.0.1.0/24")
            .WithServiceEndpoint("Microsoft.Sql", "eastus", "westus");

        // Assert
        Assert.Single(subnet.Resource.ServiceEndpoints);
        var endpoint = subnet.Resource.ServiceEndpoints[0];
        Assert.Equal("Microsoft.Sql", endpoint.Service);
        Assert.Equal(2, endpoint.Locations.Length);
        Assert.Contains("eastus", endpoint.Locations);
        Assert.Contains("westus", endpoint.Locations);
    }

    [Fact]
    public void WithServiceEndpoint_MultipleEndpoints_ShouldAddAll()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var vnet = builder.AddAzureVirtualNetwork("vnet");

        // Act
        var subnet = vnet.AddSubnet("subnet", "10.0.1.0/24")
            .WithServiceEndpoint("Microsoft.Storage")
            .WithServiceEndpoint("Microsoft.Sql", "eastus")
            .WithServiceEndpoint("Microsoft.KeyVault");

        // Assert
        Assert.Equal(3, subnet.Resource.ServiceEndpoints.Count);
        Assert.Contains(subnet.Resource.ServiceEndpoints, e => e.Service == "Microsoft.Storage");
        Assert.Contains(subnet.Resource.ServiceEndpoints, e => e.Service == "Microsoft.Sql");
        Assert.Contains(subnet.Resource.ServiceEndpoints, e => e.Service == "Microsoft.KeyVault");
    }

    [Fact]
    public void WithServiceEndpoint_NullBuilder_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IResourceBuilder<AzureSubnetResource>)null!).WithServiceEndpoint("Microsoft.Storage"));
    }

    [Fact]
    public void WithServiceEndpoint_NullService_ShouldThrowException()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var vnet = builder.AddAzureVirtualNetwork("vnet");
        var subnet = vnet.AddSubnet("subnet", "10.0.1.0/24");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            subnet.WithServiceEndpoint(null!));
    }

    [Fact]
    public void WithServiceEndpoint_EmptyService_ShouldThrowException()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var vnet = builder.AddAzureVirtualNetwork("vnet");
        var subnet = vnet.AddSubnet("subnet", "10.0.1.0/24");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            subnet.WithServiceEndpoint(string.Empty));
    }
}
