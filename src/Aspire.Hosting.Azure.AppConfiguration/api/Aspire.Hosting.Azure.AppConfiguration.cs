//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Aspire.Hosting
{
    public static partial class AzureAppConfigurationExtensions
    {
        public static ApplicationModel.IResourceBuilder<Azure.AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, string name) { throw null; }
    }
}

namespace Aspire.Hosting.Azure
{
    public partial class AzureAppConfigurationResource : AzureProvisioningResource, ApplicationModel.IResourceWithConnectionString, ApplicationModel.IResource, ApplicationModel.IManifestExpressionProvider, ApplicationModel.IValueProvider, ApplicationModel.IValueWithReferences
    {
        public AzureAppConfigurationResource(string name, System.Action<AzureResourceInfrastructure> configureInfrastructure) : base(default!, default!) { }

        public ApplicationModel.ReferenceExpression ConnectionStringExpression { get { throw null; } }

        public BicepOutputReference Endpoint { get { throw null; } }
    }
}