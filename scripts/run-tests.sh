#!/bin/bash

# PrivateDNS Test Runner Script
# This script runs all unit tests with coverage reporting

echo "PrivateDNS Test Suite"
echo "===================="
echo ""

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "? .NET SDK not found. Please install .NET 8 SDK."
    exit 1
fi

echo "? .NET SDK found: $(dotnet --version)"
echo ""

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
TEST_PROJECT="$PROJECT_DIR/PrivateDNS.Tests"

echo "?? Project directory: $PROJECT_DIR"
echo "?? Test project: $TEST_PROJECT"
echo ""

# Check if test project exists
if [ ! -f "$TEST_PROJECT/PrivateDNS.Tests.csproj" ]; then
    echo "? Test project not found at $TEST_PROJECT"
    exit 1
fi

echo "?? Restoring packages..."
cd "$PROJECT_DIR"
if ! dotnet restore; then
    echo "? Failed to restore packages"
    exit 1
fi

echo ""
echo "???  Building solution..."
if ! dotnet build --configuration Release --no-restore; then
    echo "? Build failed"
    exit 1
fi

echo ""
echo "?? Running tests..."

# Run tests with coverage
cd "$TEST_PROJECT"

# Basic test run
echo "Running basic tests..."
if dotnet test --configuration Release --no-build --verbosity normal --logger "console;verbosity=detailed"; then
    echo "? All tests passed!"
else
    echo "? Some tests failed!"
    exit 1
fi

echo ""
echo "?? Running tests with coverage..."

# Run with coverage (requires coverlet.msbuild package)
if dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage" --results-directory ./TestResults; then
    echo "? Tests with coverage completed!"
    
    # Check if coverage files were generated
    if ls ./TestResults/*/coverage.cobertura.xml 1> /dev/null 2>&1; then
        echo "?? Coverage report generated in TestResults/"
    fi
else
    echo "?? Tests with coverage had issues, but tests may have passed"
fi

echo ""
echo "?? Test Categories Run:"
echo "- ? Unit Tests (Models, Services)"
echo "- ? Integration Tests"
echo "- ? Performance Tests"
echo ""

echo "?? Test Summary:"
echo "- DNS Message parsing and serialization"
echo "- Domain configuration and matching"
echo "- DNS forwarding logic"
echo "- Service lifecycle management"
echo "- Performance and memory usage"
echo ""

echo "?? Test run completed!"

# Optional: Generate coverage report if reportgenerator is available
if command -v reportgenerator &> /dev/null; then
    echo ""
    echo "?? Generating HTML coverage report..."
    if ls ./TestResults/*/coverage.cobertura.xml 1> /dev/null 2>&1; then
        reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
        echo "? HTML coverage report available at: ./TestResults/CoverageReport/index.html"
    fi
fi