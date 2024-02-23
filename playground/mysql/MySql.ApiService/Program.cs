// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.AddMySqlDbContext<MyDbContext>("Catalog");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MyDbContext>().Database.EnsureCreated();
}

app.MapGet("/catalog", async (MyDbContext db) =>
{
    return await db.Items.ToListAsync();
});

app.Run();

public class MyDbContext(DbContextOptions<MyDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CatalogItem>().HasKey(e => e.Id);
    }

    public DbSet<CatalogItem> Items { get; set; }
}

public record CatalogItem(int Id, string Name, string Description, decimal Price);
