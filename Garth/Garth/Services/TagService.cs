using Garth.IO;
using Garth.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garth.Services
{
    public class TagService : JsonInterfacer<List<Tag>>
    {
        public TagService() : base("tags.json", (location) => { Console.WriteLine("Created tags.json"); })
        {

        }
    }
}
