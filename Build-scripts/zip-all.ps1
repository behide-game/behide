try {
    rm -Force ./Builds/Linux.zip
    rm -Force ./Builds/Windows.zip
    rm -Force ./Builds/Windows-arm64.zip
} catch {}

Compress-Archive .\Builds\Linux\* .\Builds\Linux.zip
Compress-Archive .\Builds\Windows\* .\Builds\Windows.zip
Compress-Archive .\Builds\Windows-arm64\* .\Builds\Windows-arm64.zip
