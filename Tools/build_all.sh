#!/usr/bin/env bash
# Rebuilds everything from source: generated defs, textures, sounds, the C# assembly, and validates the result.
# Requirements: python3 with numpy + pillow, .NET SDK 8+ (only needed to recompile the DLL / run the validator).
set -euo pipefail
cd "$(dirname "$0")/.."
echo "== Generating defs"
python3 Tools/gen/build.py
echo "== Rendering textures"
python3 Tools/art/make_textures.py
echo "== Synthesizing sounds"
python3 Tools/audio/make_sounds.py
echo "== Compiling assembly"
dotnet build Source/ArcanumAscendant/ArcanumAscendant.csproj -c Release
echo "== Validating XML against the assembly"
REFS=$(ls -d ~/.nuget/packages/krafs.rimworld.ref/*/ref/net472 | tail -1)
dotnet build Tools/validator/validator.csproj -c Release -v q
dotnet Tools/validator/bin/Release/net8.0/validator.dll . "$REFS"
