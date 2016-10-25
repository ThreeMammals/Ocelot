echo -------------------------

echo Packing Ocelot Version %1
nuget pack .\Ocelot.nuspec -version %1

echo Publishing Ocelot
 nuget push Ocelot.%1.nupkg -ApiKey adc6c39d-ae94-496c-823e-b96f64ee23ff -Source https://www.nuget.org/api/v2/package




