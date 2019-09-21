# VoxSim
VoxSim is a semantically-informed event simulation engine created by the Brandeis University Lab for Language and Computation (Department of Computer Science), for creating custom intelligent agent behaviors.  This work is funded by the DARPA Communicating with Computers (CwC) progam.

Add this as a submodule in your own Unity project:

$ mkdir submodules

$ cd submodules

$ git submodule add https://github.com/VoxML/VoxSim VoxSim

$ cd ../Assets/Plugins

$ ln -s ../../submodules/VoxSim/Assets/VoxSimPlatform VoxSimPlatform

$ git submodule foreach git pull
