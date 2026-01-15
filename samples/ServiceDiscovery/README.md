# Ocelot Service Discovery Custom Provider
> An example how to build custom service discovery in Ocelot.<br/>
> **Documentation**: [Service Discovery](../../docs/features/servicediscovery.rst) > [Custom Providers](../../docs/features/servicediscovery.rst#custom-providers)

This sample constains a basic setup using a custom service discovery provider.<br/>

## Instructions
    
### 1. Run Downstream Service app
```shell
cd ./DownstreamService/
dotnet run
```
Leave the service running.

### 2. Run API Gateway app
```shell
cd ./ApiGateway/
dotnet run
```
Leave the gateway running.

### 3. Make a HTTP request
To the URL: http://localhost:5000/categories<br/>
You should get the following response:
```json
{
  [ "category1", "category2" ]
}
```
