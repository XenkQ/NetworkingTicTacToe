using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.FileProviders;
using NetworkingTicTacToe.Server.Hubs;
using NetworkingTicTacToe.Server.Services;

const string CORS_POLICY_NAME = "LanCorsPolicy";
const string DEFAULT_LAN_URLS = "http://0.0.0.0:5079";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(builder.Configuration["Urls"]) &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls(DEFAULT_LAN_URLS);
}

builder.Services.AddSingleton<IGameSessionService, GameSessionService>();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORS_POLICY_NAME, policy =>
    {
        policy.SetIsOriginAllowed(IsAllowedLanOrLocalDevOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

WebApplication app = builder.Build();

app.UseCors(CORS_POLICY_NAME);

string clientDistPath = Path.Combine(builder.Environment.ContentRootPath, "..", "client", "dist");
string clientDistFullPath = Path.GetFullPath(clientDistPath);
string wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

bool hasClientFallbackFile = false;

if (Directory.Exists(clientDistFullPath))
{
    PhysicalFileProvider distProvider = new(clientDistFullPath);
    app.Lifetime.ApplicationStopping.Register(distProvider.Dispose);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = distProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = distProvider });

    if (File.Exists(Path.Combine(clientDistFullPath, "index.html")))
    {
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = distProvider });
        hasClientFallbackFile = true;
    }
}
else if (Directory.Exists(wwwrootPath))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    if (File.Exists(Path.Combine(wwwrootPath, "index.html")))
    {
        app.MapFallbackToFile("index.html");
        hasClientFallbackFile = true;
    }
}

app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy", service = "NetworkingTicTacToe.Server" });
});

if (!hasClientFallbackFile)
{
    app.MapFallback(() =>
    {
        return Results.Text("Networking Tic-Tac-Toe Server is running. Client assets not yet built.", "text/plain");
    });
}

app.MapHub<GameHub>("/hubs/game");

app.Run();

static bool IsAllowedLanOrLocalDevOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri))
    {
        return false;
    }

    if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
        uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (IPAddress.TryParse(uri.Host, out IPAddress? ipAddress))
    {
        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            bool isPrivateLanIp =
                bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168) ||
                (bytes[0] == 169 && bytes[1] == 254);

            return isPrivateLanIp;
        }
    }

    return false;
}
