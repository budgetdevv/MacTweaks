#!/bin/bash

# Save the current directory
current_dir=$(pwd)

# Ensure the script is running in the intended directory
cd "$(dirname "$0")"

# Delete the /bin/release directory
if [ -d "bin/release" ]; then
    rm -rf bin/release
    echo "bin/release directory deleted."
else
    echo "bin/release directory not found."
fi

# Clean and rebuild the C# project
dotnet clean -c Release
dotnet build -c Release

echo "Cleanup and rebuild complete."