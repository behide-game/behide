godot --headless --export-release "Linux/X11" ./Builds/Linux/Behide.x86_64
scp -r .\Builds\Linux\Behide.x86_64 $args[0]:~/Downloads/Linux/
scp -r .\Builds\Linux\Behide.pck $args[0]:~/Downloads/Linux/
scp -r .\Builds\Linux\data_Behide_linuxbsd_x86_64\Behide.dll $args[0]:~/Downloads/Linux/data_Behide_linuxbsd_x86_64/
ssh $args[0] "chmod +x ~/Downloads/Linux/*; ~/Downloads/Linux/Behide.x86_64"
