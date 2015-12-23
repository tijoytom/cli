#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#
param([string]$Version, [string]$InstallDir)

$ErrorActionPreference="Stop"
#$ProgressPreference="SilentlyContinue"

$Feed="https://dotnetcli.blob.core.windows.net/dotnet"
$Channel="dev"

function say($str)
{
    Write-Host "dotnet_install: $str"
}

if(!$Version)
{
    $Version = "Latest"
}

if (!$InstallDir) {
    if($env:DOTNET_INSTALL_DIR) {
        $InstallDir = $env:DOTNET_INSTALL_DIR
    } else {
        $InstallDir = "$env:LocalAppData\Microsoft\dotnet"
    }
}

say "Preparing to install .NET Tools to $InstallDir"

# Check if we need to bother
$LocalFile = "$InstallDir\cli\.version"
if (Test-Path $LocalFile)
{
    $LocalData = @(cat $LocalFile)
    $LocalHash = $LocalData[0].Trim()
    $LocalVersion = $LocalData[1].Trim()

    if($Version -eq "Latest") {
        if ($LocalVersion -and $LocalHash)
        {
            $RemoteResponse = Invoke-WebRequest -UseBasicParsing "$Feed/$Channel/dnvm/latest.win.version"
            $RemoteData = @([Text.Encoding]::UTF8.GetString($RemoteResponse.Content).Split([char[]]@(), [StringSplitOptions]::RemoveEmptyEntries));
            $RemoteHash = $RemoteData[0].Trim()
            $RemoteVersion = $RemoteData[1].Trim()

            if (!$RemoteVersion -or !$RemoteHash) {
                throw "Invalid response from feed"
            }

            say "Latest version: $RemoteVersion"
            say "Local Version: $LocalVersion"

            if($LocalHash -eq $RemoteHash)
            {
                say "You already have the latest version"
                exit 0
            }

            $Version = $RemoteVersion
        }
    } else {
        if($LocalVersion -eq $Version)
        {
            say "You already have $Version installed."
            exit 0
        }
    }
}

# Set up the install location
if (!(Test-Path $InstallDir)) {
    mkdir $InstallDir | Out-Null
}

# De-powershell the path before passing to .NET APIs
$InstallDir = Convert-Path $InstallDir

$DotNetFileName="dotnet-win-x64.$($Version.ToLower()).zip"
$DotNetUrl="$Feed/$Channel/Binaries/$Version"

say "Downloading $DotNetFileName from $DotNetUrl"

try {
    $resp = Invoke-WebRequest -UseBasicParsing "$DotNetUrl/$DotNetFileName" -OutFile "$InstallDir\$DotNetFileName"
} catch {
    if($_.Exception.Response.StatusCode -eq [System.Net.HTTPStatusCode]::NotFound) {
        say "The requested version of the CLI was not found on the server."
    } else {
        say "Error downloading file. StatusCode: $($_.Exception.Response.StatusCode)"
    }
    exit 0
}

say "Extracting zip"
# Create the destination
if (Test-Path "$InstallDir\cli_new") {
    del -rec -for "$InstallDir\cli_new"
}
mkdir "$InstallDir\cli_new" | Out-Null

Add-Type -Assembly System.IO.Compression.FileSystem | Out-Null
[System.IO.Compression.ZipFile]::ExtractToDirectory("$InstallDir\$DotNetFileName", "$InstallDir\cli_new")

# Replace the old installation (if any)
if (Test-Path "$InstallDir\cli") {
    del -rec -for "$InstallDir\cli"
}
mv "$InstallDir\cli_new" "$InstallDir\cli"

# Clean the zip
if (Test-Path "$InstallDir\$DotNetFileName") {
    del -for "$InstallDir\$DotNetFileName"
}

say "The .NET Tools have been installed to $InstallDir\cli!"

# New layout
say "Add '$InstallDir\cli\bin' to your PATH to use dotnet"