using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Garth.Services;

namespace Garth.Modules
{
    public class PlsModule : ModuleBase<SocketCommandContext>
    {
        public ReplyTrackerService? _replyTracker { get; set; }

        public static string GeneratePls()
        {
            string[] phrases = new[] {
                "so",
                "go work on khronos",
                "go work on expression evaluator",
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
                "C++20 guys are spoiled",
                "fanshawe students tend to have a lot more, for lack of a better term, shit in their life to deal with"
            };
            int item = new Random().Next(0, phrases.Length);
            return phrases[item];
        }

        [Command("pls")]
        public async Task pls([Remainder]string text = null)
        {
            await _replyTracker!.SmartReplyAsync(Context, PlsModule.GeneratePls());
        }
    }
}
