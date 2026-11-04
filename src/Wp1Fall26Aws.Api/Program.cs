using Amazon.S3;
using Wp1Fall26Aws.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Add CORS for static client development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalClient", cors =>
    {
        cors.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Only add storage service if not already registered (allows test override)
if (!builder.Services.Any(x => x.ServiceType == typeof(IStorageService)))
{
    // For development without AWS credentials configured, use in-memory storage
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<IStorageService, FakeStorageService>();
    }
    else
    {
        // Production: use S3 with AWS credentials
        builder.Services.AddDefaultAWSOptions(new Amazon.Extensions.NETCore.Setup.AWSOptions { Region = Amazon.RegionEndpoint.USEast1 });
        builder.Services.AddAWSService<IAmazonS3>();
        builder.Services.AddSingleton<IStorageService, S3StorageService>();
    }
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Apply CORS before routing
app.UseCors("AllowLocalClient");

app.MapControllers();

// Only run the app if this is not a test
if (!app.Environment.IsEnvironment("Test"))
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    await app.RunAsync($"http://localhost:{port}");
}

// Make Program accessible for testing
public partial class Program { }

