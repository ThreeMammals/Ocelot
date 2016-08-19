 #!/bin/bash 
echo -------------------------

echo Running Ocelot.UnitTests

dotnet test test/Ocelot.UnitTests/

echo Running Ocelot.AcceptanceTests

dotnet test test/Ocelot.AcceptanceTests/

echo Building Ocelot

dotnet publish src/Ocelot


