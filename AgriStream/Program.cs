using AgriStream.Data;
using AgriStream.Models;
using AgriStream.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;


var builder = WebApplication.CreateBuilder(args);

// Azure SQL Database
builder.Services.AddDbContext<AgriStreamDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AgriStreamDbContext>()
    .AddDefaultTokenProviders();

// Azure Storage connection
var storageConnection =
    builder.Configuration["StorageConnection"]
    ?? throw new InvalidOperationException(
        "Storage connection string is missing.");

// Azure Blob Storage
builder.Services.AddSingleton(
    new BlobServiceClient(storageConnection));

// Azure Table Storage
builder.Services.AddSingleton(
    new TableServiceClient(storageConnection));

// Azure Queue Storage
builder.Services.AddSingleton(
    new QueueServiceClient(storageConnection));

// Azure Functions HTTP client
builder.Services.AddHttpClient("AgriStreamFunctions", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Functions:BaseUrl"]
        ?? "http://localhost:7071/api/");
});

builder.Services.AddScoped<FunctionsApiService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed the default Identity roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    await IdentitySeeder.SeedRolesAsync(roleManager);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();