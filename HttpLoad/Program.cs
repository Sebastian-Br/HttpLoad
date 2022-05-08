using HttpLoad;
using System.Text.Json;

Configuration configuration = new();

configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText("appsettings.json"));

HttpClient client = new HttpClient();
Console.WriteLine("client.DefaultRequestVersion: " + client.DefaultRequestVersion);
client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
client.DefaultRequestHeaders.Add("Connection", "keep-alive");
client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:99.0) Gecko/20100101 Firefox/99.0");
client.Timeout = new TimeSpan(0, 0, 3);
Console.WriteLine("Targets: " + configuration.WebPages.Count);
foreach(WebPage w in configuration.WebPages)
{
    w.HttpClient = client;
    Console.WriteLine("Target: " + w.BaseUrl);
}

int taskCount = -1;

Console.WriteLine("----------------------------------------");
Console.WriteLine("Welcome to the Http-Load tool! This tool works by going through a list of URLs & URIs specified in the appsettings.json.");
Console.WriteLine("\nSome target servers may refuse a connection when requests are sent too quickly - i.e. requests time out.\nTo stop this from interfering with the load-cycle (task), the tool may choose to skip that URL.");
Console.WriteLine("A single task is equivalent to ~ 80-100 kB/s averaged over a 10s interval (may be much higher at a short interval)");
Console.WriteLine("If you want to use the machine to work, watch streams, play games, etc., you should set the task-count accordingly.");
Console.WriteLine("Note that it takes time for the tool to adapt to the connectivity of remote hosts; i.e. it has to collect data as to which URLs are unreachable.");
Console.WriteLine("This means that the datarate will continuously increase.");
Console.WriteLine("Some hosts always block requests sent by this tool; I haven't yet figured out why.");

while (taskCount < 1)
{
    Console.Write("\nPlease enter the amount of tasks (load-cycles) to be created: ");
    try
    {
        string str_taskCount = Console.ReadLine();
        taskCount = int.Parse(str_taskCount);
    }
    catch (Exception ex)
    {

    }

    Console.WriteLine("\nSetting Task-Count to: " + taskCount);
}

System.Threading.Thread.Sleep(3000);
BandwithMgr bandwithMgr = new(10);
List<Task> tasks = new();
int taskRunTimeSeconds = configuration.WebPages.Count / 2;
for(int i = 0; i < taskCount; i++)
{
    tasks.Add(Task.Run(() => LoadCycle()));
    Console.ForegroundColor = ConsoleColor.DarkMagenta;
    Console.WriteLine("TASK " + (i+1) + "/" + taskCount + " STARTED!");
    Console.ForegroundColor = ConsoleColor.White;
    System.Threading.Thread.Sleep(1000*taskRunTimeSeconds/taskCount);
}

while (true)
{

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("#########\nDATARATE (kB/s): " + bandwithMgr.GetDataRate() / (1000.0) );
    Console.WriteLine("Total Data Received: " + bandwithMgr.GetTotalDataReceived() + " [GB]" + "\n#########");
    Console.ForegroundColor = ConsoleColor.White;
    Task.Delay(10000).Wait();
}

async Task LoadCycle()
{
    while(true)
    {
        foreach (WebPage page in configuration.WebPages)
        {
            BandwithEvent bandwithEvent = await page.LoadURI();
            if(bandwithEvent != null)
            {
                bandwithMgr.AddEvent(bandwithEvent);
            }
        }
    }
}