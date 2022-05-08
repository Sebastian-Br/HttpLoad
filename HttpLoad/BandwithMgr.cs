using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpLoad
{
    internal class BandwithMgr
    {
        public BandwithMgr(int timeout)
        {
            BandwithEvents = new();
            Timeout = timeout;
            TotalDataGB = 0;
        }
        private List<BandwithEvent> BandwithEvents { get; set; }

        private int Timeout { get;set; }

        public void AddEvent(BandwithEvent b)
        {
            BandwithEvents.Add(b);
            TotalDataGB += (double)b.BytesReceived / 1000000000.0;
        }

        private double TotalDataGB { get; set; }

        public double GetDataRate()
        {
            int Bytes = 0;
            foreach(BandwithEvent bandwithEvent in BandwithEvents.ToList())
            {
                if(bandwithEvent.EventTime.AddSeconds(Timeout) >= DateTime.Now)
                {
                    Bytes += bandwithEvent.BytesReceived;
                }
                else
                {
                    BandwithEvents.Remove(bandwithEvent);
                }
            }

            return Bytes / (double)Timeout;
        }

        public double GetTotalDataReceived()
        {
            return TotalDataGB;
        }
    }
}