#!/bin/bash

# Development script with hot reload
# Run this for development with React hot reload

echo "🔥 Starting Benha Scooters in HOT RELOAD mode"
echo ""
echo "This will start:"
echo "  - React dev server on http://localhost:5173 (with hot reload)"
echo "  - .NET API server on http://localhost:5000"
echo ""

# Check if node_modules exists
if [ ! -d "src/BenhaScooters/ClientApp/node_modules" ]; then
    echo "📦 Installing npm dependencies..."
    cd src/BenhaScooters/ClientApp
    npm install
    cd ../../..
fi

# Function to cleanup background processes
cleanup() {
    echo ""
    echo "🛑 Stopping servers..."
    kill $REACT_PID $DOTNET_PID 2>/dev/null
    exit
}

trap cleanup EXIT INT TERM

# Start .NET backend in background
echo "🚀 Starting .NET backend..."
cd src/BenhaScooters
dotnet run &
DOTNET_PID=$!
cd ../..

# Wait a bit for .NET to start
sleep 3

# Start React dev server
echo "🚀 Starting React dev server..."
cd src/BenhaScooters/ClientApp
npm run dev &
REACT_PID=$!
cd ../../..

echo ""
echo "✅ Both servers are running!"
echo "📍 React Dev Server: http://localhost:5173"
echo "📍 .NET API Server: http://localhost:5000"
echo "🔑 Admin credentials: 01234567890 / password"
echo ""
echo "Press Ctrl+C to stop both servers"

# Wait for any process to exit
wait
