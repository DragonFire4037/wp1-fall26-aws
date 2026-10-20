using Amazon.S3;
using Wp1Fall26Aws.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Only add S3 service if not already registered (allows test override)
if (!builder.Services.Any(x => x.ServiceType == typeof(IStorageService)))
{
    builder.Services.AddSingleton<IStorageService, S3StorageService>();
}

builder.Services.AddDefaultAWSOptions(new Amazon.Extensions.NETCore.Setup.AWSOptions{Region = Amazon.RegionEndpoint.USEast1});
builder.Services.AddAWSService<IAmazonS3>();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// Only run the app if this is not a test
if (!app.Environment.IsEnvironment("Test"))
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    await app.RunAsync($"http://0.0.0.0:{port}");
}

// Make Program accessible for testing
public partial class Program { }

