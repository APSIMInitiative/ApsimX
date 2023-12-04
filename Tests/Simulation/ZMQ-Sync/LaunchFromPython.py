import subprocess
# fullFP = '/home/mwmaster/Documents/oasis_sim/oasis_sim/Tests/Simulation/ZMQ-Sync/ZMQ-Oneshot.R'
fullFP = '/home/mwmaster/Documents/oasis_sim/oasis_sim/Tests/Simulation/ZMQ-Sync/ZMQ-InteractiveVariables.R'

subprocess.call (["/usr/bin/Rscript", "--vanilla", fullFP])
