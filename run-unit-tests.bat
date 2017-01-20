echo -------------------------

echo Restoring Ocelot
dotnet restore src/Ocelot

echo Running Ocelot.UnitTests
dotnet restore test/Ocelot.UnitTests/
dotnet test test/Ocelot.UnitTests/
