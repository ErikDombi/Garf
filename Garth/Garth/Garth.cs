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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Garth.Modules;
using System.IO;

namespace Garth
{
    public class Garth
    {
        private DiscordSocketClient? _client;

        private Configuration? _configuration;

        private ReplyTrackerService? _replyTracker;

        private TagService? _tagService;

        private PaginationReplyTracker<TagService> _paginationService;

        public Garth()
            => StartBot().GetAwaiter().GetResult();

        private async Task StartBot()
        {
            using (var services = ConfigureServices())
            {
                _client = services.GetRequiredService<DiscordSocketClient>();
                _configuration = services.GetRequiredService<Configuration>();
                _replyTracker = services.GetRequiredService<ReplyTrackerService>();
                _tagService = services.GetRequiredService<TagService>();
                _paginationService = services.GetRequiredService<PaginationReplyTracker<TagService>>();

                _client.MessageReceived += InlineTagReply;

                _client.MessageDeleted += async (cacheable, channel) =>
                {
                    _replyTracker.DeleteAll(cacheable.Id);
                };
                _client.MessageUpdated += async (Cacheable<IMessage, ulong> arg1, SocketMessage arg2,
                    ISocketMessageChannel arg3) =>
                {
                    InlineTagReply(arg2);
                };
                _client.ReactionAdded += async (msg, channel, arg3) =>
                {
                    if (arg3.User.Value.IsBot)
                        return;

                    var paginationReply = _paginationService.FirstOrDefault(t => t.ReplyMessage.Id == msg.Id);
                    bool isPageRight = arg3.Emote.Name == "▶️";
                    bool isPageLeft = arg3.Emote.Name == "◀️";
                    var message = await channel.GetMessageAsync(arg3.MessageId);
                    if (paginationReply != null && (isPageLeft || isPageRight))
                    {
                        await paginationReply.ReplyMessage.ModifyAsync(async (t) =>
                        {
                            if (isPageLeft)
                            {
                                t.Content = paginationReply.PaginationMessage.PreviousPage();
                                await message.RemoveReactionAsync(new Emoji("◀️"), arg3.UserId);
                            }
                            else if (isPageRight)
                            {
                                t.Content = paginationReply.PaginationMessage.NextPage();
                                await message.RemoveReactionAsync(new Emoji("▶️"), arg3.UserId);
                            }
                        });
                    }
                };

                _client.Log += Log;

#if DEBUG
                var token = _configuration.TestingToken;
#else
                var token = _configuration.Token;
#endif

                if (token == null)
                    throw new Exception("Bot token not set in config.json");

                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

                await Task.Delay(-1);
            }
        }

        DateTime lastReply = DateTime.Now.AddMinutes(-5);
        private async Task InlineTagReply(SocketMessage message)
        {
            if(new Random().Next(0, _configuration.RandomOpinionChance) == 1 && DateTime.Now > lastReply.AddMinutes(5))
            {
                lastReply = DateTime.Now;
                await message.Channel.SendMessageAsync(PlsModule.GeneratePls());
            }

            if (message.Source != MessageSource.User) return;
            var regexMatches = Regex.Matches(message.Content, "\\$+([A-Za-z0-9!.#@$%^&()]+)");
            
            if(regexMatches.Count > 0)
                await _replyTracker.DeleteAll(message.Id);

            foreach (Match match in regexMatches)
            {
                if (message.Channel is IGuildChannel channel)
                {
                    var tag = _tagService!.GetTag(channel.GuildId, match.Groups[1].ToString());
                    if (tag == null)
                        continue;

                    if (!tag.IsFile)
                        await _replyTracker!.SmartReplyAsync(message.Channel, message, tag.Content);
                    else
                    {
                        await using MemoryStream stream = new(Convert.FromBase64String(tag.Content));
                        await message.Channel.SendFileAsync(stream, tag.FileName);
                    }
                }
            }
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<ReplyTrackerService>()
                .AddSingleton<PaginationReplyTracker<TagService>>()
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
