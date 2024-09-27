// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Identity;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("db1", configureDataSourceBuilder: dataSourceBuilder =>
{
    if (string.IsNullOrEmpty(dataSourceBuilder.ConnectionStringBuilder.Password))
    {
        dataSourceBuilder.UsePeriodicPasswordProvider(async (_, ct) =>
        {
            var credentials = new DefaultAzureCredential();
            var token = await credentials.GetTokenAsync(new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]), ct);
            return token.Token;
        }, TimeSpan.FromHours(24), TimeSpan.FromSeconds(10));
    }
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", async (NpgsqlDataSource db1) =>
{
    var result = await db1.CreateCommand("SELECT 1").ExecuteScalarAsync();
    return result?.ToString();
});

app.Run();
