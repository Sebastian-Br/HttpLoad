using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpLoad
{
    /// <summary>
    /// Describes how much data was received when a request succeeded.
    /// Comes with a time stamp to be able to compute the amount of data received during a certain time span.
    /// </summary>
    internal class BandwithEvent
    {
        public BandwithEvent()
        {
        }

        public DateTime EventTime { get; set; }

        public int BytesReceived { get; set; }
    }
}