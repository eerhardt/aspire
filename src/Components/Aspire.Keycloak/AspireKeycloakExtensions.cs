// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Keycloak;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Provides extension methods for registering Keycloak-related services in an <see cref="IHostApplicationBuilder"/>.
/// </summary>
public static class AspireKeycloakExtensions
{
    private const string DefaultConfigSectionName = "Aspire:Keycloak";
    private const string KeycloakBackchannel = nameof(KeycloakBackchannel);

    /// <summary>
    /// Adds Keycloak JWT Bearer authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">The connection name to use to find a connection string.</param>
    /// <param name="authenticationScheme">The authentication scheme name. Default is "Bearer".</param>
    /// <param name="configureJwtBearerOptions">An optional action to configure the <see cref="JwtBearerOptions"/>.</param>
    /// <param name="configureSettings">An optional action to configure the <see cref="KeycloakSettings"/>.</param>
    public static void AddKeycloakJwtBearer(
        this IHostApplicationBuilder builder,
        string connectionName,
        string authenticationScheme = JwtBearerDefaults.AuthenticationScheme,
        Action<JwtBearerOptions>? configureJwtBearerOptions = null,
        Action<KeycloakSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAuthentication(authenticationScheme)
                .AddJwtBearer(authenticationScheme);

        builder.Services.AddHttpClient(KeycloakBackchannel);

        builder.Services
               .AddOptions<JwtBearerOptions>(authenticationScheme)
               .Configure<IConfiguration, IHttpClientFactory, IHostEnvironment>((options, configuration, httpClientFactory, hostEnvironment) =>
               {
                   options.Backchannel = httpClientFactory.CreateClient(KeycloakBackchannel);
                   options.Authority = GetAuthorityUri(connectionName, configuration, configureSettings);

                   configureJwtBearerOptions?.Invoke(options);
               });
    }

    /// <summary>
    /// Adds Keycloak OpenID Connect authentication to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder" /> to read config from and add services to.</param>
    /// <param name="connectionName">The connection name to use to find a connection string.</param>
    /// <param name="openIdConnectScheme">The OpenID Connect authentication scheme name. Default is "OpenIdConnect".</param>
    /// <param name="cookieScheme">The cookie authentication scheme name. Default is "Cookie".</param>
    /// <param name="configureOpenIdConnectOptions">An optional action to configure the <see cref="OpenIdConnectOptions"/>.</param>
    /// <param name="configureSettings">An optional action to configure the <see cref="KeycloakSettings"/>.</param>
    public static void AddKeycloakOpenIdConnect(
        this IHostApplicationBuilder builder,
        string connectionName,
        string openIdConnectScheme = OpenIdConnectDefaults.AuthenticationScheme,
        string cookieScheme = CookieAuthenticationDefaults.AuthenticationScheme,
        Action<OpenIdConnectOptions>? configureOpenIdConnectOptions = null,
        Action<KeycloakSettings>? configureSettings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAuthentication(openIdConnectScheme)
                        .AddCookie(cookieScheme)
                        .AddOpenIdConnect(openIdConnectScheme, options => { });

        builder.Services.AddHttpClient(KeycloakBackchannel);

        builder.Services
               .AddOptions<OpenIdConnectOptions>(openIdConnectScheme)
               .Configure<IConfiguration, IHttpClientFactory, IHostEnvironment>((options, configuration, httpClientFactory, hostEnvironment) =>
               {
                   options.Backchannel = httpClientFactory.CreateClient(KeycloakBackchannel);
                   options.Authority = GetAuthorityUri(connectionName, configuration, configureSettings);
                   options.SignInScheme = cookieScheme;
                   options.SignOutScheme = openIdConnectScheme;

                   configureOpenIdConnectOptions?.Invoke(options);
               });
    }

    private static string GetAuthorityUri(
        string connectionName,
        IConfiguration configuration,
        Action<KeycloakSettings>? configureSettings = null)
    {
        var settings = new KeycloakSettings();
        configuration.GetSection(DefaultConfigSectionName).Bind(settings);

        configureSettings?.Invoke(settings);

        return $"https+http://{connectionName}/realms/{settings.Realm}";
    }
}