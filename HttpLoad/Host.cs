using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace HttpLoad
{
    internal class Host
    {
        /// <summary>
        /// A host/request target
        /// </summary>
        public Host()
        {
            BaseUrl = "";
            URIs = new();
            URIindex = 0;
            TotalRequestAttempts = 0;
            RecentConnectionFailures = 0;
            RecentConnectionFailuresMax = 50;
            RNG = new();
        }

        /// <summary>
        /// A URL like "https://lenta.ru/"
        /// </summary>
        public string BaseUrl { get; set; }

        /// <summary>
        /// A list of URIs such as "specprojects/"
        /// </summary>
        public List<string> URIs { get; set; }

        /// <summary>
        /// The regular http-client with 
        /// </summary>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// Used to select the URI to load
        /// </summary>
        private int URIindex { get; set; }

        /// <summary>
        /// Counts the number of requests that have recently failed. The tool may choose to randomly skip a request if there is a high chance to failure.
        /// The chance for skipping a request increases as the number of recent failures increases.
        /// </summary>
        private int RecentConnectionFailures { get; set; }

        /// <summary>
        /// The RecentConnectionFailures variable will max out at this value
        /// </summary>
        private int RecentConnectionFailuresMax { get; set; }

        private int TotalRequestAttempts { get; set; }

        private int TotalSuccessfulRequests { get; set; }

        private Random RNG { get; set; }

        public HttpClient ShortTimeoutClient { get; set; }

        /// <summary>
        /// Loads the URL+URI if possible.
        /// </summary>
        /// <returns>A BandwithEvent object detailling the amount of data transferred.</returns>
        public async Task<int> LoadURI()
        {
            string uri = "";
            try
            {
                TotalRequestAttempts++;
                if (URIindex >= URIs.Count)
                {
                    URIindex = 0;
                }
                uri = URIs[URIindex]; // selects URI here
                URIindex++;


                if (RecentConnectionFailures > RecentConnectionFailuresMax) // makes sure RecentConnectionFailures stays >= 0 and < RecentConnectionFailuresMax
                {
                    RecentConnectionFailures = RecentConnectionFailuresMax;
                }
                else if (RecentConnectionFailures < 0)
                {
                    RecentConnectionFailures = 0;
                }


                if (RecentConnectionFailures > 0) // check if to SKIP this Http-Get.
                {
                    if ((TotalRequestAttempts + RNG.Next(0,31)) % (1 + RecentConnectionFailures) != 0) // for 1 recent failure, only every 2nd request will be sent.
                    {
                        /*Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("SKIPPING " + BaseUrl);
                        Console.ForegroundColor = ConsoleColor.White;*/
                        return -1;
                    }
                }
                
                HttpResponseMessage response; // firing the request with either the normal or short timeout client
                if (RecentConnectionFailures > 5)
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

                int responseBytesCount = responseBody.Length + 128; // add some bytes to account for the headers/etc

                if (responseBody.Length < 500) // <500 byte responses are scored as connection failures
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Reply too short (" + responseBody.Length + ") from " + BaseUrl + uri + ":\n" + responseBody);
                    Console.ForegroundColor = ConsoleColor.White;
                    RecentConnectionFailures++;
                }
                else
                {
                    if(RecentConnectionFailures > 1) // highlights the text when a URL was reachable again when there are RecentConnectionFailures
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("RecentFailures from " + RecentConnectionFailures + " to " + (RecentConnectionFailures - 1) + " @ " + BaseUrl + " w/ " + responseBytesCount + "[Bytes]");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    RecentConnectionFailures--;
                }

                TotalSuccessfulRequests++;
                return responseBytesCount;
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

            RecentConnectionFailures++;
            Console.WriteLine("RecentFailures is now " + RecentConnectionFailures);
            return -2;
        }

        public double GetTotalSuccessPercentage()
        {
            if (TotalSuccessfulRequests == 0)
            {
                return 0.0;
            }
            else
            {
                // return TotalSuccessfulRequests / TotalSuccessfulRequests; <- a legendary IntelliSense suggestion!
                double dSuccessPercentage = 100* (double) TotalRequestAttempts / (double) TotalSuccessfulRequests;
                return dSuccessPercentage;
            }
        }
    }
}