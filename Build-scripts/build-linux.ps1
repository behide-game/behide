rm -Recurse -Force ./Builds/Linux/*
godot --headless --export-release "Linux/X11"
