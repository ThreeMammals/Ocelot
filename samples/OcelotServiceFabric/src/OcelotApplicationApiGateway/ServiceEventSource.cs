// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OcelotApplicationApiGateway
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Fabric;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// Implements methods for logging service related events.
    /// </summary>
    public class ServiceEventSource : EventSource
    {
        public static ServiceEventSource Current = new ServiceEventSource();

        // Define an instance method for each event you want to record and apply an [Event] attribute to it.
        // The method name is the name of the event.
        // Pass any parameters you want to record with the event (only primitive integer types, DateTime, Guid & string are allowed).
        // Each event method implementation should check whether the event source is enabled, and if it is, call WriteEvent() method to raise the event.
        // The number and types of arguments passed to every event method must exactly match what is passed to WriteEvent().
        // Put [NonEvent] attribute on all methods that do not define an event.
        // For more information see https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource.aspx

        [NonEvent]
        public void Message(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                var finalMessage = string.Format(message, args);
                this.Message(finalMessage);
            }
        }

        private const int MessageEventId = 1;

        [Event(MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
        public void Message(string message)
        {
            if (this.IsEnabled())
            {
                this.WriteEvent(MessageEventId, message);
            }
        }

        private const int ServiceTypeRegisteredEventId = 3;

        [Event(ServiceTypeRegisteredEventId, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}")]
        public void ServiceTypeRegistered(int hostProcessId, string serviceType)
        {
            this.WriteEvent(ServiceTypeRegisteredEventId, hostProcessId, serviceType);
        }

        [NonEvent]
        public void ServiceHostInitializationFailed(Exception e)
        {
            this.ServiceHostInitializationFailed(e.ToString());
        }

        private const int ServiceHostInitializationFailedEventId = 4;

        [Event(ServiceHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Service host initialization failed: {0}")]
        private void ServiceHostInitializationFailed(string exception)
        {
            this.WriteEvent(ServiceHostInitializationFailedEventId, exception);
        }

        [NonEvent]
        public void ServiceWebHostBuilderFailed(Exception e)
        {
            this.ServiceWebHostBuilderFailed(e.ToString());
        }

        private const int ServiceWebHostBuilderFailedEventId = 5;

        [Event(ServiceWebHostBuilderFailedEventId, Level = EventLevel.Error, Message = "Service Owin Web Host Builder Failed: {0}")]
        private void ServiceWebHostBuilderFailed(string exception)
        {
            this.WriteEvent(ServiceWebHostBuilderFailedEventId, exception);
        }

    }
}
