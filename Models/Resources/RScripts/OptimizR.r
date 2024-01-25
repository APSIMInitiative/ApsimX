library(ApsimOnR)
library(CroptimizR)
library(dplyr)
library(nloptr)
library(DiceDesign)
library(stringr)

start_time <- Sys.time()

# Here we assume that the observed/met data is in the same directory as the .apsimx file.
files_path <- dirname(apsimx_file)

# met files path
met_files_path <- files_path

# obs path
obs_files_path <- files_path

# Runnning the model without forcing parameters
model_options=apsimx_wrapper_options(apsimx_path = apsimx_path,
                                     apsimx_file = apsimx_file,
                                     variable_names = variable_names,
                                     predicted_table_name = predicted_table_name,
                                     met_files_path = met_files_path,
                                     observed_table_name = observed_table_name,
                                     obs_files_path = obs_files_path)

sim_before_optim=apsimx_wrapper(model_options=model_options)

# observations
obs_list <- read_apsimx_output(sim_before_optim$db_file_name,
                               model_options$observed_table_name,
                               observed_variable_names,
                               names(sim_before_optim$sim_list))

simulation_names_old <- simulation_names
simulation_names <- intersect(names(obs_list), simulation_names)
obs_list=obs_list[simulation_names]

dropped_sims <- setdiff(simulation_names_old, simulation_names)
if (length(dropped_sims) > 0) {
  print(paste('NOTE: dropping simulation', dropped_sims, 'as there is no data for this simulation'))
}

# Remove "Observed." from the start of any column.
# This helps when retrieving observed data from PredictedObserved tables,
# where the observed columns all start with "Observed.", but CroptimizR
# expects the predicted and observed variables to have the same name.
for (sim_name in simulation_names) {
  for (col in names(obs_list[[sim_name]])) {
    if (startsWith(col, "Observed.")) {
      names(obs_list[[sim_name]])[names(obs_list[[sim_name]]) == col] <- str_replace(col, "Observed.", "Predicted.")
    }
  }
}

# Run the optimization
optim_output=estim_param(obs_list=obs_list,
                         crit_function=crit_function,
                         model_function=apsimx_wrapper,
                         model_options=model_options,
                         optim_options=optim_options,
                         optim_method=optim_method,
                         param_info=param_info)

duration <- as.double(difftime(Sys.time(), start_time, units = "secs"))
print(sprintf('duration: %s seconds', duration)) 
