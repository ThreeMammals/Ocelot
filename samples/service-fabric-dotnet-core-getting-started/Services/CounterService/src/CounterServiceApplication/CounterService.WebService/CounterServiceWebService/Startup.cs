// // ------------------------------------------------------------
// //  Copyright (c) Microsoft Corporation.  All rights reserved.
// //  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// // ------------------------------------------------------------

// namespace CounterServiceApp
// {
//     using Microsoft.AspNetCore.Builder;
//     using Microsoft.AspNetCore.Hosting;
//     using Microsoft.Extensions.Configuration;
//     using Microsoft.Extensions.DependencyInjection;
//     using System.Collections.Generic;
//     using System.Globalization;
//     using System.IO;
//     using System.Text;
//     using System.Threading.Tasks;
//     using Microsoft.ServiceFabric.Services;
//     using Microsoft.ServiceFabric.Services.Client;
// 	using System;
//     using Ocelot.DependencyInjection;
//     using Ocelot.Middleware;

//     public class Startup 
//     {
//         /// <summary>
//         /// OWIN configuration
//         /// </summary>
//         public Startup(IHostingEnvironment env)
//         {
//             var builder = new ConfigurationBuilder()
//                 .SetBasePath(env.ContentRootPath)
//                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
//                 .AddJsonFile("configuration.json")
//                 .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
//                 .AddEnvironmentVariables();
                
//             Configuration = builder.Build();
//         }

//         public IConfigurationRoot Configuration { get; }

//         // This method gets called by the runtime. Use this method to add services to the container.
//         public void ConfigureServices(IServiceCollection services)
//         {
//             services.AddOcelot();
//         }

//         /// <summary>
//         /// Configures the app builder using Web API.
//         /// </summary>
//         public void Configure(IApplicationBuilder app)
//         {
//             app.UseOcelot().Wait();
//         }
//     }
// }

