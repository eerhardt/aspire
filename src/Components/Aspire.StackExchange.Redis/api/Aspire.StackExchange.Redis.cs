//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
namespace Aspire.StackExchange.Redis
{
    public sealed partial class StackExchangeRedisSettings
    {
        public string? ConnectionString { get { throw null; } set { } }

        public bool DisableHealthChecks { get { throw null; } set { } }

        public bool DisableTracing { get { throw null; } set { } }
    }
}

namespace Microsoft.Extensions.Hosting
{
    public static partial class AspireRedisExtensions
    {
        public static void AddKeyedRedisClient(this IHostApplicationBuilder builder, string name, System.Action<Aspire.StackExchange.Redis.StackExchangeRedisSettings>? configureSettings = null, System.Action<StackExchange.Redis.ConfigurationOptions>? configureOptions = null) { }

        public static void AddRedisClient(this IHostApplicationBuilder builder, string connectionName, System.Action<Aspire.StackExchange.Redis.StackExchangeRedisSettings>? configureSettings = null, System.Action<StackExchange.Redis.ConfigurationOptions>? configureOptions = null) { }
    }
}