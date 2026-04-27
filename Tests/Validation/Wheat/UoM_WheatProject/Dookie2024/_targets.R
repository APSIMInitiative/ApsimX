# ==============================================================================
# APSIM-X DATA PIPELINE: Dookie2024
# ==============================================================================
# Description: Pipeline to process Wheat phenology and Haun stage observations.
# Goal: Interpolate missing stages, enforce Haun-derived dates, format for APSIM,
#       and merge discrete phenology events back into continuous observations.
# ==============================================================================

library(targets)
library(here)

# ------------------------------------------------------------------------------
# 1. GLOBAL SETTINGS & PACKAGES
# ------------------------------------------------------------------------------
tar_option_set(
  packages = c(
    "here", "tidyverse", "lubridate", "readxl"
  )
)

# ------------------------------------------------------------------------------
# 2. SOURCE CUSTOM FUNCTIONS
# ------------------------------------------------------------------------------
source("R/read_and_merge_phenology_observed.R")
source("R/check_manual_params.R")
source("R/read_csv_sowDates.R")
source("R/find_first_stage_dates.R")
source("R/interpolate_and_create_phenoStages.R")
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

# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "Dookie2024"

# ==============================================================================
# PIPELINE TARGETS
# ==============================================================================
list(
  
  # ----------------------------------------------------------------------------
  # PHASE A: CONFIGURATION & METADATA
  # ----------------------------------------------------------------------------
  tar_target(
    name = config,
    command = list(
      # Folders and file names
      proj_name                  = proj_name, 
      folder_thisScript          = here::here(),
      folder_inputs              = here::here("..", "inputs"),
      folder_met                 = here::here("..", "met"),
      folder_apsimx              = here::here(), # One level up from Analysis
      folder_rawData             = here::here(proj_name),  
      file_rawData_excel         = c("DookieEVA2024.xlsx", "DookieWWHI2024.xlsx"),
      file_saved_obs_excel       = paste0(proj_name, "_Observed.xlsx"), 
      sheet_name_observed        = "Observed",
      
      # Model parameters
      date_DOY_ref               = "01-01-2024", # Transform DOY output into ddmmyy
      btwStgFrac                 = 0.5,          # Fraction of time in-between stages
      max_leaf_limit             = 0.95,         # Fraction of max leaf number to define final leaf date 
      
      # Column extractions
      cols_to_extract            = c(
        "SimulationName",
        "Clock.Today",
        "Wheat.Phenology.HaunStage",
        "Wheat.Phenology.Stage"
      ),
      
      # Output file names & Metadata
      file_name_input_pheno      = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun       = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_cult_by_sowDate  = "CultivarBySowingDatesTemplate.csv"
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE B: RAW OBSERVATION INGESTION
  # ----------------------------------------------------------------------------
  # Read observations specifically for phenology and Haun processing
  tar_target(
    name = file_pheno_haun_obs, 
    command = read_and_merge_phenology_observed(
      config$folder_rawData,
      config$file_rawData_excel, 
      config$sheet_name_observed,
      config$cols_to_extract
    )
  ),
  
  # Read the full dataset to be used for the final master merge later
  tar_target(
    name = df_all_obs_files, 
    command = read_and_merge_obs_files(
      file.path(config$folder_rawData),
      config$file_rawData_excel,
      config$sheet_name_observed
    )
  ),
  
  # Prepare sowing dates metadata
  tar_target(
    name = df_sowDates_by_sim, 
    command = read_csv_sowDates(
      config$folder_rawData,
      config$file_name_cult_by_sowDate
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE C: PHENOLOGY INTERPOLATION & HAUN DERIVATION
  # ----------------------------------------------------------------------------
  # 1. Haun Stage Pathway
  tar_target(
    name = df_haun, 
    command = tidy_up_haun(file_pheno_haun_obs) # Prepare Haun input format
  ),
  
  tar_target(
    name = df_haun_pheno_dates,
    command = derive_haun_pheno_dates(
      df             = df_haun,
      max_leaf_limit = config$max_leaf_limit
    )
  ),
  
  tar_target(
    name = msg_haun_input_checked, 
    command = check_manual_params(
      config$folder_inputs,
      config$file_name_input_haun,
      file_pheno_haun_obs
    )
  ),
  
  # 2. General Phenology Pathway
  tar_target(
    name = df_pheno_start_date, 
    command = find_first_stage_dates(
      df_sowDates_by_sim, 
      file_pheno_haun_obs, 
      sow_date_col = "SowingDate"
    )
  ),
  
  tar_target(
    name = df_pheno_interp, 
    command = interpolate_and_create_phenoStages(
      df_pheno_start_date, 
      config$btwStgFrac
    )
  ),
  
  tar_target(
    name = df_pheno_wide, 
    command = spread_pheno_dates(df_pheno_interp)
  ),
  
  # 3. Merge Pathways (Haun priority)
  tar_target(
    name = df_apsimStageInput_haunBased,
    command = updatePhenoStageInput(
      obsIntPheno = df_pheno_wide,        # Interpolated observations
      haunPheno   = df_haun_pheno_dates   # Haun stage has priority
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: FINAL OBSERVATION FORMATTING
  # ----------------------------------------------------------------------------
  # Add HarvestRipe flags at final measurements
  tar_target(
    name = df_final_observed_harv, 
    command = add_harv_into_obs(
      df            = df_all_obs_files,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"), 
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  # Convert pheno dates into discrete event markers and bind to observations
  tar_target(
    name = df_obs_mean_harv_pheno,
    command = add_stages_to_obs(
      df_obs       = df_final_observed_harv, 
      df_pheno     = df_apsimStageInput_haunBased,
      new_var_name = "Wheat.Phenology.Stage"
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: EXPORT & VALIDATION
  # ----------------------------------------------------------------------------
  tar_target(
    name = msg_pheno_csv_saved, 
    command = save_df_into_csv(
      df       = df_apsimStageInput_haunBased, 
      folder   = config$folder_inputs, 
      filename = config$file_name_input_pheno
    ),
    format = "file" 
  ),
  
  tar_target(
    name = msg_obs_saved,
    command = save_df_to_excel(
      config$folder_apsimx,
      config$file_saved_obs_excel, 
      config$sheet_name_observed, 
      df_obs_mean_harv_pheno
    ),
    format = "file" 
  ),
  
  # Post-flight dependency check for APSIM
  tar_target(
    name = check_depend, 
    command = {
      # 1. Force dependency tracking
      msg_obs_saved
      msg_haun_input_checked
      msg_pheno_csv_saved
      
      # 2. Execute validation
      check_project_dependencies(
        projects   = config$proj_name,
        dir_met    = config$folder_met,
        dir_inputs = config$folder_inputs,
        dir_obs    = config$folder_apsimx
      )
    }
  )
  
)