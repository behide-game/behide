$x = $args[0]

godot --headless --export-release "Linux/X11" ./Builds/Linux/Behide.x86_64
scp -r .\Builds\Linux\Behide.x86_64 ($x + ":~/Downloads/Linux/")
scp -r .\Builds\Linux\Behide.pck ($x + ":~/Downloads/Linux/")
scp -r .\Builds\Linux\data_Behide_linuxbsd_x86_64\Behide.dll ($x + ":~/Downloads/Linux/data_Behide_linuxbsd_x86_64/")
ssh ($x) "chmod +x ~/Downloads/Linux/*; ~/Downloads/Linux/Behide.x86_64"
