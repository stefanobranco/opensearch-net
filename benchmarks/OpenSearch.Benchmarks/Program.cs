using BenchmarkDotNet.Running;
using OpenSearch.Benchmarks;

if (args is ["smoke"])
	return await Smoke.RunAsync();

BenchmarkSwitcher.FromAssembly(typeof(ClientBenchmarks).Assembly).Run(args);
return 0;
