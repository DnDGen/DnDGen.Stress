 
echo "Deploying DnDGen.Stress to NuGet"

ApiKey=$1
Source=$2

echo "Nuget Source is $Source"
echo "Nuget API Key is $ApiKey (should be secure)"

echo "Packing DnDGen.Stress.Events"
nuget pack ./DnDGen.Stress.Events/DnDGen.Stress.Events.nuspec -Verbosity detailed

echo "Pushing DnDGen.Stress"
nuget push ./DnDGen.Stress/bin/Release/DnDGen.Stress.*.nupkg -Verbosity detailed -ApiKey $ApiKey -Source $Source

echo "Pushing DnDGen.Stress.Events"
nuget push ./DnDGen.Stress.Events.*.nupkg -Verbosity detailed -ApiKey $ApiKey -Source $Source