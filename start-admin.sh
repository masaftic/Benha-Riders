#!/bin/bash

# Development script for Benha Scooters Admin Dashboard
# This script runs both the .NET backend and React frontend in development mode

echo "🚀 Starting Benha Scooters Admin Dashboard (Development Mode)"
echo ""

# Check if node_modules exists
if [ ! -d "src/BenhaScooters/ClientApp/node_modules" ]; then
    echo "📦 Installing npm dependencies..."
    cd src/BenhaScooters/ClientApp
    npm install
    cd ../../..
fi

# Build React app
echo "🔨 Building React app..."
cd src/BenhaScooters/ClientApp
npm run build
cd ../../..

echo ""
echo "✅ Build complete! Starting .NET application..."
echo ""
echo "📍 Admin Dashboard will be available at: http://localhost:5000"
echo "🔑 Default admin credentials:"
echo "   Phone: 01234567890"
echo "   Password: password"
echo ""

# Run .NET application
cd src/BenhaScooters
dotnet run
