library(targets)
library(here)

# - What? Base simulation and observed data copied from https://github.com/FAR-Australia/UOM2312-001RTX/tree/master 
# - When? 2026-02-09
# - Who? Ben Jones @ FAR Australia (ben.jones@faraustralia.com.au)
# - Changes requested:
# - Average the 4 reps and add into sim as 1 point
# - Force fit of Haun Stage with Observation (as model input)
# - Force fit of phenology from observations (as model input)

# libraries
tar_option_set(packages = c("here", "tidyverse", "lubridate", "readxl"))

# functions
source("R/read_excel_observed.R")
source("R/do_obs_means.R")
source("R/save_df_into_excel.R")
source("R/get_pheno_dates.R")
source("R/add_interp_pheno_dates.R")
source("R/save_df_into_csv.R")
source("R/add_stages_to_obs.R")
source("R/get_column_var_from_observ.R")
source("R/check_manual_params.R")


# target objects
list(
  # all configuration parameters
  tar_target(
    config,
    list(
      # folders and file names
      folder_thisScript           = here::here(),
      folder_inputs               = here::here("..", "..", "inputs"),
      folder_apsimx               = here::here(".."), # a level up from where Analysis is
      file_rawData_excel          = "Observed.xlsx", # raw observed data (pre-defined file name),
      file_workData_excel         = "Observed_GrassPatch2024.xlsx", # raw observed data (pre-defined file name)
      sheet_name_observed         = "Observed",
      date_DOY_ref                = "01-01-2024", # date to transform DOY output into ddmmyy within simulations
      btwStgPerc                  = 0.5, # fraction of time in-between two pheno-stages when we assume a missing stage 
      file_name_input_pheno       = "GrassPatch2024_PhenoDatesInput.csv",
      file_name_input_haun        = "GrassPatch2024_HaunStagesInput.csv"
        )),
  
  # read observations
  tar_target(file_obs, read_excel_observed(file.path(config$folder_apsimx, 
                                                     config$file_rawData_excel))),
  
  # average reps
  tar_target(file_obs_mean, do_obs_means(file_obs)),
  
  # retrieve measured pheno dates from observations
  tar_target(df_obs_pheno_dates, 
             get_pheno_dates(file_obs_mean, config$date_DOY_ref)),
  
  # add observed stages to Observed excel
  tar_target(df_new_obs, add_stages_to_obs(file_obs_mean,df_obs_pheno_dates)),
  
  # create and add pheno-dates not measured in-between
  tar_target(df_new_pheno_dates, 
             add_interp_pheno_dates(df_obs_pheno_dates, config$btwStgPerc)),
  
  # get Haun stage data for enforced parameters
  tar_target(df_haun, get_column_var_from_observ(file_obs_mean, 
                                                 "Wheat.Phenology.HaunStage")),
  
  # check if haun manual parameters is correct
  tar_target(haun_input_checked, check_manual_params(config$folder_inputs,
                                                      config$file_name_input_haun,
                                                      file_obs_mean)),
  
  # save pheno-date input into excel
  tar_target(
    exported_csv_file,
    save_df_into_csv(
      df = df_new_pheno_dates, 
      folder = config$folder_inputs, 
      filename = config$file_name_input_pheno
    ),
    format = "file"
  ),
  
  # save new mean Observed
  tar_target(
    saved_obs_file,
    save_df_into_excel(
      df = df_new_obs, 
      folder = config$folder_apsimx, 
      filename = config$file_workData_excel,
      sheetname = config$sheet_name_observed
    ),
    format = "file"
  )
  
  
)
