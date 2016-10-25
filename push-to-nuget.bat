echo -------------------------

echo Packing Ocelot Version %1
nuget pack .\Ocelot.nuspec -version %1

echo Publishing Ocelot
 nuget push Ocelot.%1.nupkg -ApiKey %2 -Source https://www.nuget.org/api/v2/package




