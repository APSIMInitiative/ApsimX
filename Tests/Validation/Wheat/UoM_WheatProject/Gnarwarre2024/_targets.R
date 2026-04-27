# ==============================================================================
# APSIM-X DATA PIPELINE: Gnarwarre2024
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
source("R/get_pheno_dates.R")
source("R/check_manual_params.R")
source("R/check_project_dependencies.R")
source("R/add_stages_to_obs.R")
source("R/get_harvestRipe_dates.R")
source("R/add_harv_into_obs.R")
source("R/add_interp_pheno_dates.R")
source("R/save_df_into_csv.R")
source("R/apply_tailored_obs_corrections.R")

# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "Gnarwarre2024"

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
      date_DOY_ref            = "01-01-2024", # Transform DOY output into ddmmyy
      btwStgPerc              = 0.5,          # Fraction of time in-between stages
      ref_yield_var           = "Wheat.AboveGround.Wt", 
      
      # Output file names & Metadata
      file_name_input_pheno   = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun    = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_met           = "Gnarwarre_-38.20_144.05_2024.met" 
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
  
  # ----------------------------------------------------------------------------
  # PHASE C: PHENOLOGY STAGES
  # ----------------------------------------------------------------------------
  # Retrieve measured pheno dates from observations
  tar_target(
    name = df_obs_pheno_dates, 
    command = get_pheno_dates(df_obs_mean, config$date_DOY_ref)
  ),
  
  # Create and add interpolated pheno-dates not measured in-between
  tar_target(
    name = df_new_pheno_dates, 
    command = add_interp_pheno_dates(df_obs_pheno_dates, config$btwStgPerc)
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: OBSERVATION FORMATTING & CORRECTIONS
  # ----------------------------------------------------------------------------
  # Add HarvestRipe flags at final measurements
  tar_target(
    name = df_obs_with_harvDate, 
    command = add_harv_into_obs(
      df            = df_obs_mean,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"), 
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  # Apply tailored needed corrections (project-specific)
  tar_target(
    name = df_new_obs_fixed, 
    command = apply_tailored_obs_corrections(df_obs_with_harvDate)
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
  # Save pheno-date input into CSV
  tar_target(
    name = msg_pheno_param_saved,
    command = save_df_into_csv(
      df       = df_new_pheno_dates, 
      folder   = config$folder_inputs, 
      filename = config$file_name_input_pheno
    ),
    format = "file"
  ),
  
  # Save new mean Observed data to Excel
  tar_target(
    name = msg_obs_saved,
    command = save_df_into_excel(
      df        = df_new_obs_fixed,
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