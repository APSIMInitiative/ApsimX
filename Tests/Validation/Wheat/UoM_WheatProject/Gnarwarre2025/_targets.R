# ==============================================================================
# APSIM-X DATA PIPELINE: Gnarwarre2025
# ==============================================================================
# Description: Base simulation and observed data copied from FAR Australia.
# Author: Edmar Teixeira (edmar.teixeira@plantandfood.co.nz)
# Data provider: Ben Jones @ FAR Australia (ben.jones@faraustralia.com.au)
# Date: 2026-02-09
#
# NOTE: Incomplete - awaits raw data availability.
# Obs data for phenology stages 6 and 8 is required to continue development.
# ==============================================================================

library(targets)
library(here)

# ------------------------------------------------------------------------------
# 1. GLOBAL SETTINGS & PACKAGES
# ------------------------------------------------------------------------------
tar_option_set(
  packages = c("here", "tidyverse", "lubridate", "readxl")
)

# ------------------------------------------------------------------------------
# 2. SOURCE CUSTOM FUNCTIONS
# ------------------------------------------------------------------------------
source("R/read_excel_observed.R")
source("R/do_obs_means.R")
source("R/save_df_into_excel.R")
source("R/save_df_into_csv.R")
source("R/get_pheno_dates.R")
source("R/check_manual_params.R")
source("R/check_project_dependencies.R")
source("R/add_harv_into_obs.R")
source("R/add_interp_pheno_dates.R")
source("R/add_stages_to_obs.R")
source("R/add_mock_pheno_dates.R") # ---- TEMPORARY TO BE REMOVED ----
source("R/get_column_var_from_observ.R")
source("R/derive_haun_pheno_dates.R")
source("R/updatePhenoStageInput.R")

# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "Gnarwarre2025"

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
      proj_name               = proj_name,
      folder_thisScript       = here::here(),
      folder_rawData          = here::here(proj_name),       # Cloud source
      folder_inputs           = here::here("..", "inputs"),
      folder_apsimx           = here::here(),                # One level up from Analysis
      folder_met              = here::here("..", "met"),
      
      file_rawData_excel      = paste0(proj_name, "/Observed.xlsx"), 
      file_workData_excel     = paste0(proj_name, "_Observed.xlsx"), 
      sheet_name_observed     = "Observed",
      
      # Model parameters
      date_DOY_ref            = "01-01-2025", # Transform DOY output into ddmmyy
      btwStgPerc              = 0.5,          # Fraction of time in-between stages
      ref_yield_var           = "Wheat.AboveGround.Wt", 
      max_leaf_limit          = 0.95,         # Fractional limit for max leaves (Haun)
      
      # Output file names & Metadata
      file_name_input_pheno   = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun    = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_met           = "Gnarwarre_-38.20_144.05_2025.met", 
      file_mock_pheno_csv     = "mock_pheno_dates.csv" # Temporary FIX
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE B: RAW OBSERVATION INGESTION & PROCESSING
  # ----------------------------------------------------------------------------
  # Read raw observations
  tar_target(
    name = df_obs_raw, 
    command = read_excel_observed(
      file.path(config$folder_apsimx, config$file_rawData_excel)
    )
  ),
  
  # Average the replicates
  tar_target(
    name = df_obs_mean, 
    command = do_obs_means(df_obs_raw)
  ),
  
  # ============================================================================
  # ---> DEVELOPMENT / TEMPORARY MOCK BLOCK <---
  # Add MOCK pheno stages (emergence + flower) which are missing.
  # Remove this target and switch the downstream input back to df_obs_mean 
  # once real data is available.
  #
  # tar_target(
  #   name = df_obs_mean_with_mock_pheno, 
  #   command = add_mock_pheno_dates(df_obs_mean, config$file_mock_pheno_csv)
  # ),
  # ============================================================================
  
  # ----------------------------------------------------------------------------
  # PHASE C: PHENOLOGY & HAUN PRIORITY
  # ----------------------------------------------------------------------------
  # Retrieve measured pheno dates from observations
  tar_target(
    name = df_obs_pheno_dates, 
    command = get_pheno_dates(
      df_obs_mean, 
      config$date_DOY_ref
    )
    # command = get_pheno_dates(df_obs_mean_with_mock_pheno, config$date_DOY_ref) # (Use while mocking)
  ),
  
  # Create and add interpolated pheno-dates not measured in-between
  tar_target(
    name = df_pheno_dates_paramInput, 
    command = add_interp_pheno_dates(
      df_obs_pheno_dates, 
      config$btwStgPerc
    )
  ),
  
  # Isolate Haun observations
  tar_target(
    name = df_haun, 
    command = get_column_var_from_observ(
      df_obs_mean, 
      "Wheat.Phenology.HaunStage"
    )
  ),
  
  # Derive pheno-stages' dates from Haun
  tar_target(
    name = df_haun_pheno_dates,
    command = derive_haun_pheno_dates(
      df             = df_haun,
      max_leaf_limit = config$max_leaf_limit
    )
  ),
  
  # Join interpolated and Haun-based pheno dates (Haun has priority)
  tar_target(
    name = df_apsimStageInput_haunBased,
    command = updatePhenoStageInput(
      obsIntPheno = df_pheno_dates_paramInput,  # Interpolated observations
      haunPheno   = df_haun_pheno_dates         # Haun stage has priority
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: OBSERVATION FORMATTING & CORRECTIONS
  # ----------------------------------------------------------------------------
  # Add HarvestRipe flags at final measurements
  tar_target(
    name = df_final_observed_harv, 
    command = add_harv_into_obs(
      df            = df_obs_mean,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"), 
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  # Convert fully merged pheno dates into obs file friendly input and add to obs timeline
  tar_target(
    name = df_final_observed_harv_pheno, 
    command = add_stages_to_obs(
      df_obs       = df_final_observed_harv,
      df_pheno     = df_apsimStageInput_haunBased, # FIX: use complete Haun-prioritized dates
      new_var_name = "Wheat.Phenology.Stage"
    )
  ),
  
  # Check if Haun manual parameters are correct
  tar_target(
    name = haun_input_checked, 
    command = check_manual_params(
      config$folder_inputs,
      config$file_name_input_haun,
      df_obs_mean
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: EXPORT & VALIDATION
  # ----------------------------------------------------------------------------
  # Save Haun-prioritized pheno-date input into CSV
  tar_target(
    name = msg_pheno_param_saved,
    command = save_df_into_csv(
      df       = df_apsimStageInput_haunBased, 
      folder   = config$folder_inputs, 
      filename = config$file_name_input_pheno
    ),
    format = "file"
  ),
  
  # Save new mean Observed data to Excel
  tar_target(
    name = msg_obs_saved,
    command = save_df_into_excel(
      df        = df_final_observed_harv_pheno,
      folder    = config$folder_apsimx,
      filename  = config$file_workData_excel,
      sheetname = config$sheet_name_observed
    ),
    format = "file"
  ),
  
  # Post-flight dependency check for APSIM
  tar_target(
    name = check_depend, 
    command = {
      # 1. Force dependency tracking
      msg_obs_saved
      msg_pheno_param_saved
      haun_input_checked
      
      # 2. Execute validation
      check_project_dependencies(
        met_name   = config$file_name_met,
        projects   = config$proj_name,
        dir_met    = config$folder_met,
        dir_inputs = config$folder_inputs,
        dir_obs    = config$folder_apsimx
      )
    }
  )
  
)