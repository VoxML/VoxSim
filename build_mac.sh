#!/bin/bash
osascript -e 'quit app "Unity"'
/Applications/Unity/Unity.app/Contents/MacOS/Unity -projectpath $(pwd) -executeMethod AutoBuilder.BuildMac VoxSim -quit
