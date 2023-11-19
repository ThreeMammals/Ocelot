using Microsoft.AspNetCore.Http;
using Ocelot.Configuration.File;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.AcceptanceTests;

public class CancelRequestTests : IDisposable
{
    private const int SERVICE_WORK_TIME = 5_000;
    private const int MAX_WAITING_TIME = 60_000;

    private readonly Steps _steps;
    private readonly ServiceHandler _serviceHandler;
    private readonly Notifier _serviceWorkStartedNotifier;
    private readonly Notifier _serviceWorkStoppedNotifier;

    private bool _cancelExceptionThrown;

    public CancelRequestTests()
    {
        _steps = new Steps();
        _serviceHandler = new ServiceHandler();
        _serviceWorkStartedNotifier = new Notifier("service work started notifier");
        _serviceWorkStoppedNotifier = new Notifier("service work finished notifier");
    }

    [Fact]
    public void Should_abort_service_work_when_cancelling_the_request()
    {
        var port = PortFinder.GetRandomPort();

        var configuration = new FileConfiguration
        {
            Routes = new List<FileRoute>
            {
                new()
                {
                    DownstreamPathTemplate = "/",
                    DownstreamHostAndPorts = new List<FileHostAndPort>
                    {
                        new()
                        {
                            Host = "localhost",
                            Port = port,
                        },
                    },
                    DownstreamScheme = "http",
                    UpstreamPathTemplate = "/",
                    UpstreamHttpMethod = new List<string> { "Get" },
                },
            },
        };

        this.Given(x => x.GivenThereIsAServiceRunningOn($"http://localhost:{port}"))
            .And(x => _steps.GivenThereIsAConfiguration(configuration))
            .And(x => _steps.GivenOcelotIsRunning())
            .When(x => _steps.WhenIGetUrlOnTheApiGatewayAndDontWait("/"))
            .And(x => WhenIWaitForNotification(_serviceWorkStartedNotifier))
            .And(x => _steps.WhenICancelTheRequest())
            .And(x => WhenIWaitForNotification(_serviceWorkStoppedNotifier))
            .Then(x => x.ThenOcelotClientRequestIsCanceled())
            .BDDfy();
    }

    private void GivenThereIsAServiceRunningOn(string baseUrl)
    {
        _serviceHandler.GivenThereIsAServiceRunningOn(baseUrl, async context =>
        {
            try
            {
                var response = string.Empty;

                _serviceWorkStartedNotifier.NotificationSent = true;
                await Task.Delay(SERVICE_WORK_TIME, context.RequestAborted);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(response);
            }
            catch (TaskCanceledException)
            {
                _cancelExceptionThrown = true;
            }
            finally
            {
                _serviceWorkStoppedNotifier.NotificationSent = true;
            }
        });
    }

    private static async Task WhenIWaitForNotification(Notifier notifier)
    {
        int waitingTime = 0;
        while (!notifier.NotificationSent)
        {
            var waitingInterval = 50;
            await Task.Delay(waitingInterval);
            waitingTime += waitingInterval;

            if (waitingTime > MAX_WAITING_TIME)
            {
                throw new TimeoutException(notifier.Name + $" did not sent notification within {MAX_WAITING_TIME / 1000} second(s).");
            }
        }
    }

    private void ThenOcelotClientRequestIsCanceled()
    {
        _serviceWorkStartedNotifier.NotificationSent.ShouldBeTrue();
        _serviceWorkStoppedNotifier.NotificationSent.ShouldBeTrue();

        _cancelExceptionThrown.ShouldBeTrue();
    }

    public void Dispose()
    {
        _serviceHandler?.Dispose();
        _steps.Dispose();
        GC.SuppressFinalize(this);
    }

    class Notifier
    {
        public Notifier(string name) => Name = name;

        public bool NotificationSent { get; set; }
        public string Name { get; set; }
    }
}
