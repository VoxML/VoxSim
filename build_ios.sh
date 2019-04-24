#!/bin/bash
# On Mac, builds and deploys VoxSim for iOS platform
# Pass -b with a build configuration XML file (required)
# You must have Unity iOS build support installed
# Clean quits Unity if already open
# Quits Unity when complete
# Make sure your device is plugged in and provisioned!
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
    osascript -e 'quit app "Unity"'
    cd ../VoxSim-Mobile
    git submodule foreach git pull origin master
    /Applications/Unity/Unity.app/Contents/MacOS/Unity -projectpath $(pwd) -executeMethod StandaloneBuild.AutoBuilder.BuildIOS VoxSim $CONFIG -quit
    mkdir Build/ios/VoxSim/VoxML
    mkdir Build/ios/VoxSim/VoxML/voxml
    cd ../VoxSim
    cp deploy_script.sh ../VoxSim-Mobile/Build/ios/VoxSim
    cp -r Data/voxml ../VoxSim-Mobile/Build/ios/VoxSim/VoxML
fi
