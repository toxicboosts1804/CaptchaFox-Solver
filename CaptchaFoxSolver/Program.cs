using CaptchaFoxSolver.Entities;
using System.Security.Cryptography;
using System.Text.Json;

namespace CaptchaFoxSolver;

public class Program
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public static SolverConfig Config;
    public static SemaphoreSlim Limiter;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

   public static async Task Main(string[] args)
    {
        Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_ENABLEACTIVITYPROPAGATION", "false");
        if (!File.Exists("Config.json"))
        {
            Console.WriteLine("Config.json not found, creating with default values...");
            Config = new SolverConfig
            {
                AuthorizationToken = Environment.GetEnvironmentVariable("AUTH_TOKEN") ?? string.Join("", RandomNumberGenerator.GetHexString(64, true).Select(x => Random.Shared.NextSingle() > .5 ? char.ToUpper(x) : x)),
                Host = "http://0.0.0.0:5462",
                ChallengeWidth = 250,
                SampleN = 50,
                CursorStepsPerSecond = 44,
                CursorYFrequency = 1.5f,
                CursorYAmplitude = 5f,
                RequireAuthorization = true,
                RequireProxies = false,
                MaxConcurrency = 50
            };
            File.WriteAllText("Config.json", JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true}));
            Console.WriteLine("Config.json created successfully. Starting server...");
            Console.WriteLine($"🔑 Authorization Token: {Config.AuthorizationToken}");
        }
        else Config = JsonSerializer.Deserialize<SolverConfig>(File.ReadAllText("Config.json"))!;
    
        Console.WriteLine($"🔑 Using Authorization Token: {Config.AuthorizationToken}");
        
        Limiter = new SemaphoreSlim(Config.MaxConcurrency);
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddScoped<FoxSolver>();
        builder.Services.AddControllers(opts =>
        {
            opts.Filters.Add<ExceptionReadabilityFilter>();
        }).AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            opts.JsonSerializerOptions.WriteIndented = true;
        });
        var app = builder.Build();
        
        // Commented out for Render - HTTPS handled by load balancer
        // if(app.Environment.IsProduction())
        //     app.UseHttpsRedirection();
        
        app.MapControllers();
        app.Logger.LogWarning("Repository available at https://github.com/1xKvSUbAg1xJx9KutZW1lzrdGImI3CaW/CaptchaFox-Solver");
        app.Logger.LogWarning("Remember to star the repository or to send a donation :)");
        app.Logger.LogWarning("Issues about anything other than the solver will be closed" + Environment.NewLine + Environment.NewLine + Environment.NewLine);
        
        await app.RunAsync(Config.Host);
    }
}
