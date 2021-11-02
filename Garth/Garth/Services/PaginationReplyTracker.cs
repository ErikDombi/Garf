using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Garth.Models;

namespace Garth.Services
{
    public class PaginationReplyTracker<T> : List<PaginationReply<T>>
    {
        private readonly int _mSize = 15;

        public DiscordSocketClient? _client { get; set; }

        public new void Add(PaginationReply<T> item)
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

        public async Task<IUserMessage> SmartReplyAsync(SocketCommandContext Context, T obj, Func<T, int, string> paginationFunc)
        {
            return await SmartReplyAsync(Context.Channel, Context.Message, obj, paginationFunc);
        }

        public async Task<IUserMessage> SmartReplyAsync(ISocketMessageChannel channel, IMessage source, T obj, Func<T, int, string> paginationFunc)
        {
            var interaction = this.FirstOrDefault(msg => msg.SourceMessage.Id == source.Id);

            if (interaction == null) // This message hasn't been replied to. Create a new reply
            {
                var reply = await channel.SendMessageAsync(text: paginationFunc(obj, 0));
                this.Add(new PaginationReply<T>(source, reply, obj, paginationFunc));

                return reply;
            }

            var prevReply = await interaction.ReplyMessage.Channel.GetMessageAsync(interaction.ReplyMessage.Id);
            if (prevReply == null)
            {
                var reply = await channel.SendMessageAsync(text: paginationFunc(obj, 0));
                this.Add(new PaginationReply<T>(source, reply, obj, paginationFunc));

                await (await channel.GetMessageAsync(reply.Id)).AddReactionAsync(new Emoji("️◀️"));
                await (await channel.GetMessageAsync(reply.Id)).AddReactionAsync(new Emoji("️▶️"));

                return reply;
            }

            await interaction.ReplyMessage.ModifyAsync(x =>
            {
                x.Content = "";
            });
            return interaction.ReplyMessage;
        }
    }

    public class PaginationReply<T>
    {
        public IMessage SourceMessage { get; set; }
        public RestUserMessage ReplyMessage { get; set; }
        public PaginationMessage<T> PaginationMessage { get; set; }

        public PaginationReply(IMessage sourceMessage, RestUserMessage replyMessage, T obj, Func<T, int, string> paginationFunc)
        {
            this.SourceMessage = sourceMessage;
            this.ReplyMessage = replyMessage;
            this.PaginationMessage = new PaginationMessage<T>(obj, paginationFunc);
        }
    }
}
