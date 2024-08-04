// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1132:Do not combine fields", Justification = "Has no much sense in test projects", Scope = "type", Target = "~T:Ocelot.AcceptanceTests.ServiceDiscovery.KubernetesServiceDiscoveryTests")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:Closing brace should be followed by blank line", Justification = "Has no much sense in test projects", Scope = "member", Target = "~M:Ocelot.AcceptanceTests.ServiceDiscovery.KubernetesServiceDiscoveryTests.GivenThereIsAFakeKubernetesProvider(System.String,System.String,KubeClient.Models.EndpointsV1,System.Boolean,System.Int32,System.Int32)")]
