using Amazon.S3;
using Wp1Fall26Aws.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddSingleton<IStorageService, S3StorageService>();

builder.Services.AddDefaultAWSOptions(new Amazon.Extensions.NETCore.Setup.AWSOptions{Region = Amazon.RegionEndpoint.USEast1});
builder.Services.AddAWSService<IAmazonS3>();

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// Configure Kestrel to listen on 0.0.0.0 and the PORT environment variable (default 5000 for EB)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
