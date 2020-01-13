 
echo "Deploying DnDGen.Stress and DnDGen.Stress.Events to NuGet"

ApiKey=$1
Source=$2

echo "Nuget Source is $Source"
echo "Nuget API Key is $ApiKey (should be secure)"

echo "Pushing DnDGen.Stress"
dotnet nuget push ./DnDGen.Stress/bin/Release/DnDGen.Stress.*.nupkg --api-key $ApiKey --source $Source --skip-duplicate

echo "Pushing DnDGen.Stress.Events"
dotnet nuget push ./DnDGen.Stress.Events/bin/Release/DnDGen.Stress.Events.*.nupkg --api-key $ApiKey --source $Source --skip-duplicate