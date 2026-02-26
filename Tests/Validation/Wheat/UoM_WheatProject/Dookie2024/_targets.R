library(targets)
library(here)
# Dookie 2024

# libraries
tar_option_set(packages = c("here", "tidyverse", "lubridate", "readxl"))

# functions
source("R/read_and_merge_phenology_observed.R")
source("R/check_manual_params.R")
source("R/read_csv_sowDates.R")
source("R/find_first_stage_dates.R")
source("R/interpolate_and_create_phenoStages.R")
source("R/spread_and_csv_save.R")
source("R/read_and_merge_obs_files.R")
source("R/save_df_to_excel.R")
source("R/check_project_dependencies.R")


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
      proj_name                   = "Dookie2024", 
      folder_thisScript           = here::here(),
      folder_inputs               = here::here("..", "inputs"),
      folder_met                  = here::here("..", "met"),
      folder_apsimx               = here::here(), # a level up from where Analysis is
      folder_rawData              = here::here("Dookie2024"),  
      file_rawData_excel          = c("DookieEVA2024.xlsx",
                                      "DookieWWHI2024.xlsx"), # raw observed data (pre-defined file name),
      file_workData_excel         = "Dookie2024_Observed.xlsx", #produced observed data (pre-defined file name)
      sheet_name_observed         = "Observed",
      date_DOY_ref                = "01-01-2024", # date to transform DOY output into ddmmyy within simulations
      btwStgFrac                  = 0.5, # fraction of time in-between two pheno-stages when we assume a missing stage 
      file_name_input_pheno       = "Dookie2024_PhenoDatesInput.csv",
      file_name_input_haun        = "Dookie2024_HaunStagesInput.csv",
      cols_to_extract             = c("SimulationName",
                                         "Clock.Today",
                                         "Wheat.Phenology.HaunStage",
                                         "Wheat.Phenology.Stage"),
      file_name_cult_by_sowDate  = "CultivarBySowingDatesTemplate.csv"
        )),
  

  # read observations of phenology and haun
  tar_target(file_pheno_haun_obs, read_and_merge_phenology_observed(config$folder_rawData,
                                               config$file_rawData_excel, 
                                               config$sheet_name_observed,
                                               config$cols_to_extract)),
  
  
  
  # check if haun manual-parameters are correct
  tar_target(haun_input_checked, check_manual_params(config$folder_inputs,
                                                     config$file_name_input_haun,
                                                     file_pheno_haun_obs)),
    
  # prepare the phenology input 
  tar_target(df_sowDates_by_sim, read_csv_sowDates(config$folder_rawData,
                                             config$file_name_cult_by_sowDate)),
  
   tar_target(df_pheno_start_date, 
              find_first_stage_dates(df_sowDates_by_sim, 
                                                 file_pheno_haun_obs, 
                                                 sow_date_col = "SowingDate")),
  tar_target(df_pheno_interp, 
             interpolate_and_create_phenoStages(df_pheno_start_date, config$btwStgFrac)),
  
  tar_target(df_pheno_input_csv_saved, spread_and_csv_save(config$folder_inputs, config$file_name_input_pheno, 
                                                   df_pheno_interp)),
  
  tar_target(df_all_obs_files, read_and_merge_obs_files(file.path(config$folder_rawData),
                                                        config$file_rawData_excel,
                                                        config$sheet_name_observed)),
  
  tar_target(saved_obs_file,save_df_to_excel(config$folder_apsimx,
                                             config$file_workData_excel, 
                                             config$sheet_name_observed, 
                                             df_all_obs_files)),
  
  # pre-flight dependency check for APSIM
  tar_target(check_depend, check_project_dependencies(projects = config$proj_name,
                                                      dir_met = config$folder_met,
                                                      dir_inputs= config$folder_inputs,
                                                      dir_obs= config$folder_apsimx))
  
  
)
