using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Garth.Models;
using Garth.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
            if(subcmd.Equals("create", StringComparison.OrdinalIgnoreCase) ||
               subcmd.Equals("add", StringComparison.OrdinalIgnoreCase) ||
               subcmd.Equals("createglobal", StringComparison.OrdinalIgnoreCase) ||
               subcmd.Equals("addglobal", StringComparison.OrdinalIgnoreCase))
            {
                if(string.IsNullOrWhiteSpace(name))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**You need to supply a name for the tag!**");
                    return;
                }

                if(new string[] {"create", "delete", "info", "edit", "search", "add", "remove", "global", "createglobal", "addglobal"}.Contains(name.ToLower()))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**Invalid tag name!**");
                    return;
                }

                if(text == null && Context.Message.Attachments.Count == 0)
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**You need to supply a value for the tag!**");
                    return;
                }

                if(Regex.IsMatch(name, "[^A-Za-z0-9!.#@$%^&()]"))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**Invalid tag name!**");
                    return;
                }

                if(Regex.IsMatch(name, "<@![0-9]{18}>"))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**Illegal mention in tag name!**");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text, "<@![0-9]{18}>"))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**Illegal mention in tag content!**");
                    return;
                }

                if (TagService!.Data.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "A tag with that name already exists!");
                    return;
                }

                if (Context.Message.Attachments.Count > 0)
                {
                    WebClient wc = new();
                    var bytes = await wc.DownloadDataTaskAsync(Context.Message.Attachments.FirstOrDefault()!.Url);
                    TagService!.Data.Add(new Tag
                    {
                        Name = name,
                        Content = Convert.ToBase64String(bytes),
                        CreatorId = Context.User.Id,
                        CreatorName = Context.User.ToString(),
                        IsFile = true,
                        FileName = Context.Message.Attachments.FirstOrDefault()!.Filename,
                        Server = Context.Guild.Id,
                        Global = subcmd.Equals("createglobal", StringComparison.OrdinalIgnoreCase) ||
                                 subcmd.Equals("addglobal", StringComparison.OrdinalIgnoreCase)
                    });
                }
                else
                {
                    TagService!.Data.Add(new Tag
                    {
                        Name = name,
                        Content = text!,
                        CreatorId = Context.User.Id,
                        CreatorName = Context.User.ToString(),
                        Server = Context.Guild.Id,
                        Global = subcmd.Equals("createglobal", StringComparison.OrdinalIgnoreCase) ||
                                 subcmd.Equals("addglobal", StringComparison.OrdinalIgnoreCase)
                    });
                }

                TagService.Save();
                await Context.Message.AddReactionAsync(new Emoji("☑️"));
            }
            else if(subcmd.Equals("delete", StringComparison.OrdinalIgnoreCase) || subcmd.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**You need to supply a tag name!**");
                    return;
                }

                var tag = TagService!.GetTag(Context.Guild.Id, name);
                if (tag == null)
                {
                    await _replyTracker!.SmartReplyAsync(Context, tag != null ? tag.Content : "**Tag not found!**");
                    return;
                }
                if(tag.CreatorId != Context.User.Id && (Context.User.Id != _configuration?.BotOwnerId))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**You do not have permission to delete this tag!**");
                    return;
                }
                TagService.Data.Remove(tag);
                TagService.Save();

                await Context.Message.AddReactionAsync(new Emoji("☑️"));
            }
            else if (subcmd.Equals("edit", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**You need to supply a tag name!**");
                    return;
                }
                var tag = TagService!.GetTag(Context.Guild.Id, name);
                if (tag == null)
                {
                    await _replyTracker!.SmartReplyAsync(Context, embed: error("**Tag not found!**"));
                    return;
                }
                if (tag.CreatorId != Context.User.Id && (Context.User.Id != _configuration?.BotOwnerId))
                {
                    await _replyTracker!.SmartReplyAsync(Context, embed: error("**You do not have permission to edit this tag!"));
                    return;
                }

                if (text == null && Context.Message.Attachments.Count == 0)
                {
                    await _replyTracker!.SmartReplyAsync(Context, embed: error("**You need to supply a value for the tag!**"));
                    return;
                }

                tag.Content = text!;
                TagService.Save();
                await Context.Message.AddReactionAsync(new Emoji("☑️"));
            }
            else if (subcmd.Equals("info", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**You need to supply a tag name!**");
                    return;
                }
                var tag = TagService!.GetTag(Context.Guild.Id, name);
                if (tag == null)
                {
                    await _replyTracker!.SmartReplyAsync(Context, tag != null ? tag.Content : "**Tag not found!**");
                    return;
                }

                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithAuthor(_client?.GetUser(tag.CreatorId));
                embedBuilder.WithTitle(tag.Name);
                if (!tag.IsFile)
                    embedBuilder.WithDescription($"```{tag.Content}```");
                else
                    embedBuilder.WithDescription("**Tag is a file**");
                embedBuilder.WithFooter(tag.CreationDate);
                embedBuilder.AddField("Global", tag.Global);
                embedBuilder.AddField("Origin", tag.Server);
                await _replyTracker!.SmartReplyAsync(Context, embed: embedBuilder.Build());
            }
            else if (subcmd.Equals("global", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**You need to supply a tag name!**");
                    return;
                }
                var tag = TagService!.GetTag(Context.Guild.Id, name);
                if (tag == null)
                {
                    await _replyTracker!.SmartReplyAsync(Context, tag != null ? tag.Content : "**Tag not found!**");
                    return;
                }

                if (tag.CreatorId == Context.User.Id || Context.User.Id == _configuration?.BotOwnerId)
                {
                    tag.Global = !tag.Global;
                    if (tag.Global)
                        await _replyTracker!.SmartReplyAsync(Context, $"Made tag **{tag.Name}** global!");
                    else
                        await _replyTracker!.SmartReplyAsync(Context, $"Made tag **{tag.Name}** not global!");
                    TagService.Save();
                }
                else
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**Only the tag owner can do that!**");
                }
            }
            else if (subcmd.Equals("search", StringComparison.OrdinalIgnoreCase) || subcmd.Equals("find", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    var reply = await _paginationService.SmartReplyAsync(Context, TagService!, (tags, page) =>
                    {
                        StringBuilder responseString =
                            new StringBuilder(
                                $"List of Garth tags (Page {page + 1} of {Math.Ceiling(tags.Data.Where(t => t.Server == Context.Guild.Id || t.Global).ToList().Count / 10.0)})");

                        int startIndex = page * 10;
                        if (startIndex >= tags.Data.Where(t => t.Server == Context.Guild.Id || t.Global).ToList().Count)
                            return "";

                        responseString.AppendLine("```d\n");

                        var pageTags = tags.Data.Where(t => t.Server == Context.Guild.Id || t.Global).Skip(startIndex).Take(10).ToList();
                        if (pageTags.Count == 0)
                            return "";

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

                var tags = TagService!.Data.Where(t => (t.Server == Context.Guild.Id || t.Global) && t.Name.ToLower().Contains(name.ToLower())).Take(10).OrderBy(t => t.Name.Length).ToList();
                if(tags.Count == 0)
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**No tags found!**");
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("```d");
                for(int i = 0; i < tags.Count; ++i)
                {
                    sb.AppendLine($"[{i + 1}] {tags[i].Name}");
                }
                sb.AppendLine("```");
                await _replyTracker!.SmartReplyAsync(Context, sb.ToString());
            }
            else
            {
                var tag = TagService!.GetTag(Context.Guild.Id, subcmd);
                if (tag == null)
                {
                    await _replyTracker!.SmartReplyAsync(Context, "**Tag not found!**");
                    return;
                }

                if(!tag!.IsFile)
                    await _replyTracker!.SmartReplyAsync(Context, tag.Content);
                else
                {
                    await using MemoryStream stream = new(Convert.FromBase64String(tag.Content));
                    await Context.Channel.SendFileAsync(stream, tag.FileName);
                }
            }

            return;
        }
    }
}
