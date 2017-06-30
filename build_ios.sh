#!/bin/bash
cd ../VoxSim-Mobile;echo $(pwd);git submodule foreach git pull origin master;/Applications/Unity/Unity.app/Contents/MacOS/Unity -projectpath $(pwd) -executeMethod AutoBuilder.BuildIOS VoxSim -quit
