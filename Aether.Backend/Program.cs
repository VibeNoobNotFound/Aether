using Aether.Backend.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
{
    var socketPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/Aether/aether.sock");
    // Ensure directory exists
    Directory.CreateDirectory(Path.GetDirectoryName(socketPath)!);

    if (File.Exists(socketPath)) File.Delete(socketPath);

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenUnixSocket(socketPath, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
        });
    });
    Console.WriteLine($"Configured to listen on UDS: {socketPath}");
}

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
{
    app.Urls.Add("http://localhost:50051");
    Console.WriteLine("Listening on http://localhost:50051");
}

// Configure the HTTP request pipeline.
app.MapGrpcService<AetherGrpcService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
