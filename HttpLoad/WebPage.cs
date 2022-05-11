using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace HttpLoad
{
    internal class WebPage
    {
        public WebPage()
        {
            BaseUrl = "";
            URIs = new();
            URIindex = 0;
            TotalAttempts = 0;
            RecentFailures = 0;
            RecentFailuresMax = 50;
            RNG = new();
            PingsTimeout = 500;
            PingCount = 10;
            RecentPingFailures = 0;
            ShortTimeoutClient = new();
            ShortTimeoutClient.Timeout = new TimeSpan(0, 0, 0, 0, 1500); //1.5s
            ShortTimeoutClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            ShortTimeoutClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            ShortTimeoutClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.41 Safari/537.36");
        }
        public string BaseUrl { get; set; }

        public List<string> URIs { get; set; }

        public HttpClient HttpClient { get; set; }

        private int URIindex { get; set; }

        private int RecentFailures { get; set; }

        private int RecentFailuresMax { get; set; }

        private int TotalAttempts { get; set; }

        private Random RNG { get; set; }

        private int PingsTimeout    { get; set; }

        private int PingCount { get; set; }

        private int RecentPingFailures { get; set; }

        private HttpClient ShortTimeoutClient { get; set; }

        public async Task<BandwithEvent> LoadURI()
        {
            string uri = "";
            try
            {
                TotalAttempts++;
                if (URIindex >= URIs.Count)
                {
                    URIindex = 0;
                }
                uri = URIs[URIindex];
                URIindex++;


                if (RecentFailures > RecentFailuresMax)
                {
                    RecentFailures = RecentFailuresMax;
                }
                else if (RecentFailures < 0)
                {
                    RecentFailures = 0;
                }

                if (RecentPingFailures > 10)
                {
                    RecentPingFailures = 10; // max 10
                }
                else if (RecentPingFailures < 0)
                {
                    RecentPingFailures = 0;
                }

                if (RecentFailures > 0) // check if to SKIP this Http-Get.
                {
                    //Console.WriteLine("TotalAttempts: " + TotalAttempts + " Recent Failures: " + RecentFailures + "/" + RecentFailuresMax + ", RecentPingFailures: " + RecentPingFailures + "/10 - " + BaseUrl);
                    if ((TotalAttempts+ RNG.Next(0,31)) % (1 + RecentFailures) != 0) // for 1 recent failure, only every 2nd request will be sent.
                    {
                        if(((TotalAttempts + RNG.Next(0, 10)) % (1+RecentPingFailures)) == 0)
                        {
                            int successfulPings = TrySendPings();
                            float pingSuccessRate = (float) successfulPings / (float) PingCount;
                            if(pingSuccessRate > 0.75)
                            {
                                RecentPingFailures--;
                                PingCount++;
                            }
                            else
                            {
                                RecentPingFailures++;
                                PingCount--;
                            }

                            int loadBytes = (successfulPings) * 40 + 8;
                            if(successfulPings > 1)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("PINGED " + BaseUrl + " for " + loadBytes + " [Bytes] total (" + successfulPings + " pings)");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            
                            return new BandwithEvent() { BytesReceived = loadBytes, EventTime = DateTime.Now };
                        }
                        else // Skip HTTP GET and PING
                        {
                            Console.ForegroundColor = ConsoleColor.Blue;
                            Console.WriteLine("SKIPPING " + BaseUrl);
                            Console.ForegroundColor = ConsoleColor.White;
                            return null;
                        }
                    }
                }
                
                HttpResponseMessage response;

                if (RecentFailures > 5)
                {
                    response = await ShortTimeoutClient.GetAsync(BaseUrl + uri);
                }
                else
                {
                    response = await HttpClient.GetAsync(BaseUrl + uri);
                }
                string responseBody;
                using (var sr = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.GetEncoding("iso-8859-1")))
                {
                    responseBody = sr.ReadToEnd();
                }

                int responseBytesCount = responseBody.Length + 128;
                //Console.WriteLine("Accessed: " + BaseUrl + uri + " | Bytes: " + responseBytesCount);
                if (responseBody.Length < 500)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Reply too short (" + responseBody.Length + ") from " + BaseUrl + uri + ":\n" + responseBody);
                    Console.ForegroundColor = ConsoleColor.White;
                    RecentFailures++;
                }
                else
                {
                    if(RecentFailures > 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("***Success!*** RecentFailures from " + RecentFailures + " to " + (RecentFailures - 1) + " @ " + BaseUrl + " w/ " + responseBytesCount + "[Bytes]");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    RecentFailures--;
                }

                return new BandwithEvent() { BytesReceived = responseBytesCount, EventTime = DateTime.Now };
            }
            catch (System.Threading.Tasks.TaskCanceledException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("HTTP-TIMEOUT(CANCELED): " + BaseUrl /*+ "\n" + e*/);
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("HTTP-TIMEOUT: " + BaseUrl);
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("HTTP-EXCEPTION: " + e + BaseUrl + uri);
                Console.ForegroundColor = ConsoleColor.White;
            }

            RecentFailures++;
            Console.WriteLine("RecentFailures is now " + RecentFailures);
            return null;
        }

        private int TrySendPings()
        {
            int receivedPings = 0;
            try
            {
                List<Task<bool>> pingTaskList = new();
                for(int i = 0; i < PingCount; i++)
                {
                    pingTaskList.Add(TryPingAsync());
                }

                int timeElapsed = 0;

                while(timeElapsed < PingsTimeout)
                {
                    if(pingTaskList.Count > 0)
                    {
                        foreach (Task<bool> pingTask in pingTaskList)
                        {
                            if (pingTask.IsCompleted)
                            {
                                if(pingTask.Result == true)
                                {
                                    receivedPings++;
                                }
                                pingTask.Dispose();
                                pingTaskList.Remove(pingTask);
                            }
                        }
                    }
                    else
                    {
                        return receivedPings;
                    }

                    System.Threading.Thread.Sleep(100);
                    timeElapsed += 100;
                }

                foreach(Task<bool> pingTask in pingTaskList)
                {
                    if (pingTask.IsCompleted)
                    {
                        if (pingTask.Result == true)
                        {
                            receivedPings++;
                        }
                        pingTask.Dispose();
                        pingTaskList.Remove(pingTask);
                    };
                }

                return receivedPings;
            }
            catch (Exception e)
            {

            }
            return receivedPings;
        }

        private async Task<bool> TryPingAsync()
        {
            try
            {
                Ping pingSender = new Ping();

                // Create a buffer of 32 bytes of data to be transmitted.
                string data = "5top5endingRussians2Cmertb..<3<3";
                byte[] buffer = Encoding.ASCII.GetBytes(data);

                // Wait 500 m seconds for a reply.
                int timeout = 500;

                PingOptions options = new PingOptions(64, true);
                string pingUrl = BaseUrl.Replace("/", "");
                pingUrl = pingUrl.Replace("https", "");
                pingUrl = pingUrl.Replace("http", "");
                PingReply reply = pingSender.Send(pingUrl, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                    return true;
                }
                else
                {
                    Console.WriteLine(reply.Status);
                    return false;
                }
            }
            catch (Exception e)
            {

            }

            return false;
        }
    }
}