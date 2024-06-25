---
title: "Command Line on Linux"
draft: false
---


# Install

There are two options, option 1 is recommended for users running APSIM on cloud infrastructure (HPC, AWS, Azure) or WSL (Windows Subsystem for Linux)

1. run APSIM using a docker container
2. install APSIM using debian file (.deb) to see how to do this click [here](../../../install/linux/)

# Running APSIM on Linux using Docker container

<em>Note: it helps to be familiar with the linux command line basics to perform the following steps.</em>

1. Make sure you have the docker engine installed. Instructions on how to do this can be found <a href="https://docs.docker.com/engine/install/ubuntu/#install-using-the-repository">here</a>
2. Pull down the apsimng docker image using: `docker pull apsiminitiative/apsimng`
3. To see an example of how to run the wheat example, follow the instructions from the <a href="https://github.com/APSIMInitiative/APSIM.Docker" target="_blank">APSIM.Docker GitHub repo</a> otherwise continue.
4. Organise your files into a directory. I'll use an example directory here called `test-run`
5. Place an Apsimx file in this directory called `Wheat.apsimx`
6. Change directory to the newly created test-run directory.
7. Run the simulation using this command:  `docker run -i --rm -v "$PWD:/test-run" apsiminitiative/apsimng /test-run/Wheat.apsimx`
8. You can use any commands available for the APSIM command line tool such as `--apply` or `--csv` after the `apsiminitiative/apsimng` command portion in the above docker command. The above command simply ran the Wheat.apsimx file we put in the test-run directory.
