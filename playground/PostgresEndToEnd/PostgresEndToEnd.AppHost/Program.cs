// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var builder = DistributedApplication.CreateBuilder(args);

// Abstract resources.
var db1 = builder.AddAzurePostgresFlexibleServer("pg1")
    .RunAsContainer(c => c.WithLifetime(ContainerLifetime.Persistent).WithPgAdmin(adm => adm.WithLifetime(ContainerLifetime.Persistent)))
    .AddDatabase("db1");

builder.AddProject<Projects.PostgresEndToEnd_ApiService>("api")
       .WithExternalHttpEndpoints()
       .WithReference(db1);
#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

builder.Build().Run();
