echo -------------------------

echo Building Ocelot
dotnet restore src/Ocelot
dotnet build src/Ocelot
dotnet publish src/Ocelot -o artifacts/


