using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MonikAIBot.Modules;
using MonikAIBot.Services;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using CoreRCON;
using CoreRCON.Parsers.Standard;
using System.Net;

//henlo world
//messaging best admin, i am number 2 best

namespace MonikAIBot
{
    public class Program
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceCollection _map = new ServiceCollection();
        private readonly CommandService _commands = new CommandService(new CommandServiceConfig
        {
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Verbose
        });
        private Configuration _config;
        private MonikAIBotLogger _logger = new MonikAIBotLogger();
        private Random _random;

        private BirthdayService birthdayService = new BirthdayService();
        private BotStatusService statusService = new BotStatusService();

        private RCON _rcon = null;

        private Program()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 1000
            });

            _client.Log += Log;
        }

        public static void Main(string[] args)
            => new Program().AsyncMain().GetAwaiter().GetResult();

        public async Task AsyncMain()
        {
            _config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(@"data/config.json"));
            _random = new Random();

            if (_config.RconIP != "")
            {
                _rcon = new RCON(IPAddress.Parse(_config.RconIP), _config.RconPort, _config.RCONPassword);
            }                             

            //Set ondisconnect
            _rcon.OnDisconnected += OnDisconnected;

            //Command Setup
            await InitCommands();

            var provider = _map.BuildServiceProvider();

            _commands.Log += Log;

            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            provider.GetRequiredService<CommandHandler>();
            provider.GetRequiredService<ImageRateLimitHandler>();
            provider.GetRequiredService<DeletedMessageHandler>();

            //Stops crashing due to these services below
            await Task.Delay(2000);

            //Start birthdays
            int hours = 9;
            var dateNow = DateTime.Now;
            var date = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, hours, 0, 0);

            birthdayService.StartBirthdays(date, _client, _config, _random);

            statusService.StartBotStatuses(_client);

            await Task.Delay(-1);
        }

        private void OnDisconnected()
        {
            _logger.Log("Automatically Disconnected", "Rcon");
        }

        private async Task InitCommands()
        {
            //Repeat for all service classes
            _map.AddSingleton(_client);
            _map.AddSingleton(_logger);
            _map.AddSingleton(_random);
            _map.AddSingleton(_rcon);
            _map.AddSingleton(new Cooldowns());

            //For each module do the following
            await _commands.AddModuleAsync<Administration>();
            await _commands.AddModuleAsync<Interactions>();
            await _commands.AddModuleAsync<NSFW>();

            _map.AddSingleton(_commands);
            _map.AddSingleton<CommandHandler>();
            _map.AddSingleton<ImageRateLimitHandler>();
            _map.AddSingleton<DeletedMessageHandler>();
            _map.AddSingleton(_config);
        }

        private Task Log(LogMessage msg)
        {
            var cc = Console.ForegroundColor;
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
            }

            Console.WriteLine($"{DateTime.Now,-19} [{msg.Severity,8}] {msg.Source}: {msg.Message} {msg.Exception}");
            Console.ForegroundColor = cc;

            return Task.CompletedTask;
        }
    }
}
