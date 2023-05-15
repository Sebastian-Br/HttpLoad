/*
 The HttpLoad tool puts a small Http-load, duh, on a list of hosts.
First, initializes the default Client, then creates the defined (appsettings) amount of LoadCycles().
A LoadCycle is a loop which continues to spawn Http-Get requests.
The target hosts are specified in the appsettings.json file.
This app adapts to the responsiveness of a host by choosing to skip requests, and is intended
to be run in the background at a low to moderate bandwith setting such as not to interfere with
whatever the device is used for.
The bandwith can currently only be set by adjusting the amount of LoadCycles in the appsettings file.

The 'MaxBandwith_GigabytePerHour' setting is a WiP.
*/

using HttpLoad;
using System.Text.Json;
using NLog;
using System.Reflection;

Logger logger = NLog.LogManager.GetCurrentClassLogger();
Configuration configuration = new();

string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
Directory.SetCurrentDirectory(assemblyPath);
Console.WriteLine("assemblyPath= " + assemblyPath);
if (!File.Exists("appsettings.json"))
{
    Console.WriteLine("appsettings.json not found!");
    Console.ReadLine();
}
else
{
    Console.WriteLine("Reading configuration from appsettings.json..");
}

configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText("appsettings.json"));

HttpClient client = new HttpClient();   // default Client to be used by all of the Host-classes
Console.WriteLine("client.DefaultRequestVersion: " + client.DefaultRequestVersion);
client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
client.DefaultRequestHeaders.Add("Accept-Encoding", configuration.AcceptHeader);
client.DefaultRequestHeaders.Add("Connection", "keep-alive");
client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:99.0) Gecko/20100101 Firefox/99.0");
client.Timeout = new TimeSpan(0, 0, 3);
HttpClient ShortTimeoutClient = new HttpClient();
ShortTimeoutClient.Timeout = new TimeSpan(0, 0, 0, 0, 1500); //1.5s
ShortTimeoutClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
ShortTimeoutClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
ShortTimeoutClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.41 Safari/537.36");
Console.WriteLine("Hosts: " + configuration.Hosts.Count);
foreach(Host w in configuration.Hosts)
{
    w.HttpClient = client;
    w.ShortTimeoutClient = ShortTimeoutClient;
    Console.WriteLine("Host: " + w.BaseUrl);
}

int taskCount = configuration.TaskCount;
if(args != null)
{
    Console.WriteLine("args.Count(): " + args.Count());
    if(args.Count() > 0)
    {
        try
        {
            Console.WriteLine("Trying to parse: " + args[0]);
            taskCount = int.Parse(args[0]);
        }
        catch(Exception ex)
        {
        }
    }
}
Console.WriteLine("----------------------------------------");
Console.WriteLine("Welcome to the Http-Load tool! This tool works by going through a list of URLs & URIs specified in the appsettings.json.");
Console.WriteLine("\nSome target servers may refuse a connection when requests are sent too quickly - i.e. requests time out.\nTo stop this from interfering with the load-cycle (task), the tool may choose to skip that URL.");
Console.WriteLine("A single task is equivalent to ~115 kB/s (gzip) & ? kB/s (uncompressed) averaged over a 10s interval (may be much higher at a short interval)"); // Todo: give estimate 
Console.WriteLine("If you want to use the machine to work, watch streams, play games, etc., you should set the task-count accordingly.");
Console.WriteLine("Note that it takes time for the tool to adapt to the connectivity of remote hosts; i.e. it has to collect data as to which URLs are unreachable.");
Console.WriteLine("This means that the datarate will continuously increase.");
Console.WriteLine("Some hosts always block requests sent by this tool; I haven't yet figured out why.");

while (taskCount < 1) // todo: validate startup behavior when started with a command line argument
{
    Console.Write("\nPlease enter the amount of tasks (load-cycles) to be created: ");
    try
    {
        string str_taskCount = Console.ReadLine();
        taskCount = int.Parse(str_taskCount);
    }
    catch (Exception ex)
    {}

    Console.WriteLine("\nSetting Task-Count to: " + taskCount);
}

BandwithMgr bandwithMgr = new(20); // The timeout is set to 20 seconds here
List<Task> tasks = new();
int taskRunTimeSeconds = configuration.Hosts.Count / 2; // approximate time for a task to complete 1 for loop

int waitTimeInBetweenHttpGet_Ms = 0;
double dataRateGBperHour = 0;
DateTime PreTasksStartedTime = DateTime.Now;
if(configuration.MaxBandwith_GigabytePerHour > 0)
{
    tasks.Add(Task.Run(() => AdjustDataRate()));
}

for (int i = 0; i < taskCount; i++) // starts the tasks with a short delay inbetween
{
    tasks.Add(Task.Run(() => LoadCycle()));
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("TASK " + (i+1) + "/" + taskCount + " STARTED!");
    Console.ForegroundColor = ConsoleColor.White;
    System.Threading.Thread.Sleep(1000*taskRunTimeSeconds/taskCount);
}

double hoursSinceTasksStarted = 0.0;
double totalGBreceived = 0.0;
double currentDataRateKiloBytes = 0.0;
DateTime TasksStartedTime = DateTime.Now;

while (true) // displays datarate statistics every 10 seconds after all tasks have been started
{
    try
    {
        hoursSinceTasksStarted = (DateTime.Now - TasksStartedTime).TotalHours;
        currentDataRateKiloBytes = (bandwithMgr.GetDataRateBytesPerSecond()) / (1000.0);
        totalGBreceived = bandwithMgr.GetTotalDataReceivedGB();
        dataRateGBperHour = totalGBreceived / hoursSinceTasksStarted;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("#########\nDATARATE (kB/s): " + currentDataRateKiloBytes);
        Console.WriteLine("Total Data Received: " + totalGBreceived + " [GB] @~ " + dataRateGBperHour + " [GB/h]" +
            "\n#########");
        Console.ForegroundColor = ConsoleColor.White;
        await Task.Delay(10000);
    }
    catch (Exception ex)
    {
        logger.Error(ex);
    }
}

/// <summary>
/// This task runs the loop that GETs one URL+uri after another.
/// Multiple tasks can be started to amp up the datarate.
/// Some sites may block requests when requests are sent too quickly; thus, tasks should be started
/// with a slight delay inbetween (implemented above).
/// </summary>
async Task LoadCycle()
{
    while(true)
    {
        foreach (Host page in configuration.Hosts)
        {
            try
            {
                await Task.Delay(1 + waitTimeInBetweenHttpGet_Ms);
                int receivedBytes = await page.LoadURI();
                if (receivedBytes > 0)
                {
                    bandwithMgr.AddReceivedBytes(receivedBytes);
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}

async Task AdjustDataRate()
{
    int max_AdjustDataRate_TaskDelay_Ms = 10000;
    int AdjustDataRate_TaskDelay_Ms = 1000;
    int min_AdjustDataRate_TaskDelay_Ms = 300;

    double fastUpdatingDataRateGBperHour = 0.0;
    while (true)
    {
        fastUpdatingDataRateGBperHour = ((bandwithMgr.GetDataRateBytesPerSecond()) * 3600.0) / 1000000000.0;
        if (fastUpdatingDataRateGBperHour > 1.05 * configuration.MaxBandwith_GigabytePerHour)
        {
            waitTimeInBetweenHttpGet_Ms += 5;
            AdjustDataRate_TaskDelay_Ms = SubstractWithLowerLimit(AdjustDataRate_TaskDelay_Ms, 100, min_AdjustDataRate_TaskDelay_Ms);
            Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine("Increased WaitTime to " + waitTimeInBetweenHttpGet_Ms + " [ms]");
            Console.ForegroundColor = ConsoleColor.White;
        }
        else if (fastUpdatingDataRateGBperHour < 0.85 * configuration.MaxBandwith_GigabytePerHour && waitTimeInBetweenHttpGet_Ms > 0)
        {
            waitTimeInBetweenHttpGet_Ms--;
            AdjustDataRate_TaskDelay_Ms = SubstractWithLowerLimit(AdjustDataRate_TaskDelay_Ms, 100, min_AdjustDataRate_TaskDelay_Ms);
            Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine("Decreased WaitTime to " + waitTimeInBetweenHttpGet_Ms + " [ms]");
            Console.ForegroundColor = ConsoleColor.White;
        }
        else
        {
            if(DateTime.Now.Subtract(PreTasksStartedTime).TotalMinutes > 1.0)
            AdjustDataRate_TaskDelay_Ms = AddWithUpperLimit(AdjustDataRate_TaskDelay_Ms, 100, max_AdjustDataRate_TaskDelay_Ms);
        }

        await Task.Delay(AdjustDataRate_TaskDelay_Ms);
    }
}


int AddWithUpperLimit(int n, int addAmount, int upperLimit)
{
    int sum = n + addAmount;
    if (sum > upperLimit)
    {
        return upperLimit;
    }
    else
    {
        return sum;
    }
}

int SubstractWithLowerLimit(int n, int substractAmount, int lowerLimit)
{
    int sum = n - substractAmount;
    if (sum < lowerLimit)
    {
        return lowerLimit;
    }
    else
    {
        return sum;
    }
}