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
     Method |      Median |    StdDev |        Mean |  StdError |    StdDev |       Op/s |         Min |          Q1 |      Median |          Q3 |         Max |
----------- |------------ |---------- |------------ |---------- |---------- |----------- |------------ |------------ |------------ |------------ |------------ |
 Benchmark1 | 180.4251 ns | 4.1294 ns | 180.4400 ns | 0.9234 ns | 4.1294 ns | 5542007.02 | 174.5503 ns | 177.6286 ns | 180.4251 ns | 182.5334 ns | 190.9792 ns |
 Benchmark2 | 178.7267 ns | 6.1670 ns | 180.6081 ns | 1.3148 ns | 6.1670 ns |  5536849.8 | 174.0821 ns | 177.0992 ns | 178.7267 ns | 182.1962 ns | 198.1308 ns |
