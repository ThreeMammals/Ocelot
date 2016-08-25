 #!/bin/bash 
echo -------------------------

echo Running Ocelot.Benchmarks

cd test/Ocelot.Benchmarks

dotnet restore 

dotnet run 

cd ../../




