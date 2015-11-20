#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [Parameter(Mandatory=$true)][string]$Configuration,
    [Parameter(Mandatory=$true)][string]$OutputDir,
    [Parameter(Mandatory=$true)][string]$RepoRoot)

$Projects = @(
    "Microsoft.DotNet.ProjectModel",
    "Microsoft.DotNet.ProjectModel.Workspaces"
)

# Pack each project using the newly built stage2
$startPath = $env:PATH
try {
    $env:PATH="$RepoRoot\artifacts\win7-x64\stage2\bin;$env:PATH"
    $Projects | ForEach-Object {
        dotnet pack --output "$OutputDir" --configuration "$Configuration" "$RepoRoot\src\$_" --version $env:DOTNETCLI_BUILD_VERSION
    }
} finally {
    $env:PATH = $startPath
}

