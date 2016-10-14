echo -------------------------

echo Restoring Ocelot
dotnet restore src/Ocelot

echo Restoring Ocelot
dotnet restore src/Ocelot.Library

echo Running Ocelot.UnitTests
dotnet restore test/Ocelot.UnitTests/
dotnet test test/Ocelot.UnitTests/

echo Running Ocelot.AcceptanceTests
cd test/Ocelot.AcceptanceTests/
dotnet restore 
dotnet test 
cd ../../