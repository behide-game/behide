rm -Recurse -Force ./Builds/Windows-arm64/*
godot --headless --export-release "Windows Desktop Arm64"
