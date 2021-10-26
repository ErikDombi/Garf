using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Garth.IO;
using Garth.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Garth
{
    public class Garth
    {
        private DiscordSocketClient? _client;

        private Configuration? _configuration;

        public Garth()
            => StartBot().GetAwaiter().GetResult();

        private async Task StartBot()
        {
            using (var services = ConfigureServices())
            {
                _client = services.GetRequiredService<DiscordSocketClient>();
                _configuration = services.GetRequiredService<Configuration>();

                _client.Log += Log;

                var token = _configuration.Token;

                if (token == null)
                    throw new Exception("Bot token not set in config.json");

                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

                await Task.Delay(-1);
            }
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton(Configuration.LoadConfiguration())
                .AddSingleton<TagService>()
                .BuildServiceProvider();
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
