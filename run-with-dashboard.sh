#!/bin/bash

# Build Angular Dashboard and Run .NET API
echo "Building Angular Dashboard..."
cd src/BenhaScooters/DashboardApp

if [ ! -d "node_modules" ]; then
    echo "Installing npm dependencies..."
    npm install
fi

echo "Building Angular app..."
npm run build

if [ $? -eq 0 ]; then
    echo "Angular build successful!"
    echo "Starting .NET API..."
    cd ..
    dotnet run
else
    echo "Angular build failed!"
    exit 1
fi
