library(targets)
library(here)
# Dookie 2024

# libraries
tar_option_set(packages = c("here", "tidyverse", "lubridate", "readxl"))

# functions
source("R/read_and_merge_observed.R")
source("R/check_manual_params.R")


# Read Wheat.Phenology.Stage from Observed (2 sets)
# Organise in spread format
# interpolate for non-available phases
# set a haun manual-input check


# target objects
list(
  # all configuration parameters
  tar_target(
    config,
    list(
      # folders and file names
      folder_thisScript           = here::here(),
      folder_inputs               = here::here("..", "inputs"),
      folder_apsimx               = here::here(), # a level up from where Analysis is
      folder_rawData              = here::here("Dookie2024"),  
      file_rawData_excel          = c("DookieEVA2024.xlsx",
                                      "DookieWWHI2024.xlsx"), # raw observed data (pre-defined file name),
      file_workData_excel         = "Dookie2024_Observed.xlsx", #produced observed data (pre-defined file name)
      sheet_name_observed         = "Observed",
      date_DOY_ref                = "01-01-2024", # date to transform DOY output into ddmmyy within simulations
      btwStgPerc                  = 0.5, # fraction of time in-between two pheno-stages when we assume a missing stage 
      file_name_input_pheno       = "Dookie2024_PhenoDatesInput.csv",
      file_name_input_haun        = "Dookie2024_HaunStagesInput.csv",
      cols_to_extract             = c("SimulationName",
                                         "Clock.Today",
                                         "Wheat.Phenology.HaunStage",
                                         "Wheat.Phenology.Stage")
        )),
  
  # read observations of phenology and haun
  tar_target(file_pheno_haun_obs, read_and_merge_observed(config$folder_rawData,
                                               config$file_rawData_excel, 
                                               config$sheet_name_observed,
                                               config$cols_to_extract)),
  
  # prepare the phenology input 
  
  # check if haun manual parameters is correct
  tar_target(haun_input_checked, check_manual_params(config$folder_inputs,
                                                     config$file_name_input_haun,
                                                     file_pheno_haun_obs))
  
)
