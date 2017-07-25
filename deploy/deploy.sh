 
echo "Deploying StressGen to NuGet"

ApiKey=$1
Source=$2

echo "Nuget Source is $Source"
echo "Nuget API Key is $ApiKey (should be secure)"

echo "Listing bin directory"
for entry in "./StressGen/bin"/*
do
  echo "$entry"
done

echo "Packing StressGen"
nuget pack ./StressGen/StressGen.nuspec -Verbosity detailed

echo "Packing StressGen.Events"
nuget pack ./StressGen.Events/StressGen.Events.nuspec -Verbosity detailed

echo "Pushing StressGen"
nuget push ./StressGen.*.nupkg -Verbosity detailed -ApiKey $ApiKey -Source $Source

echo "Pushing StressGen.Events"
nuget push ./StressGen.Events.*.nupkg -Verbosity detailed -ApiKey $ApiKey -Source $Source