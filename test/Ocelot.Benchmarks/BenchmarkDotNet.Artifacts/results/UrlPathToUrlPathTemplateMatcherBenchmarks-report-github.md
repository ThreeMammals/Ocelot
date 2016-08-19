```ini

Host Process Environment Information:
BenchmarkDotNet=v0.9.8.0
OS=OSX
Processor=?, ProcessorCount=4
Frequency=1000000000 ticks, Resolution=1.0000 ns, Timer=UNKNOWN
CLR=CORE, Arch=64-bit ? [RyuJIT]
GC=Concurrent Workstation
JitModules=?
dotnet cli version: 1.0.0-preview2-003121

Type=UrlPathToUrlPathTemplateMatcherBenchmarks  Mode=Throughput  Toolchain=Core  
GarbageCollection=Concurrent Workstation  

```
     Method |      Median |     StdDev |        Mean |  StdError |     StdDev |       Op/s |         Min |          Q1 |      Median |          Q3 |         Max |
----------- |------------ |----------- |------------ |---------- |----------- |----------- |------------ |------------ |------------ |------------ |------------ |
 Benchmark1 | 184.4215 ns |  5.1537 ns | 185.3322 ns | 1.1524 ns |  5.1537 ns | 5395716.74 | 178.2386 ns | 181.8117 ns | 184.4215 ns | 188.2762 ns | 196.7310 ns |
 Benchmark2 | 186.1899 ns | 35.7006 ns | 202.4315 ns | 3.9425 ns | 35.7006 ns | 4939943.34 | 176.9750 ns | 182.9672 ns | 186.1899 ns | 205.8946 ns | 369.0701 ns |
