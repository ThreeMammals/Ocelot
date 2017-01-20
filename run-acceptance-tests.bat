echo Running Ocelot.AcceptanceTests
cd test/Ocelot.AcceptanceTests/
dotnet restore 
dotnet test 
cd ../../

echo Restoring Ocelot.ManualTest
dotnet restore test/Ocelot.ManualTest/