using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Garth.Models;
using Garth.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Garth.Modules
{
    public class TagModule : ModuleBase<SocketCommandContext>
    {
        public TagService? TagService { get; set; }
        public DiscordSocketClient? _client { get; set; }
        public Configuration? _configuration { get; set; }
        public ReplyTrackerService? _replyTracker { get; set; }
        public PaginationReplyTracker<TagService> _paginationService { get; set; }

        private Embed error(string msg)
        {
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription(msg)
                .Build();
        }

        [Command("tag")]
        [Alias("t")]
        public async Task TagCommand(string subcmd, string name = null, [Remainder] string? text = null)
        {
            if(subcmd.Equals("create", StringComparison.OrdinalIgnoreCase) || subcmd.Equals("add", StringComparison.OrdinalIgnoreCase)) {
                if(name == null)
                {
                    await _replyTracker.SmartReplyAsync(Context, "**You need to supply a name for the tag!**");
                    return;
                }

                if(new string[] {"create", "delete", "info", "edit", "search", "add", "remove"}.Contains(name.ToLower()))
                {
                    await _replyTracker.SmartReplyAsync(Context, "**Invalid tag name!**");
                    return;
                }

                if(text == null)
                {
                    await _replyTracker.SmartReplyAsync(Context, "**You need to supply a value for the tag!**");
                    return;
                }

                if(Regex.IsMatch(name, "[^A-Za-z0-9!.#@$%^&()]"))
                {
                    await _replyTracker.SmartReplyAsync(Context, "**Invalid tag name!**");
                    return;
                }

                if(Regex.IsMatch(name, "<@![0-9]{18}>"))
                {
                    await _replyTracker.SmartReplyAsync(Context, "**Illegal mention in tag name!**");
                    return;
                }

                if (Regex.IsMatch(text, "<@![0-9]{18}>"))
                {
                    await _replyTracker.SmartReplyAsync(Context, "**Illegal mention in tag content!**");
                    return;
                }

                if (TagService!.Data.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    await _replyTracker.SmartReplyAsync(Context, "A tag with that name already exists!");
                    return;
                }

                TagService!.Data.Add(new Tag
                {
                    Name = name,
                    Content = text,
                    CreatorId = Context.User.Id,
                    CreatorName = Context.User.ToString()
                });
                TagService.Save();
                Context.Message.AddReactionAsync(new Emoji("☑️"));
            }
            else if(subcmd.Equals("delete", StringComparison.OrdinalIgnoreCase) || subcmd.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                if (name == null)
                {
                    await _replyTracker.SmartReplyAsync(Context, "**You need to supply a tag name!**");
                    return;
                }
                var tag = TagService!.Data.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if(tag == null)
                {
                    await _replyTracker.SmartReplyAsync(Context, tag != null ? tag.Content : "**Tag not found!**");
                    return;
                }
                if(tag.CreatorId != Context.User.Id && (Context.User.Id != _configuration?.BotOwnerId))
                {
                    await _replyTracker.SmartReplyAsync(Context, "**You do not have permission to delete this tag!");
                    return;
                }
                TagService.Data.Remove(tag);
                TagService.Save();
                Context.Message.AddReactionAsync(new Emoji("☑️"));
            }
            else if (subcmd.Equals("edit", StringComparison.OrdinalIgnoreCase))
            {
                if (name == null)
                {
                    await _replyTracker.SmartReplyAsync(Context, "**You need to supply a tag name!**");
                    return;
                }
                var tag = TagService!.Data.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (tag == null)
                {
                    await _replyTracker.SmartReplyAsync(Context, embed: error("**Tag not found!**"));
                    return;
                }
                if (tag.CreatorId != Context.User.Id && (Context.User.Id != _configuration?.BotOwnerId))
                {
                    await _replyTracker.SmartReplyAsync(Context, embed: error("**You do not have permission to edit this tag!"));
                    return;
                }

                if (text == null)
                {
                    await _replyTracker.SmartReplyAsync(Context, embed: error("**You need to supply a value for the tag!**"));
                    return;
                }

                tag.Content = text;
                TagService.Save();
                Context.Message.AddReactionAsync(new Emoji("☑️"));
            }
            else if (subcmd.Equals("info", StringComparison.OrdinalIgnoreCase))
            {
                if (name == null)
                {
                    await _replyTracker.SmartReplyAsync(Context, "**You need to supply a tag name!**");
                    return;
                }
                var tag = TagService!.Data.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (tag == null)
                {
                    _replyTracker.SmartReplyAsync(Context, tag != null ? tag.Content : "**Tag not found!**");
                    return;
                }

                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithAuthor(_client?.GetUser(tag.CreatorId));
                embedBuilder.WithTitle(tag.Name);
                embedBuilder.WithDescription($"```{tag.Content}```");
                embedBuilder.WithFooter(tag.CreationDate);
                _replyTracker.SmartReplyAsync(Context, embed: embedBuilder.Build());
            }
            else if (subcmd.Equals("search", StringComparison.OrdinalIgnoreCase) || subcmd.Equals("find", StringComparison.OrdinalIgnoreCase))
            {
                if (name == null)
                {
                    var reply = await _paginationService.SmartReplyAsync(Context, TagService, (tags, page) =>
                    {
                        StringBuilder responseString =
                            new StringBuilder(
                                $"List of Garth tags (Page {page + 1} of {Math.Ceiling(tags.Data.Count / 10.0)})");

                        int startIndex = page * 10;
                        if (startIndex >= tags.Data.Count)
                            return "";

                        responseString.AppendLine("```d\n");

                        var pageTags = tags.Data.Skip(startIndex).Take(10).ToList();
                        for (int i = 0; i < pageTags.Count; ++i)
                        {
                            responseString.AppendLine($"[{startIndex + i + 1}] {pageTags[i].Name}");
                        }

                        responseString.AppendLine("```");

                        return responseString.ToString();
                    });

                    await reply.AddReactionAsync(new Emoji("◀️"));
                    await reply.AddReactionAsync(new Emoji("▶️"));
                    return;
                }

                var tags = TagService!.Data.Where(t => t.Name.ToLower().Contains(name.ToLower())).Take(10).OrderBy(t => t.Name.Length).ToList();
                if(tags.Count == 0)
                {
                    await _replyTracker.SmartReplyAsync(Context, "**No tags found!**");
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("```d");
                for(int i = 0; i < tags.Count; ++i)
                {
                    sb.AppendLine($"[{i + 1}] {tags[i].Name}");
                }
                sb.AppendLine("```");
                _replyTracker.SmartReplyAsync(Context, sb.ToString());
            }
            else
            {
                var tag = TagService!.Data.FirstOrDefault(t => t.Name.Equals(subcmd, StringComparison.OrdinalIgnoreCase));
                _replyTracker.SmartReplyAsync(Context, tag != null ? tag.Content : "**Tag not found!**");
            }

            return;
        }
    }
}
