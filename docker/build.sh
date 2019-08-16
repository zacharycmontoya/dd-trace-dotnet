#!/bin/bash
set -euxo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null && pwd )"

cd "$DIR/.."

for project in src/*/*.csproj ; do
    dotnet build -f netstandard2.0 -c $buildConfiguration $project
done

for project in test/*/*.csproj samples/*/*.csproj reproductions/*/*.csproj ; do
    dotnet build -f netcoreapp2.1 -c $buildConfiguration $project
done

dotnet msbuild Datadog.Trace.proj -t:RestoreAndBuildSamplesForPackageVersions -p:Configuration=$buildConfiguration
