 #!/bin/bash 
echo -------------------------

echo Running Ocelot.UnitTests

dotnet restore test/Ocelot.UnitTests/
dotnet test test/Ocelot.UnitTests/

echo Running Ocelot.AcceptanceTests

cd test/Ocelot.AcceptanceTests/
dotnet restore 
dotnet test 
cd ../../

