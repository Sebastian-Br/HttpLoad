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
            TaskCount = -1;
            AcceptHeader = "gzip, deflate, br";
            MaxBandwith_GigabytePerHour = 12;
        }
        public List<WebPage> WebPages { get; set; }

        public string AcceptHeader { get; set; }

        public int TaskCount { get; set; }

        public double MaxBandwith_GigabytePerHour { get; set; }
    }
}
