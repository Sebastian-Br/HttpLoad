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

System.Threading.Thread.Sleep(3000);
BandwithMgr bandwithMgr = new(10);
List<Task> tasks = new();
int taskCount = 4;
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