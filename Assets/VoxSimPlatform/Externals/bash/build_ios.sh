#!/bin/bash
# On Mac, builds and deploys VoxSim for iOS platform
# Requires Xcode to run to completion and deploy, will not run on Windows
# Pass -b with a build configuration XML file (required)
# Pass -a with a path to Unity (optional: defaults to assumed known location in Applications)
#  use this if you have Hub installed and need to make sure to build VoxSim with a particular version of Unity
# You must have Unity iOS build support installed
# Clean quits Unity if already open
# Quits Unity when complete
# Make sure your device is plugged in and provisioned!
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
        cd ../VoxSim-Mobile
        git submodule foreach git pull origin master
        if [ -z "$UNITYPATH" ]; then
            UNITYPATH="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
        fi
        "$UNITYPATH" -projectpath $(pwd) -executeMethod StandaloneBuild.AutoBuilder.BuildIOS VoxSim $CONFIG -quit
        mkdir Build/ios/VoxSim/VoxML
        mkdir Build/ios/VoxSim/VoxML/voxml
        cd ../VoxSim
        cp deploy_script.sh ../VoxSim-Mobile/Build/ios/VoxSim
        cp -r Data/voxml ../VoxSim-Mobile/Build/ios/VoxSim/VoxML
    else
        echo "Building for iOS requires Mac OSX and Xcode!"
    fi
fi
