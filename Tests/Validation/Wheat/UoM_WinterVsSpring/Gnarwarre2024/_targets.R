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
# 2. SOURCE CUSTOM FUNCTIONS (UNIVERSAL INGESTOR)
# ------------------------------------------------------------------------------
# This single command safely loads EVERY .R file inside the "R/" directory.
# (If you create a central hub later, change "R" to "C:/github/APSIM_Master_Scripts")
targets::tar_source("R") 

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
      proj_name                = proj_name,
      folder_thisScript        = here::here(),
      folder_rawData           = here::here(proj_name),        # Cloud source
      folder_apsimx            = here::here(),                 # One level up from Analysis
      folder_met               = here::here("Met"),
      folder_inputs            = here::here("Inputs"),
      folder_observed          = file.path(here::here(), "Observed"),
      
      # Security
      file_zip_out             = file.path(here::here(), "Observed.zip"), 
      file_pass                = file.path(here::here(), "secret_pass.txt"),
      
      file_rawData_excel       = paste0(proj_name, "/Observed.xlsx"), 
      file_workData_excel      = paste0(proj_name, "_Observed.xlsx"), 
      sheet_name_observed      = "Observed",
      
      # Model parameters
      date_DOY_ref             = "01-01-2024", # Transform DOY output into ddmmyy
      btwStgPerc               = 0.5,          # Fraction of time in-between stages
      ref_yield_var            = "Wheat.AboveGround.Wt", 
      max_leaf_limit           = 0.95,         # Fractional limit for max leaves (Haun)
      
      # Output file names & Metadata
      file_name_input_pheno    = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun     = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_new_met        = paste0(proj_name, ".met"),
      file_name_met            = "Gnarwarre_-38.20_144.05.met" 
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE B: MET DATA VALIDATION & INGESTION
  # ----------------------------------------------------------------------------
  # 1. Track the raw physical file so updates trigger a pipeline rerun
  tar_target(
    name = raw_met_tracker,
    command = file.path(config$folder_rawData, config$file_name_met),
    format = "file"
  ),
  
  # 2. Validate and copy
  tar_target(
    name = validated_met_file,
    command = {
      force(raw_met_tracker) # Explicitly bind to the file tracker
      
      copy_and_check_met(
        sourceFolder   = config$folder_rawData,
        targetFolder   = config$folder_met,
        orig_file_name = config$file_name_met,
        new_file_name  = config$file_name_new_met
      )
    },
    format = "file"
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE C: RAW OBSERVATION INGESTION & PROCESSING
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
  # PHASE D: PHENOLOGY & HAUN PRIORITY
  # ----------------------------------------------------------------------------
  # Retrieve measured pheno dates from observations
  tar_target(
    name = df_obs_pheno_dates, 
    command = get_pheno_dates(
      df_obs_mean, 
      config$date_DOY_ref
    )
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
  
  # Check and create (if-needed) external-haun-parameter input file 
  tar_target(
    name = haun_input_checked, 
    command = check_manual_params(
      config$folder_inputs,
      config$file_name_input_haun,
      df_obs_mean
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: OBSERVATION FORMATTING & CORRECTIONS
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
  
  # Convert pheno dates into obs file friendly input and add to obs timeline
  tar_target(
    name = df_obs_mean_harv_pheno,
    command = add_stages_to_obs(
      df_obs       = df_new_obs_fixed, 
      df_pheno     = df_apsimStageInput_haunBased,
      new_var_name = "Wheat.Phenology.Stage"
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: EXPORT & VALIDATION
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
      df        = df_obs_mean_harv_pheno,
      folder    = config$folder_observed,
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
        met_name   = config$file_name_new_met,
        projects   = config$proj_name,
        dir_met    = config$folder_met,
        dir_inputs = config$folder_inputs,
        dir_obs    = config$folder_observed
      )
    }
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE G: SECURITY & ZIPPING 
  # ----------------------------------------------------------------------------
  
  # 1. THE WATCHER: Track every Excel file in the folder.
  tar_target(
    name = tracked_excel_files,
    command = {
      force(msg_obs_saved) # Fix: Force Watcher to wait until Phase F finishes exporting
      list.files(config$folder_observed, pattern = "\\.xls[mx]?$", full.names = TRUE)
    },
    format = "file"
  ),
  
  # 2. THE ZIPPER: Only runs if 'tracked_excel_files' detects a change.
  tar_target(
    name = encrypted_zip_artifact,
    command = {
      force(tracked_excel_files) 
      
      secure_zip_folder(
        input_folder = config$folder_observed, 
        output_zip   = config$file_zip_out, 
        pass_file    = config$file_pass
      )
    },
    format = "file"
  )
)