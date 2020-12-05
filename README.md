# VoxSim
VoxSim is a semantically-informed event simulation engine created by the Brandeis University Lab for Language and Computation (Department of Computer Science), for creating custom intelligent agent behaviors.  This work is funded by the DARPA Communicating with Computers (CwC) progam.

## Quick Start

There is a forkable "Quick Start" repository at: https://github.com/VoxML/VoxWorld-QS. Once the setup is complete, the VoxSim submodule will be installed in a Unity project that contains a sample scene with all required VoxWorld components that you can begin working in immediately.

## API Documentation

Currently in progress

## Add VoxSim as a submodule in your own Unity project:

```
$ mkdir submodules
$ cd submodules
$ git submodule add https://github.com/VoxML/VoxSim VoxSim
$ cd ../Assets/Plugins
```

If on Mac or \*nix:
```
$ ln -s ../../submodules/VoxSim/Assets/VoxSimPlatform VoxSimPlatform\
```

If on Windows:\
Run *cmd* and cd to the main directory of your VoxSim-based implementation (parallel to Assets). Run the following commands:
```
@setlocal enableextensions\
@cd /d "%~dp0"\
rmdir Assets\Plugins\VoxSimPlatform & del /Q Assets\Plugins\VoxSimPlatform & mklink /D Assets\Plugins\VoxSimPlatform ..\\..\submodules\VoxSim\Assets\VoxSimPlatform
```

Then:
```
$ git submodule foreach git pull
```

## Dependencies

VoxSim depends on the following 3rd-party Unity libraries which are not included in the repository.  Find them on the Unity Asset Store.
* RTVoice
* FinalIK
* FlashbackRecorder
* ConsoleEnhanced Free
* SimpleFileBrowser

VoxSim also depends on Newtonsoft's JsonDotNet package, which is included as a .zip file.  Unzip the package from within *VoxSimPlatform/Packages* and place the result directly under *VoxSimPlatform*

## Keep your Unity project up to date with VoxSim

```
$ git add submodules/VoxSim
$ git commit -m "New commits in VoxSim"
$ git push origin <myBranch>
```
