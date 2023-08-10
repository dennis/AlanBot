// https://discord.com/api/oauth2/authorize?client_id=1070371504371531796&permissions=2147485760&scope=bot

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using Serilog;

using System.Reflection;

namespace AlanBot;

public class Application
{
    private readonly string _token;
    private readonly ILogger _logger;
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;

    public Application(
        ILogger logger,
        IConfiguration config,
        DiscordSocketClient discordSocketClient,
        InteractionService interactionService,
        IServiceProvider serviceProvider
    )
    {
        _token = config.GetSection("DiscordToken").Get<string>() ?? "";
        _logger = logger;
        _discordSocketClient = discordSocketClient;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
    }

    internal async Task RunAsync()
    {
        _discordSocketClient.Log += LogAsync;
        _discordSocketClient.Ready += ReadyAsync;
        _interactionService.Log += LogAsync;

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        _discordSocketClient.InteractionCreated += HandleInteractionAsync;

        await _discordSocketClient.LoginAsync(TokenType.Bot, _token);
        await _discordSocketClient.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private Task LogAsync(LogMessage log)
    {
        switch (log.Severity)
        {
            case LogSeverity.Critical:
                _logger.Fatal(log.Exception, "DiscordNet {DiscordNetSource} {DiscordNetMessage}", log.Source, log.Message);
                break;
            case LogSeverity.Error:
                _logger.Error(log.Exception, "DiscordNet {DiscordNetSource} {DiscordNetMessage}", log.Source, log.Message);
                break;
            case LogSeverity.Warning:
                _logger.Warning(log.Exception, "DiscordNet {DiscordNetSource} {DiscordNetMessage}", log.Source, log.Message);
                break;
            case LogSeverity.Info:
                _logger.Information(log.Exception, "DiscordNet {DiscordNetSource} {DiscordNetMessage}", log.Source, log.Message);
                break;
            case LogSeverity.Verbose:
                _logger.Verbose(log.Exception, "DiscordNet {DiscordNetSource} {DiscordNetMessage}", log.Source, log.Message);
                break;
            case LogSeverity.Debug:
                _logger.Debug(log.Exception, "DiscordNet {DiscordNetSource} {DiscordNetMessage}", log.Source, log.Message);
                break;
        }

        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        _logger.Information("Connected as {BotUsername}", _discordSocketClient.CurrentUser.Username);
        await _interactionService.RegisterCommandsGloballyAsync(true);
    }

    private async Task HandleInteractionAsync(SocketInteraction arg)
    {
        _logger.Information("Handle Interaction");
        try
        {
            var ctx = new SocketInteractionContext(_discordSocketClient, arg);
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.Error("HandleInteractionAsync", ex);

            if (arg.Type == InteractionType.ApplicationCommand)
            {
                await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}