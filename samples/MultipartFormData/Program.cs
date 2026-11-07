using Ocelot.Samples.MultipartFormData;


await using var gateway = await ApiGateway.New(5555, args);

await using var downstream = DownstreamService.New(5556, args);
await downstream.StartAsync();

try
{
    await gateway.RunAsync();
}
finally
{
    await downstream.StopAsync();
}
