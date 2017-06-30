#!/bin/bash
xcodebuild -scheme Unity-iPhone DSTROOT="./ReleaseLocation" archive; ios-deploy --justlaunch --debug --bundle ReleaseLocation/Applications/VoxSim.app

