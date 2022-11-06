using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpLoad
{
    /// <summary>
    /// Class used for parsing appsettings.json
    /// </summary>
    internal class Configuration
    {
        public Configuration()
        {
            Hosts = new();
            TaskCount = -1;
            AcceptHeader = "gzip, deflate, br";
            MaxBandwith_GigabytePerHour = 12; // beware, this does not currently do anything!
        }
        public List<Host> Hosts { get; set; }

        /// <summary>
        /// The default header gets replaced in Program.cs
        /// </summary>
        public string AcceptHeader { get; set; }

        /// <summary>
        /// The amount of tasks/load cycles that will run in parallel
        /// </summary>
        public int TaskCount { get; set; }

        /// <summary>
        /// This functionality still needs to be added
        /// </summary>
        public double MaxBandwith_GigabytePerHour { get; set; }
    }
}