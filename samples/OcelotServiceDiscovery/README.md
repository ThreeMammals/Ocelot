#Example how to use custom service discovery

This sample constains a simple setup using a custom service discovery provider.

##Instructions
    
1. Get Downstream service running

    ```
    cd ./DownstreamService/
    dotnet run
    ```

    Leave the service running

2. Get API Gateway running

    ```
    cd ./ApiGateway/
    dotnet run
    ```

    Leave the service running

3. Make a http request to http://localhost:5000/Categories you should get the following response

    ```json
    ["category1","category2"]
    ```
