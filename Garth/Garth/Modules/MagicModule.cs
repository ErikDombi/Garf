using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garth.Modules
{
    public class MagicModule : ModuleBase<SocketCommandContext>
    {
        [Command("magic")]
        [Alias("magik", "magic8", "magik8", "m8")]
        public async Task magic([Remainder] string text = null)
        {
            var msg = await ReplyAsync("Calculating...");
            await Task.Delay(1000);

            string[] phrases = new[] {
                "It is certain",
                "It is decidedly so",
                "Without a doubt",
                "Yes - definitely",
                "You may rely on it",
                "As I see it, yes",
                "Most likely",
                "Outlook good",
                "Yes",
                "Signs point to yes",
                "Reply hazy, try again",
                "Ask again later",
                "Better not tell you now",
                "Cannot predict now",
                "Concentrate and ask again",
                "Don't count on it",
                "My reply is no",
                "My sources say no",
                "Outlook not so good",
                "Very doubtful"
            };
            int item = new Random().Next(0, phrases.Length);
            await msg.ModifyAsync(x => x.Content = phrases[item]);
        }
    }
}
