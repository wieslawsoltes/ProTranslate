@echo off
setlocal

dotnet restore ProTranslate.CI.slnx || exit /b 1
dotnet build ProTranslate.CI.slnx -c Release --no-restore || exit /b 1
dotnet test tests\ProTranslate.Tests\ProTranslate.Tests.csproj -c Release --no-build || exit /b 1
dotnet test tests\ProTranslate.Analyzers.Tests\ProTranslate.Analyzers.Tests.csproj -c Release --no-build || exit /b 1
dotnet test tests\ProTranslate.Avalonia.Tests\ProTranslate.Avalonia.Tests.csproj -c Release --no-build || exit /b 1
dotnet test tests\ProTranslate.Uno.Tests\ProTranslate.Uno.Tests.csproj -c Release --no-build || exit /b 1
