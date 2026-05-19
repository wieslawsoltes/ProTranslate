#!/bin/bash
set -euo pipefail

dotnet tool restore
rm -rf site/.lunet/build
cd site
dotnet tool run lunet --stacktrace build
