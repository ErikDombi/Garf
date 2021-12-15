using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garth
{
    public class Configuration
    {
        public string Token { get; set; } = String.Empty;
        public string TestingToken { get; set; } = String.Empty;
        public ulong BotOwnerId { get; set; } = 0;
        public string[] Prefixes { get; set; } = new[] { "g", "g++" };
        public int RandomOpinionChance { get; set; } = 50;

        public static Configuration LoadConfiguration(string file = "config.json")
        {
            if(!System.IO.File.Exists(file))
            {
                System.IO.File.Create(file).Close();
                System.IO.File.WriteAllText(file, JsonConvert.SerializeObject(new Configuration(), Formatting.Indented));
                throw new Exception("config.json file created. Please update the values");
            }
            var content = System.IO.File.ReadAllText(file);
            return JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText(file)); 
        }
    }
}
