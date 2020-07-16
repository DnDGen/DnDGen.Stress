 
echo "Deploying DnDGen.Stress to NuGet"

ApiKey=$1
Source=$2

echo "Nuget Source is $Source"
echo "Nuget API Key is $ApiKey (should be secure)"

echo "Pushing DnDGen.Stress"
dotnet nuget push ./DnDGen.Stress/bin/Release/DnDGen.Stress.*.nupkg --api-key $ApiKey --source $Source --skip-duplicate