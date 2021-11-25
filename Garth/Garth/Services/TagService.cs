using Garth.IO;
using Garth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Garth.Services
{
    public class TagService : JsonInterfacer<List<Tag>>
    {
        public TagService() : base("tags.json", (location) => { Console.WriteLine("Created tags.json"); })
        {

        }

        public Tag? GetTag(ulong server, string name)
        {
            return Data.Where(t => t.Server == server || t.Global).FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
