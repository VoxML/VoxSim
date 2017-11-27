#!/bin/bash
osascript -e 'quit app "Unity"'
cd ../VoxSim-Mobile
git submodule foreach git pull origin master
/Applications/Unity/Unity.app/Contents/MacOS/Unity -projectpath $(pwd) -executeMethod AutoBuilder.BuildIOS VoxSim -quit
mkdir Build/ios/VoxSim/VoxML
mkdir Build/ios/VoxSim/VoxML/voxml
cd ../VoxSim
cp deploy_script.sh ../VoxSim-Mobile/Build/ios/VoxSim
cp -r Data/voxml ../VoxSim-Mobile/Build/ios/VoxSim/VoxML
