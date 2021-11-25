using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Garth.Models
{
    public class Tag
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public string CreatorName { get; set; }
        public ulong CreatorId { get; set; }
        public string CreationDate { get; set; } = DateTime.Now.ToString();
        public bool IsFile { get; set; } = false;
        public string FileName { get; set; } = string.Empty;
        public bool Global { get; set; } = false;
        public ulong Server { get; set; }
    }
}
