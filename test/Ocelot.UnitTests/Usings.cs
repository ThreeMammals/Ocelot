// Default Microsoft.NET.Sdk namespaces
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;

// Project extra global namespaces
global using Moq;
global using Ocelot;
global using Ocelot.Testing;
global using Shouldly;
global using System.Net;
global using Xunit;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:DoNotUseRegions", Justification = "Reviewed.")]
[assembly: SuppressMessage("Usage", "xUnit1004:Test methods should not be skipped", Justification = "Reviewed.")]

internal class Usings { }
