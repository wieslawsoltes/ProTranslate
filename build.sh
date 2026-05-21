#!/usr/bin/env bash
set -euo pipefail

dotnet restore ProTranslate.CI.slnx
dotnet build ProTranslate.CI.slnx -c Release --no-restore
dotnet test tests/ProTranslate.Tests/ProTranslate.Tests.csproj -c Release --no-build
dotnet test tests/ProTranslate.Analyzers.Tests/ProTranslate.Analyzers.Tests.csproj -c Release --no-build
dotnet test tests/ProTranslate.Avalonia.Tests/ProTranslate.Avalonia.Tests.csproj -c Release --no-build
dotnet test tests/ProTranslate.Uno.Tests/ProTranslate.Uno.Tests.csproj -c Release --no-build
