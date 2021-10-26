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

        private Embed error(string msg)
        {
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription(msg)
                .Build();
        }

        [Command("tag")]
        [Alias("t")]
        public Task TagCommand(string subcmd, string name = null, [Remainder] string? text = null)
        {
            if(subcmd.Equals("create", StringComparison.OrdinalIgnoreCase) || subcmd.Equals("add", StringComparison.OrdinalIgnoreCase)) {
                if(name == null)
                {
                    ReplyAsync("**You need to supply a name for the tag!**");
                    return Task.CompletedTask;
                }

                if(new string[] {"create", "delete", "info", "edit", "search", "add", "remove"}.Contains(name.ToLower()))
                {
                    ReplyAsync("**Invalid tag name!**");
                    return Task.CompletedTask;
                }

                if(text == null)
                {
                    ReplyAsync("**You need to supply a value for the tag!**");
                    return Task.CompletedTask;
                }

                if(Regex.IsMatch(name, "[^A-Za-z0-9!.#@$%^&()]"))
                {
                    ReplyAsync("**Invalid tag name!**");
                    return Task.CompletedTask;
                }

                if(Regex.IsMatch(name, "<@![0-9]{18}>"))
                {
                    ReplyAsync("**Illegal mention in tag name!**");
                    return Task.CompletedTask;
                }

                if (Regex.IsMatch(text, "<@![0-9]{18}>"))
                {
                    ReplyAsync("**Illegal mention in tag content!**");
                    return Task.CompletedTask;
                }

                if (TagService!.Data.Any(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    ReplyAsync("A tag with that name already exists!");
                    return Task.CompletedTask;
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
                    ReplyAsync("**You need to supply a tag name!**");
                    return Task.CompletedTask;
                }
                var tag = TagService!.Data.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if(tag == null)
                {
                    ReplyAsync(tag != null ? tag.Content : "**Tag not found!**");
                    return Task.CompletedTask;
                }
                if(tag.CreatorId != Context.User.Id && (Context.User.Id != _configuration?.BotOwnerId))
                {
                    ReplyAsync("**You do not have permission to delete this tag!");
                    return Task.CompletedTask;
                }
                TagService.Data.Remove(tag);
                TagService.Save();
                Context.Message.AddReactionAsync(new Emoji("☑️"));
            }
            else if (subcmd.Equals("edit", StringComparison.OrdinalIgnoreCase))
            {
                if (name == null)
                {
                    ReplyAsync("**You need to supply a tag name!**");
                    return Task.CompletedTask;
                }
                var tag = TagService!.Data.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (tag == null)
                {
                    ReplyAsync(embed: error("**Tag not found!**"));
                    return Task.CompletedTask;
                }
                if (tag.CreatorId != Context.User.Id && (Context.User.Id != _configuration?.BotOwnerId))
                {
                    ReplyAsync(embed: error("**You do not have permission to edit this tag!"));
                    return Task.CompletedTask;
                }

                if (text == null)
                {
                    ReplyAsync(embed: error("**You need to supply a value for the tag!**"));
                    return Task.CompletedTask;
                }

                tag.Content = text;
                TagService.Save();
                Context.Message.AddReactionAsync(new Emoji("☑️"));
            }
            else if (subcmd.Equals("info", StringComparison.OrdinalIgnoreCase))
            {
                if (name == null)
                {
                    ReplyAsync("**You need to supply a tag name!**");
                    return Task.CompletedTask;
                }
                var tag = TagService!.Data.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (tag == null)
                {
                    ReplyAsync(tag != null ? tag.Content : "**Tag not found!**");
                    return Task.CompletedTask;
                }

                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithAuthor(_client?.GetUser(tag.CreatorId));
                embedBuilder.WithTitle(tag.Name);
                embedBuilder.WithDescription($"```{tag.Content}```");
                embedBuilder.WithFooter(tag.CreationDate);
                ReplyAsync(embed: embedBuilder.Build());
            }
            else if (subcmd.Equals("search", StringComparison.OrdinalIgnoreCase) || subcmd.Equals("find", StringComparison.OrdinalIgnoreCase))
            {
                if (name == null)
                {
                    ReplyAsync("**You need to supply a tag name!**");
                    return Task.CompletedTask;
                }

                var tags = TagService!.Data.Where(t => t.Name.ToLower().Contains(name.ToLower())).Take(10).OrderBy(t => t.Name.Length).ToList();
                if(tags.Count == 0)
                {
                    ReplyAsync("**No tags found!**");
                    return Task.CompletedTask;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("```d");
                for(int i = 0; i < tags.Count; ++i)
                {
                    sb.AppendLine($"[{i + 1}] {tags[i].Name}");
                }
                sb.AppendLine("```");
                ReplyAsync(sb.ToString());
            }
            else
            {
                var tag = TagService!.Data.FirstOrDefault(t => t.Name.Equals(subcmd, StringComparison.OrdinalIgnoreCase));
                ReplyAsync(tag != null ? tag.Content : "**Tag not found!**");
            }

            return Task.CompletedTask;
        }
    }
}
