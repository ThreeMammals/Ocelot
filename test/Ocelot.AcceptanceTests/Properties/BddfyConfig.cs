using System.Collections.Concurrent;
using TestStack.BDDfy.Configuration;

namespace Ocelot.AcceptanceTests.Properties;

public static class BddfyConfig
{
    public static void Configure()
    {
        //// Configurator.Processors.ConsoleReport.RunsOn(story => story.Result != Result.Passed);
        //Configurator.Processors.ConsoleReport.Disable();
        //Configurator.Processors.Add(() => new BddfyProcessor());

        ////Configurator.BatchProcessors.Add(new BddfyBatchProcessingReporter());
        //Configurator.BatchProcessors.HtmlReport.Disable();
    }
}

public class BddfyProcessor : IProcessor
{
    private static readonly ConcurrentDictionary<string, Scenario> Cache = new();
    public ProcessType ProcessType => ProcessType.Report;
    public void Process(Story story)
    {
        //Console.WriteLine($"{story.Result} Story: {story.Namespace} | Total Scenarios: {story.Scenarios.Count()}");
        foreach (var scenario in story.Scenarios)
        {
            if (Cache.TryAdd(scenario.Id, scenario))
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

public class BddfyBatchProcessingReporter : IBatchProcessor
{
    private static int totalStories;
    private static int totalScenarios;
    private static Result final = Result.NotExecuted;

    public static void Process(Story story)
    {
        //foreach (var scenario in story.Scenarios)
        //{
        //    //Console.WriteLine($"Scenario: {scenario.Title} - Status: {scenario.Result}");
        //    totalScenarios++;
        //}
        totalScenarios += story.Scenarios.Count();
        totalStories++;
        final = (Result)Math.Max((int)story.Result, (int)final);
    }

    public void Process(IEnumerable<Story> stories)
    {
        var list = stories.ToList();
        list.ForEach(Process);

        Console.WriteLine("Warning: Per-scenario logging has been disabled!");
        Console.WriteLine($"The {nameof(BddfyBatchProcessingReporter)} has processed total {totalStories} stories with total {totalScenarios} scenarios.");
        Console.WriteLine($"Final result: {final}");
        Console.WriteLine();
    }
}
