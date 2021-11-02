using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;

namespace Garth.Services
{
    public class ReplyTrackerService : List<Reply>
    {
        private readonly int _mSize = 15;

        public DiscordSocketClient? _client { get; set; }

        public new void Add(Reply item)
        {
            if (Count >= _mSize)
            {
                base.RemoveAt(0);
            }

            base.Add(item);
        }

        public async Task DeleteAll(SocketUserMessage source)
        {
            await DeleteAll(source.Id);
        }

        public async Task DeleteAll(ulong sourceId)
        {
            var replies = this.Where(_ => _.SourceMessage.Id == sourceId);
            foreach (var reply in replies)
            {
                await reply.ReplyMessage.DeleteAsync();
                base.Remove(reply);
            }
        }

        public async Task<IUserMessage> SmartReplyAsync(SocketCommandContext Context, string text = "", Embed embed = null)
        {
            return await SmartReplyAsync(Context.Channel, Context.Message, text, embed);
        }

        public async Task<IUserMessage> SmartReplyAsync(ISocketMessageChannel channel, IMessage source, string text = "", Embed embed = null, bool disableEdit = false)
        {
            var interaction = this.FirstOrDefault(msg => msg.SourceMessage.Id == source.Id);

            if (interaction == null || disableEdit) // This message hasn't been replied to. Create a new reply
            {
                var reply = await channel.SendMessageAsync(text: text, embed: embed);
                this.Add(new Reply(source, reply));
                return reply;
            }

            var prevReply = await interaction.ReplyMessage.Channel.GetMessageAsync(interaction.ReplyMessage.Id);
            if (prevReply == null && !disableEdit)
            {
                var reply = await channel.SendMessageAsync(text: text, embed: embed);
                this.Add(new Reply(source, reply));
                return reply;
            }

            await interaction.ReplyMessage.ModifyAsync(x =>
            {
                x.Content = text;
                x.Embed = embed;
            });
            return interaction.ReplyMessage;
        }
    }

    public class Reply
    {
        public IMessage SourceMessage { get; set; }
        public RestUserMessage ReplyMessage { get; set; }

        public Reply(IMessage sourceMessage, RestUserMessage replyMessage)
        {
            this.SourceMessage = sourceMessage;
            this.ReplyMessage = replyMessage;
        }
    }
}
