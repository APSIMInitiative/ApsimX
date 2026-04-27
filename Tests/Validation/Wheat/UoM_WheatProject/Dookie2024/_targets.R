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
#source("R/spread_and_csv_save.R")
source("R/read_and_merge_obs_files.R")
source("R/save_df_to_excel.R")
source("R/check_project_dependencies.R")
source("R/add_harv_into_obs.R")
source("R/derive_haun_pheno_dates.R")
source("R/tidy_up_haun.R")
source("R/spread_pheno_dates.R")
source("R/save_df_into_csv.R")
source("R/updatePhenoStageInput.R")
source("R/add_stages_to_obs.R")

# Read Wheat.Phenology.Stage from Observed (2 sets)
# Organise in spread format
# interpolate for non-available phases
# set a haun manual-input check


#-------------------

proj_name <- "Dookie2024"


# target objects
list(
  # all configuration parameters
  tar_target(
    config,
    list(
      # folders and file names
      proj_name                   = proj_name, 
      folder_thisScript           = here::here(),
      folder_inputs               = here::here("..", "inputs"),
      folder_met                  = here::here("..", "met"),
      folder_apsimx               = here::here(), # a level up from where Analysis is
      folder_rawData              = here::here(proj_name),  
      file_rawData_excel          = c("DookieEVA2024.xlsx",
                                      "DookieWWHI2024.xlsx"), # raw observed data (pre-defined file name),
      file_saved_obs_excel         = paste0(proj_name,"_Observed.xlsx"), #produced observed data (pre-defined file name)
      sheet_name_observed         = "Observed",
      date_DOY_ref                = "01-01-2024", # date to transform DOY output into ddmmyy within simulations
      btwStgFrac                  = 0.5, # fraction of time in-between two pheno-stages when we assume a missing stage 
      file_name_input_pheno       = paste0(proj_name,"_PhenoDatesInput.csv"),
      file_name_input_haun        = paste0(proj_name,"_HaunStagesInput.csv"),
      cols_to_extract             = c("SimulationName",
                                         "Clock.Today",
                                         "Wheat.Phenology.HaunStage",
                                         "Wheat.Phenology.Stage"),
      max_leaf_limit             = 0.95, # Fractional of max leaf number to define date when final leaf appears 
      file_name_cult_by_sowDate  = "CultivarBySowingDatesTemplate.csv"
        )),
  

  # read observations of phenology and haun
  tar_target(file_pheno_haun_obs, read_and_merge_phenology_observed(config$folder_rawData,
                                               config$file_rawData_excel, 
                                               config$sheet_name_observed,
                                               config$cols_to_extract)),
  
  # derive pheno-stages' dates from haun
  
  tar_target(df_haun, tidy_up_haun(file_pheno_haun_obs)), # prepare haun input format
  
  # derive pheno-stages' dates from haun
  tar_target(
    name = df_haun_pheno_dates,
    command = derive_haun_pheno_dates(
      df             = df_haun,
      max_leaf_limit = config$max_leaf_limit
    )
  ),
  

  
  
  # check if haun manual-parameters are correct
  tar_target(msg_haun_input_checked, check_manual_params(config$folder_inputs,
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
  
  # tar_target(msg_pheno_input_saved, spread_and_csv_save(config$folder_inputs, 
  #                                                       config$file_name_input_pheno, 
  #                                                       df_pheno_interp)),
  
  tar_target(
    name = df_pheno_wide, 
    command = spread_pheno_dates(df_pheno_interp)
  ),
  
  # Join interpolated and haun-based pheno dates (haun has priority) ------------
  tar_target(
    name = df_apsimStageInput_haunBased,
    command = updatePhenoStageInput(
      obsIntPheno = df_pheno_wide,  # Interpolated observations
      haunPheno   = df_haun_pheno_dates         # Haun stage has priority
    )
  ),
  
  tar_target(
    name = msg_pheno_csv_saved, 
    command = save_df_into_csv(
      df       = df_apsimStageInput_haunBased, 
      folder   = config$folder_inputs, 
      filename = config$file_name_input_pheno
    ),
    format = "file" # Tells targets to watch the actual physical file!
  ),
  
  tar_target(df_all_obs_files, read_and_merge_obs_files(file.path(config$folder_rawData),
                                                        config$file_rawData_excel,
                                                        config$sheet_name_observed)),
  
  # Add flags of HarvestRipe at final measurements for graphing
  tar_target(
    name = df_final_observed_harv, 
    command = add_harv_into_obs(
      df            = df_all_obs_files,
      ref_vars      = c("Wheat.AboveGround.Wt","Wheat.Grain.Wt"), 
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  # Convert pheno dates into obs file friendly input and add to obs
  tar_target(
    name = df_obs_mean_harv_pheno,
    command = add_stages_to_obs(df_final_observed_harv, 
                                df_apsimStageInput_haunBased,
                                "Wheat.Phenology.Stage")
  ),
  
  
  tar_target(msg_obs_saved,save_df_to_excel(config$folder_apsimx,
                                             config$file_saved_obs_excel, 
                                             config$sheet_name_observed, 
                                            df_obs_mean_harv_pheno)),
  
  # Post-flight dependency check for APSIM
  tar_target(
    name = check_depend, 
    command = {
      # 1. List the targets here to force `{targets}` to wait for them
      msg_obs_saved
      msg_haun_input_checked
      msg_pheno_csv_saved
      
      # 2. Now run the actual function
      check_project_dependencies(
        projects = config$proj_name,
        dir_met = config$folder_met,
        dir_inputs = config$folder_inputs,
        dir_obs = config$folder_apsimx
      )
    }
  )
  
  
)
