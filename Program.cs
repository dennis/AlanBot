// Read messages/View channels, Send Messages, Use Slash Commands
// https://discord.com/api/oauth2/authorize?client_id=1070371504371531796&permissions=2147486720&scope=bot

using AlanBot;

using Aydsko.iRacingData;

using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"appsettings.json", true, true)
    .AddJsonFile($"appsettings.{env}.json", true, true)
    .AddEnvironmentVariables()
    .Build();

var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var serviceProvider = new ServiceCollection()
    .AddSingleton<IConfiguration>(configuration)
    .AddSingleton<ILogger>(logger)
    .AddSingleton(new DiscordSocketConfig { GatewayIntents = Discord.GatewayIntents.AllUnprivileged, AlwaysDownloadUsers = true })
    //.AddSingleton<DiscordSocketClient>()
    .AddSingleton(a => new DiscordSocketClient(new DiscordSocketConfig()))
    .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
        //.AddSingleton<InteractionService>()
    .AddIRacingDataApi(options =>
    {
        options.Username = configuration.GetSection("IRacingCredentials").GetSection("Username").Get<string>() ?? Environment.GetEnvironmentVariable("IRacingCredentials__Username");
        options.Password = configuration.GetSection("IRacingCredentials").GetSection("Password").Get<string>() ?? Environment.GetEnvironmentVariable("IRacingCredentials__Password");
        options.UserAgentProductName = "DiscordBot AlanBot";
    })
    .AddSingleton<Application>()
    .BuildServiceProvider();

var application = serviceProvider.GetRequiredService<Application>();

await application.RunAsync();
