---
title: "Command Line Tutorial - Windows"
draft: false
---

# Example workflow for APSIMX command line 

In this example a workflow for building up a simulation from scratch will be showcased.
We will start with building a config file that gets an absolute minimum simulation.
From there we will build up the simulation into something that contains multiple nodes.
Afterwards we will describe how to modify, delete and copy nodes from another APSIMX file.

## 1. Creating a minimal simulation

1. Find your computer's location of models.exe
	1. The location on this computer is the default which is under 'C/Program Files/APSIM<VersionNumber>/bin'.
2. Copy the path of models.exe containing directory.
3. To keep everything organised and isolated from other files, let's create a directory just for this tutorial. 
	- Let's make a directory called cli-tutorial directly under C: drive. 
	- Open a terminal and type the command ```cd/```.
	- Next make a fresh directory called 'cli-tutorial' by typing the command ```mkdir cli-tutorial```
4. let's go to this directory now so that any files we make from now on are stored here. To do this type the command ```cd cli-tutorial```
5. Next, let's create a config file which will contain our commands in a file called 'commands.txt'. To do this type the command ```type nul > commands.txt```
6. Let's add a command to save a minimum runnable apsimx file to this config file. To do this type ```notepad commands.txt```
	- add the line ```save cli-tutorial.apsimx```.
	- save and close the file and return to the terminal.
7. ok, now we will run the command line version of Apsimx. Earlier we copied the path of models.exe's directory. We can now paste this into the terminal.
	- Before pressing the enter key, type ``` --apply commands.txt```. Making sure to include a space between the models.exe file path and '--apply'.
	    - Tip: If you have a space in your file path to the Models.exe directory, such as with the above example with `C:\Program Files\` you can put quotes on either end of the file path, making it ignore the space. Having a space can cause the command not to run.
	- After a short pause, we can type ```dir```. We should see a new file now called cli-tutorial.apsimx, along with a backup file called 'NewSimulation.bak', and our config file called 'commands.txt'.
	- let's open the file and see what is in there. You'll see that there is a Simulations node with an empty DataStore node as a child.
8. You've made a minimal apsimx file. From here you can build up the simulation with any nodes that you like. See below on how to do that.

## 2. Modifying a simulation

The scenario for this tutorial is we want to run a simulation with a different soil and a different weather file.

To begin we will use our barebones file we created in the previous step. If you have not got this file, follow the instructions above in part 1 [Creating a minimal simulation](#1-creating-a-minimal-simulation).

1. To begin, let's make sure we are in the same directory we finished in from Part 1 in a terminal. The file path for this is: `C:\cli-tutorial`
2. Let's open up the notepad and add some steps to a config file called `commands.txt`. To do this type `notepad commands.txt`.
3. Before we add anything, lets change the keyword `save` to `load`.
4. In order to copy nodes from a file, the file has to be present in the directory we are running the commands from. So in order to take some of the nodes from APSIM's example files we will copy these into the directory we are currently working in. To do this, run this command: `copy "C:\Program Files\Apsim2023.10.7317.0\Examples\Wheat.apsimx C:\cli-tutorial\`. Tip: It is very likely the version number is different when you are following this tutorial. Please make sure you change it to your personal version number when you run the command.
5. Let's copy a complete simulation from the wheat example in the apsim next gen examples. To do this let's add a copy command to the barbones file. The command looks like this: `add [Simulations] Wheat.apsimx;[Simulation]`. 
    - This command simply says: add the node called Simulation from the wheat.apsimx file and place within the node called Simulations in the currently loaded apsimx file.
6. Now that we have a simulation all ready to run we can start changing the nodes. Let's start by setting up the simulation to just run one year. To make this change, open the commands.txt file and add the command: `[Clock].End=31/12/1901`. The start time is already set to the start of the year 1901.
7. Let's run the simulation now to gather the data.To do this enter the command: `run`.
8. Next let's change the weather and soil nodes to so we can see how that changes the outputs.
9. In order to change the soil on this simulation, its easiest to copy one from an existing simulation or use one of the nodes already within apsim. To do this we must first have a donor APSIM file located in the same folder. Let's copy an example file from the APSIM examples folder and put it into our working directory. Let's copy the Chickpea example from the examples folder. On my computer this is located at `C:\Program Files\Apsim2023.10.7317.0\Examples\Chickpea.apsimx`.
    - if you're in your the cli-tutorial directory you can copy this by running the command in the terminal: `copy "C:\Program Files\Apsim2023.10.7317.0\Examples\Chickpea.apsimx" .`
10. Open the commands.txt file and add these lines: '[Soil]=Chickpea.apsimx;[Black Vertosol-Mywybilla (Bongeen No001)]`
11. To change the weather file we will need to have a weather file copied to the same directory. For this run we can take the weather file from the chickpea example as well. We can do that with the command: `copy "C:\Program Files\Apsim2023.10.7317.0\Examples\WeatherFiles\WaggaWagga.met" .`
12. ~~Now let's change the weather for this as well. Add the command: `[Weather].FileName=WaggaWagga.met`~~ Need to find an alternative.
13. To finish let's save all our changes by adding the command: `save cli-tutorial-pt2.apsimx`
14. Now let's add a run command again. Your file should now look like the below:
```
load cli-tutorial.apsimx
add [Simulations] Wheat.apsimx;[Simulation]
[Clock].End=1901/12/31
run
[Soil]=Chickpea.apsimx;[Black Vertosol-Mywybilla (Bongeen No001)]
[Weather].FileName=WaggaWagga.met
run
save cli-tutorial-pt2.apsimx
```
15. Finally, let's run the config file with the command in the terminal: `"C:\Program Files\Apsim2023.10.7317.0\bin\Models.exe" --apply commands.txt`
