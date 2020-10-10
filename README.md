# VoxSim
VoxSim is a semantically-informed event simulation engine created by the Brandeis University Lab for Language and Computation (Department of Computer Science), for creating custom intelligent agent behaviors.  This work is funded by the DARPA Communicating with Computers (CwC) progam.

## Add this as a submodule in your own Unity project:

$ mkdir submodules

$ cd submodules

$ git submodule add https://github.com/VoxML/VoxSim VoxSim

$ cd ../Assets/Plugins

If on Mac or \*nix:
$ ln -s ../../submodules/VoxSim/Assets/VoxSimPlatform VoxSimPlatform
If on Windows:
*WinMakeSymLink instructions coming soon*

$ git submodule update --remote --merge

## Dependencies

VoxSim depends on the following 3rd-party Unity libraries which are not included in the repository.  Find them on the Unity Asset Store.
* RTVoice
* FinalIK
* FlashbackRecorder
* ConsoleEnhanced Free
* SimpleFileBrowser

VoxSim also depends on Newtonsoft's JsonDotNet package, which is included as a .zip file.  Unzip the package from within *VoxSimPlatform/Packages* and place the result directly under *VoxSimPlatform*

## Keep your Unity project up to date with VoxSim

$ git add submodules/VoxSim

$ git commit -m "New commits in VoxSim"

$ git push origin <myBranch>
