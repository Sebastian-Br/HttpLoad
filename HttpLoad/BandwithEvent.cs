using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpLoad
{
    internal class BandwithEvent
    {
        public BandwithEvent()
        {
            EventTime = new();
            BytesReceived = new();
        }

        public DateTime EventTime { get; set; }

        public int BytesReceived { get; set; }
    }
}
