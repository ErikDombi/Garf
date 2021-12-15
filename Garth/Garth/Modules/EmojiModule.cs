using Discord.Commands;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Garth.Services;

namespace Garth.Modules
{
    public class EmojiModule : ModuleBase<SocketCommandContext>
    {
        public ReplyTrackerService? _replyTracker { get; set; }

        //Don't worry about credential leaking. This is a public api key used by gboard. 
        private const string query =
            "https://tenor.googleapis.com/v2/featured?&contentfilter=high&media_filter=png_transparent&component=proactive&collection=emoji_kitchen_v5&locale=en_us&country=us&q=&key=AIzaSyAyimkuYQYF_FXVALexPuGQctUWRURdCYQ";

        [Command("emoji")]
        [Alias("em","mashup")]
        public async Task emoji(string emoji1, string emoji2)
        {
            var msg = await _replyTracker.SmartReplyAsync(Context, "Attempting mashup...");

            //Send query to API
            HttpClient client = new HttpClient();
            var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, query + $"&q={emoji1}_{emoji2}"));

            //Request failed
            if (!response.IsSuccessStatusCode)
            {
                await msg.ModifyAsync(x=> x.Content = $"Request failed! ( error {response.StatusCode} )");
                return;
            }

            //Request successful, check if response contains an emoji
            JObject node = JObject.Parse(await response.Content.ReadAsStringAsync());

            if (node["results"].HasValues)
            {
                await msg.ModifyAsync(x => x.Content = node["results"].First["url"].ToString());
            }
            else
            {
                await msg.ModifyAsync(x => x.Content = $"No mashup possible for {emoji1} + {emoji2}");
            }
            
        }

    }
}
