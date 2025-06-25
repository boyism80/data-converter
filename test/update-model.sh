#!/bin/bash
echo "Updating Go model from data-converter..."

# Run data-converter
cd ..
dotnet run
if [ $? -ne 0 ]; then
    echo "Error: data-converter execution failed"
    exit 1
fi

# Copy generated model.go to test/generated folder
cp "bin/output/Go/server/model.go" "test/generated/"
if [ $? -ne 0 ]; then
    echo "Error: Failed to copy model.go"
    exit 1
fi

echo "Model updated successfully!"
echo "You can now run tests with: go test -v -args -json=\"../bin/output/json/server\"" 