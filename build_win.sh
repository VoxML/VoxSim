#!/bin/bash
# On Mac, builds VoxSim for Windows platform
# Pass -b with a build configuration XML file (required)
# You must have Unity Windows build support installed
# Clean quits Unity if already open
# Quits Unity when complete
[ $# -lt 2 ] || [ $1 != "-b" ] && { echo "Usage: $0 -b config_file.xml"; exit 1; }
while getopts b: option
do
case "${option}"
in
b) CONFIG=${OPTARG};;
esac
done
if [ ! -f "$CONFIG" ]; then
    echo "No file named '$CONFIG' exists"
else
    if [[ "$OSTYPE" == "darwin"* ]]; then
        osascript -e 'quit app "Unity"'
        /Applications/Unity/Unity.app/Contents/MacOS/Unity -projectpath $(pwd) -executeMethod StandaloneBuild.AutoBuilder.BuildMac VoxSim $CONFIG -quit
    elif [[ "$OSTYLE" == "msys" ]]; then
        taskkill //F //IM Unity.exe //T
        C:\Program Files\Unity\Editor\Unity.exe -projectpath $(pwd) -executeMethod StandaloneBuild.AutoBuilder.BuildMac VoxSim $CONFIG -quit
    fi
fi
