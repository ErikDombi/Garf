using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Garth.Models
{
    public class ScriptHost
    {
        // Provides a class to pass as a global when evaluating expressions.
        public new SocketCommandContext Context { get; set; }
        public ISocketMessageChannel Channel { get; set; }
        public DiscordSocketClient Client { get; set; }
        public SocketGuild Guild { get; set; }
        public SocketUser User { get; set; }

        public ScriptHost(SocketCommandContext Context)
        {
            this.Context = Context;
            this.Channel = Context.Channel;
            this.Client = Context.Client;
            this.Guild = Context.Guild;
            this.User = Context.User;
        }
    }
}
