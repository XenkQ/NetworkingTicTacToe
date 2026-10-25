using Microsoft.Extensions.FileProviders;
using NetworkingTicTacToe.Server.Hubs;
using NetworkingTicTacToe.Server.Services;

const string CORS_POLICY_NAME = "LanCorsPolicy";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IGameSessionService, GameSessionService>();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORS_POLICY_NAME, policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
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

if (Directory.Exists(clientDistFullPath))
{
    PhysicalFileProvider distProvider = new(clientDistFullPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = distProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = distProvider });

    if (File.Exists(Path.Combine(clientDistFullPath, "index.html")))
    {
        app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = distProvider });
    }
}
else if (Directory.Exists(wwwrootPath))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();

    if (File.Exists(Path.Combine(wwwrootPath, "index.html")))
    {
        app.MapFallbackToFile("index.html");
    }
}

app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy", service = "NetworkingTicTacToe.Server" });
});

app.MapFallback(() =>
{
    return Results.Text("Networking Tic-Tac-Toe Server is running. Client assets not yet built.", "text/plain");
});

app.MapHub<GameHub>("/hubs/game");

app.Run();
