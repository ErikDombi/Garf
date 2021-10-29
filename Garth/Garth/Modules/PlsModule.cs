using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garth.Modules
{
    public class PlsModule : ModuleBase<SocketCommandContext>
    {
        [Command("pls")]
        public Task pls([Remainder]string text = null)
        {
            if (text != null)
                return Task.CompletedTask;

            string[] phrases = new[] { 
                "so",
                "go work on khronos",
                "look at the zoom transcript",
                "in the industry",
                "Apple products are terrible",
                "Intel is the worst",
                "Steve Jobs profited off free manual child labor" ,
                "bozo sort is awful",
                "We condemn capitalism",
                "Gay Dragon Boys and the Holy Grail",
                "You shouldn't follow your dreams",
                "Ask me in tutorial",
                "We'll discuss that in tutorial",
                "Java stole this from C++",
                "Stroupstrup is a genius",
                "C++20 guys are spoiled"
            };
            int item = new Random().Next(0, phrases.Length);
            ReplyAsync(phrases[item]);
            return Task.CompletedTask;
        }
    }
}
