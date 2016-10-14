echo -------------------------

echo Building Ocelot

dotnet restore src/Ocelot
dotnet restore src/Ocelot.Library
dotnet build src/Ocelot
dotnet pack src/Ocelot/project.json --no-build --output nupkgs
dotnet publish src/Ocelot -o site/wwwroot


