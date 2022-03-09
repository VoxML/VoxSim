# VoxSim
VoxSim is a semantically-informed event simulation engine created by the Brandeis University Lab for Language and Computation (Department of Computer Science) and the Colorado State University Situated Grounding and Natural Language Lab, for creating custom intelligent agent behaviors.  This work was funded by the DARPA Communicating with Computers (CwC) program, and further research is being funded by the NSF.

## Quick Start

There is a forkable "Quick Start" repository at: https://github.com/VoxML/VoxWorld-QS. Clone this repo and then install VoxSim using one of the two methods below:

## Installing VoxSim

To install VoxSim in the `VoxWorld-QS` (or any other) project:

### Latest Stable Package

Download the required VoxSim required assets as a package [here](), and import the package into your project.  Everything should uncompress into the *Plugins* folder.

[//]: # (NOTE: Add instruction to delete empty VoxSimPlatform file, or just remove the submodule from the QS project?)

### Bleeding-edge Version

The trouble with bleeding-edge versions is that you can bleed a lot.  If you feel brave, follow the instructions under "Add VoxSim as a submodule in your own Unity project" below.

### Optional Assets

Some additional artwork (models, textures) is available [here](https://github.com/VoxML/voxicon/blob/master/packages/VoxSimObjectLibrary.unitypackage.zip?raw=true).  Please note that this is provided without support or guarantees.

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

[//]: # (NOTE: remove this section)

## Keep your Unity project up to date with VoxSim

```
$ git add submodules/VoxSim
$ git commit -m "New commits in VoxSim"
$ git push origin <myBranch>
```
