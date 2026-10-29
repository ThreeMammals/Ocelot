# Ocelot Multipart Form Data
> A minimal browser form sample for forwarding `multipart/form-data` uploads through Ocelot.

This sample demonstrates the current multipart forwarding behavior related to [#714](https://github.com/ThreeMammals/Ocelot/issues/714) with a real HTML page. The browser posts a text field and a file to the gateway, Ocelot forwards the request to the downstream service, and the downstream service reads the form and file from `IFormCollection`.

The original issue is no longer reproducible for regular routing on the current codebase, so this sample acts as an executable regression scenario for the supported behavior.

## Instructions

### 1. Run the sample
```shell
cd ./ApiGateway/
dotnet run
```
The app starts the downstream service on port 5568 before starting the gateway on port 5567.

### 2. Upload a file
Open http://localhost:5567/ in a browser, choose a text file, and submit the form. The response should confirm the text field, file field name, original filename, content type, length, file contents, and the multipart request content type received by the downstream service.
