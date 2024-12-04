using System.Collections.Concurrent;
using TestStack.BDDfy.Configuration;

namespace Ocelot.AcceptanceTests;

public class BddfyConfig
{
    public BddfyConfig()
    {
        Configurator.BatchProcessors.HtmlReport.Disable();
        Configurator.BatchProcessors.Add(new BddfyTitleAndStatusReporter());

        // Configurator.Processors.ConsoleReport.RunsOn(story => story.Result != Result.Passed);
        Configurator.Processors.ConsoleReport.Disable();
        Configurator.Processors.Add(() => new BddfyTitleAndStatusProcessor());
    }
}

public class BddfyTitleAndStatusProcessor : IProcessor
{
    private static ConcurrentDictionary<string, Scenario> cache = new();
    public ProcessType ProcessType => ProcessType.Report;
    public void Process(Story story)
    {
        //Console.WriteLine($"{story.Result} Story: {story.Namespace} | Total Scenarios: {story.Scenarios.Count()}");
        foreach (var scenario in story.Scenarios)
        {
            if (cache.TryAdd(scenario.Id, scenario))
            {
                Console.ForegroundColor = scenario.Result == Result.Passed ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write(scenario.Result);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($" {scenario.Id}: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(scenario.Title);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($", in {scenario.Duration.TotalSeconds} sec");
                Console.ResetColor();
            }
        }
    }
}

public class BddfyTitleAndStatusReporter : IBatchProcessor
{
    public static void Process(Story story)
    {
        foreach (var scenario in story.Scenarios)
        {
            Result status = scenario.Result;
            Console.WriteLine($"Scenario: {scenario.Title} - Status: {status}");
        }
    }

    public void Process(IEnumerable<Story> stories)
    {
        foreach (var scenario in stories)
        {
            Process(scenario);
        }
    }
}
