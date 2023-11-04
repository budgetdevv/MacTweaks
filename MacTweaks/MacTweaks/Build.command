#!/bin/bash

# Save the current directory
current_dir=$(pwd)

# Ensure the script is running in the intended directory
cd "$(dirname "$0")"

# Delete the /bin/Release directory
if [ -d "bin/Release" ]; then
    rm -rf bin/Release
    echo "bin/Release directory deleted."
else
    echo "bin/Release directory not found."
fi

# Clean and rebuild the C# project
dotnet clean -c Release
dotnet build -c Release

echo "Cleanup and rebuild complete."