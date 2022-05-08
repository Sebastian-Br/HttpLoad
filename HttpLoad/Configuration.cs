using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpLoad
{
    internal class Configuration
    {
        public Configuration()
        {
            WebPages = new();
        }
        public List<WebPage> WebPages { get; set; }
    }
}
