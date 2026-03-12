library(targets)
library(here)
# Gnarwarre2025
# - What? Base simulation and observed data copied from https://github.com/FAR-Australia/UOM2312-001RTX/tree/master 
# - When? 2026-02-09
# - Who? Ben Jones @ FAR Australia (ben.jones@faraustralia.com.au)

# libraries
tar_option_set(packages = c("here", "tidyverse", "lubridate", "readxl"))

# functions
 source("R/read_excel_observed.R")
 source("R/do_obs_means.R")
 source("R/save_df_into_excel.R")
 source("R/get_pheno_dates.R")
 source("R/check_manual_params.R")
source("R/check_project_dependencies.R")

# NOTE: Incomplete - awaits raw data availability
# We need Obs data for phenology stages 6 and 8 to continue development

#----------------
# Project name
#----------------

proj_name <- "Gnarwarre2025"


# target objects
list(
  # all configuration parameters
  tar_target(
    config,
    list(
      # folders and file names
      proj_name                   = proj_name,
      folder_thisScript           = here::here(),
      folder_rawData              = here::here(proj_name), # this will be from Cloud
      folder_inputs               = here::here("..", "inputs"),
      folder_apsimx               = here::here(), # a level up from where Analysis is
      folder_met                  = here::here("..", "met"),
      file_rawData_excel          = "Gnarwarre2025/Observed.xlsx", # raw observed data (pre-defined file name),
      file_workData_excel         = paste0(proj_name, "_Observed.xlsx"), # raw observed data (pre-defined file name)
      sheet_name_observed         = "Observed",
      date_DOY_ref                = "01-01-2025", # date to transform DOY output into ddmmyy within simulations
      btwStgPerc                  = 0.5, # fraction of time in-between two pheno-stages when we assume a missing stage 
      file_name_input_pheno       = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun        = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_met               = "Gnarwarre_-38.20_144.05_2025.met" # pre-defined met
    )),
  
  # # read observations
   tar_target(file_obs, read_excel_observed(file.path(config$folder_apsimx, 
                                                      config$file_rawData_excel))),
  # 
  # # average reps
  tar_target(file_obs_mean, do_obs_means(file_obs)),

  # # retrieve measured pheno dates from observations
   tar_target(df_obs_pheno_dates, 
              get_pheno_dates(file_obs_mean, config$date_DOY_ref)),
  
  # # check if haun manual parameters is correct
   tar_target(haun_input_checked, check_manual_params(config$folder_inputs,
                                                       config$file_name_input_haun,
                                                       file_obs_mean)),
  # save new mean Observed
  tar_target(
    msg_obs_saved,
    save_df_into_excel(
      df = file_obs_mean,
      folder = config$folder_apsimx,
      filename = config$file_workData_excel,
      sheetname = config$sheet_name_observed
    ),
    format = "file"
  ),
  
  # Post-flight dependency check for APSIM
  tar_target(
    name = check_depend, 
    command = {
      # 1. List the targets here to force `{targets}` to wait for them
      msg_obs_saved
      #msg_pheno_param_saved
      haun_input_checked
      
      # 2. Now run the actual function
      check_project_dependencies(
        met_name = config$file_name_met,
        projects = config$proj_name,
        dir_met = config$folder_met,
        dir_inputs = config$folder_inputs,
        dir_obs = config$folder_apsimx
      )
    }
  )
  
)
