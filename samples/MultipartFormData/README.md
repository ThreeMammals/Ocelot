# Ocelot Multipart Form Data
> A minimal browser form sample for forwarding `multipart/form-data` uploads through Ocelot.

This sample demonstrates issue [#714](https://github.com/ThreeMammals/Ocelot/issues/714) with a real HTML page. The browser posts a text field and a file to the gateway, Ocelot forwards the request to the downstream service, and the downstream service reads the form and file from `IFormCollection`.

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

### 3. Upload a file
Open http://localhost:5567/ in a browser, choose a text file, and submit the form. The response should confirm the text field, file field name, original filename, content type, length, file contents, and the multipart request content type received by the downstream service.
