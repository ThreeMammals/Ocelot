using Ocelot.ManualTest.Actions;
using System.Reflection;

// When launched by IIS Express, skip menu and run gateway-only
if (Issue941.IsGatewayMode())
{
    await Issue941.RunAsGatewayAsync();
    return;
}

// Support non-interactive launch: dotnet run -- 4
if (args.Length > 0)
{
    await RunActionAsync(args[0], args);
    return;
}

var nl = Environment.NewLine;
var programName = Assembly.GetExecutingAssembly().GetName()?.Name?.Replace(".", " ") ?? "?";
do
{
    Console.Clear();
    Console.WriteLine($"{nl}Welcome to {programName} app!");
    Console.Write(@"What are you going to do?
  1. Run Ocelot with basic setup (default)
  2. Run Ocelot manual tests
  3. Run Ocelot with SSE setup
  4. Run Ocelot with SSE setup (IIS Express)
So, press 1, 2, 3 or 4 > ");
    
    ConsoleKeyInfo info = Console.ReadKey(true);
    var choice = info.KeyChar.ToString();
    
    if (info.Key != ConsoleKey.Enter)
    {
        Console.WriteLine(choice);
    }

    await RunActionAsync(choice, args);
}
while (!Quit());

async Task RunActionAsync(string choice, string[] args)
{
    switch (choice)
    {
        case "2":
            ManualTests.Run(args);
            break;
        case "3":
            await Issue941.RunAsync();
            break;
        case "4":
            await Issue941.RunWithIisExpressAsync();
            break;
        case "5":
            var method = typeof(Issue941).GetMethod("StartDownstream", BindingFlags.NonPublic | BindingFlags.Static);
            await (Task)method!.Invoke(null, null)!;
            await Task.Delay(-1);
            break;
        case "1":
        default:
            await Basic.RunAsync(args);
            break;
    }
}

bool Quit()
{
    Console.WriteLine(nl + "Enter Ctrl+Q to Quit, Ctrl+E to Exit, Ctrl+L to Clear the log");
    Console.Write("Or press any key to restart... ");
    ConsoleKeyInfo info = Console.ReadKey(true);
    if (info.Modifiers == ConsoleModifiers.Control)
    {
        if (info.Key == ConsoleKey.Q)
        {
            Console.WriteLine("Quitting...");
            Environment.ExitCode = 0;
            return true;
        }
        else if (info.Key == ConsoleKey.E)
        {
            Console.WriteLine("Exitting...");
            Environment.Exit(1);
        }
        else if (info.Key == ConsoleKey.L)
        {
            Console.WriteLine();
            Console.Clear();
        }
    }

    Console.WriteLine();
    return false;
}
