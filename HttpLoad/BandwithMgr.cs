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
            Timeout = timeout;
            TotalDataGB = 0;
            BytesReceivedDuringLastTimeout = 0;
            BytesReceivedDuringTimeoutTmp = 0;
            Task.Run(() => ContinuouslyClearTmpReceivedData());
        }

        private int BytesReceivedDuringLastTimeout;

        private int BytesReceivedDuringTimeoutTmp;

        /// <summary>
        /// The time after which a BandwithEvent is removed from the list
        /// </summary>
        private int Timeout { get;set; }

        /// <summary>
        /// Adds one such event and updates the TotalDataGB (received).
        /// </summary>
        /// <param name="b">The BandwidthEvent to Add</param>
        public void AddReceivedBytes(int receivedBytes)
        {
            Interlocked.Add(ref BytesReceivedDuringTimeoutTmp, receivedBytes);
        }

        private double TotalDataGB { get; set; }

        public double GetDataRateBytesPerSecond()
        {
            return (double)BytesReceivedDuringLastTimeout / (double) Timeout;
        }

        private async Task ContinuouslyClearTmpReceivedData()
        {
            while(true)
            {
                await Task.Delay(1000 * Timeout);
                BytesReceivedDuringLastTimeout = Volatile.Read(ref BytesReceivedDuringTimeoutTmp);
                Volatile.Write(ref BytesReceivedDuringTimeoutTmp, 0);
                TotalDataGB += ((double)BytesReceivedDuringLastTimeout) / 1000000000.0;
            }
        }

        public double GetTotalDataReceivedGB()
        {
            return TotalDataGB;
        }
    }
}