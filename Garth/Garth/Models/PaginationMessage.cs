using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Garth.Models
{
    public class PaginationMessage<T>
    {
        public int Page { get; private set; } = 0;

        private readonly Func<T, int, string> paginationFunction;

        private readonly T functionParam;

        public PaginationMessage(T obj, Func<T, int, string> buildPage)
        {
            functionParam = obj;
            paginationFunction = buildPage;
        }

        public string NextPage()
        {
            string reply = paginationFunction.Invoke(functionParam, ++Page);
            if (reply == "")
                return paginationFunction.Invoke(functionParam, --Page);
            return reply;
        }

        public string PreviousPage()
        {
            if (Page > 0)
                --Page;
            return paginationFunction.Invoke(functionParam, Page);
        }

        public string CurrentPage()
        {
            return paginationFunction.Invoke(functionParam, Page);
        }
    }
}
