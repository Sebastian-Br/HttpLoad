using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpLoad
{
    /// <summary>
    /// Provides information such as the total data received and current (average) datarate.
    /// </summary>
    internal class BandwithMgr
    {
        public BandwithMgr(int timeout)
        {
            BandwithEvents = new();
            Timeout = timeout;
            TotalDataGB = 0;
        }

        private List<BandwithEvent> BandwithEvents { get; set; }

        /// <summary>
        /// The time after which a BandwithEvent is removed from the list
        /// </summary>
        private int Timeout { get;set; }

        /// <summary>
        /// Adds one such event and updates the TotalDataGB (received).
        /// </summary>
        /// <param name="b">The BandwidthEvent to Add</param>
        public void AddBandwidthEvent(BandwithEvent b)
        {
            if (BandwithEvents.Count < 1000000)
            {
                BandwithEvents.Add(b);
                TotalDataGB += (double)b.BytesReceived / 1000000000.0;
            }

        }

        private double TotalDataGB { get; set; }

        /// <summary>
        /// Checks if a BandwithEvent has timed out. If so, removes it from the list.
        /// Adds up all data received over a *Timeout* seconds period.
        /// </summary>
        /// <returns>Returns the average datarate during the Timeout period</returns>
        public double GetDataRate_BytesPerSecond()
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

        /// <summary>
        /// Adds up all data received over a *Timeout* seconds period.
        /// Does not remove and BandwithEvents because calling this function around the same time might corrupt the list.
        /// </summary>
        /// <returns>Returns the average datarate during the Timeout period</returns>
        public double GetDataRate_BytesPerSecond_DontRemoveEvents()
        {
            int Bytes = 0;
            foreach (BandwithEvent bandwithEvent in BandwithEvents.ToList())
            {
                if (bandwithEvent.EventTime.AddSeconds(Timeout) >= DateTime.Now)
                {
                    Bytes += bandwithEvent.BytesReceived;
                }
            }

            return Bytes / (double)Timeout;
        }

        public double GetTotalDataReceivedGB()
        {
            return TotalDataGB;
        }
    }
}