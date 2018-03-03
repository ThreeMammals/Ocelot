// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OcelotApplicationApiGateway
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Diagnostics.Tracing;
    using System.Fabric;
    using System.Fabric.Common;
    using System.Fabric.Common.Tracing;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// ServiceEventListener is a class which listens to the eventsources registered and redirects the traces to a file
    /// Note that this class serves as a template to EventListener class and redirects the logs to /tmp/{appnameyyyyMMddHHmmssffff}.
    /// You can extend the functionality by writing your code to implement rolling logs for the logs written through this class.
    /// You can also write your custom listener class and handle the registered evestsources accordingly. 
    /// </summary>
    internal class ServiceEventListener : EventListener
    {
        private string fileName;
        private string filepath = Path.GetTempPath();

        public ServiceEventListener(string appName)
        {
            this.fileName = appName + DateTime.Now.ToString("yyyyMMddHHmmssffff");
        }

        /// <summary>
        /// We override this method to get a callback on every event we subscribed to with EnableEvents
        /// </summary>
        /// <param name="eventData">The event arguments that describe the event.</param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            using (StreamWriter writer = new StreamWriter( new FileStream(filepath + fileName, FileMode.Append)))
            {
                // report all event information
                writer.Write(" {0} ",  Write(eventData.Task.ToString(),
                eventData.EventName,
                eventData.EventId.ToString(),
                eventData.Level));

                if (eventData.Message != null)
                {
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture, eventData.Message, eventData.Payload.ToArray()));
                }
            }
        }

        private static String Write(string taskName, string eventName, string id, EventLevel level)
        {
            StringBuilder output = new StringBuilder();

            DateTime now = DateTime.UtcNow;
            output.Append(now.ToString("yyyy/MM/dd-HH:mm:ss.fff", CultureInfo.InvariantCulture));
            output.Append(',');
            output.Append(ConvertLevelToString(level));
            output.Append(',');
            output.Append(taskName);

            if (!string.IsNullOrEmpty(eventName))
            {
                output.Append('.');
                output.Append(eventName);
            }

            if (!string.IsNullOrEmpty(id))
            {
                output.Append('@');
                output.Append(id);
            }

            output.Append(',');
            return output.ToString();
        }

        private static string ConvertLevelToString(EventLevel level)
        {
            switch (level)
            {
                case EventLevel.Informational:
                    return "Info";
                default:
                    return level.ToString();
            }
        }
    }
}