#!/bin/bash
# On Mac, builds VoxSim for Windows platform
# Pass -b with a build configuration XML file (required)
# You must have Unity Windows build support installed
# Clean quits Unity if already open
# Quits Unity when complete
[ $# -lt 2 ] || [ $1 != "-b" ] && { echo "Usage: $0 -b <config file>.xml [-a path/to/unity]"; exit 1; }
while getopts b:a: option
do
case "${option}"
in
b) CONFIG=${OPTARG};;
a) UNITYPATH=${OPTARG};;
esac
done
if [ ! -f "$CONFIG" ]; then
    echo "No file named '$CONFIG' exists"
else
    if [[ "$OSTYPE" == "darwin"* ]]; then
        osascript -e 'quit app "Unity"'
	if [ -z "$UNITYPATH" ]; then
	    UNITYPATH="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
	fi
        "$UNITYPATH" -projectpath $(pwd) -executeMethod StandaloneBuild.AutoBuilder.BuildWindows VoxSim $CONFIG -quit
    elif [[ "$OSTYPE" == "msys" ]]; then
        taskkill //F //IM Unity.exe //T
	if [ -z "$UNITYPATH" ]; then
	    UNITYPATH="C:/Program Files/Unity/Editor/Unity.exe"
	fi
	"$UNITYPATH" -projectpath $(pwd) -executeMethod StandaloneBuild.AutoBuilder.BuildWindows VoxSim $CONFIG -quit
    fi
fi
