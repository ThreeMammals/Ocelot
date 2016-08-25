 #!/bin/bash 
echo -------------------------

echo Running Ocelot.UnitTests

dotnet restore test/Ocelot.UnitTests/
dotnet test test/Ocelot.UnitTests/

echo Running Ocelot.AcceptanceTests

dotnet restore test/Ocelot.AcceptanceTests/
dotnet test test/Ocelot.AcceptanceTests/


