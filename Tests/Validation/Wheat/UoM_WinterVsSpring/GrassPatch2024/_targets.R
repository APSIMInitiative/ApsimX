# ==============================================================================
# APSIM-X DATA PIPELINE: GrassPatch2024
# ==============================================================================
# Description: Base simulation and observed data copied from FAR Australia.
# Goal: Average reps, force fit Haun stage and phenology dates as model inputs.
# Author: Edmar Teixeira
# Data provider: Ben Jones @ FAR Australia (ben.jones@faraustralia.com.au)
# Date: 2026-02-09
# ==============================================================================

library(targets)
library(here)

# ------------------------------------------------------------------------------
# 1. GLOBAL SETTINGS & PACKAGES
# ------------------------------------------------------------------------------
tar_option_set(
  packages = c(
    "here", "tidyverse", "lubridate", "readxl", "openxlsx"
  )
)

# ------------------------------------------------------------------------------
# 2. SOURCE CUSTOM FUNCTIONS
# ------------------------------------------------------------------------------
# Load master scripts (Universal Functions)
targets::tar_source("../targets_MasterScripts")

# Load THIS project's specific local scripts (Local fixes & mapping)
source("R/apply_corrections_Grass24.R")
source("R/apply_name_corrections_Grass24.R")

# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "GrassPatch2024"

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
      folder_apsimx           = here::here(),                # One level up from Analysis
      folder_met              = here::here("Met"),
      folder_inputs           = here::here("Inputs"),
      folder_observed         = file.path(here::here(), "Observed"),
      
      # Security
      file_zip_out            = file.path(here::here(), "Observed.zip"), 
      file_pass               = file.path(here::here(), "secret_pass.txt"), 
      
      file_rawData_excel      = "Observed.xlsx",             # Raw observed data
      file_saved_obs_excel    = paste0(proj_name, "_Observed.xlsx"), 
      sheet_name_observed     = "Observed",
      
      # Model parameters
      date_DOY_ref            = "01-01-2024", # Transform DOY output into ddmmyy
      btwStgPerc              = 0.5,          # Fraction of time in-between stages
      max_leaf_limit          = 0.95,         # Fractional limit for max leaves (Haun)
      
      # Output file names & Metadata
      file_name_input_pheno   = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun    = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_met           = "Grass Patch_-33.25_121.60.met",
      file_name_mapping_csv   = paste0(proj_name, "_obs_var_new_names.csv"),
      file_name_new_met       = paste0(proj_name, ".met")
    )
  ),
  
  # ------------------------------------------------------------------
  # PHASE B: VALIDATE AND COPY MET FILE
  # ------------------------------------------------------------------
  tar_target(
    name = validated_met_file,
    command = copy_and_check_met(
      sourceFolder   = config$folder_rawData,    # Automatically grabs "data/raw_weather"
      targetFolder   = config$folder_met,        # Where you want it to go
      orig_file_name = config$file_name_met,     # Automatically grabs "GrassPatch_original.met"
      new_file_name  = config$file_name_new_met  # Your new clean name
    ),
    format = "file" # CRITICAL: Tells targets the output is a physical file
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE C: RAW OBSERVATION INGESTION & PROCESSING
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_obs_raw,
    command = read_excel_observed(
      file.path(config$folder_rawData, config$file_rawData_excel)
    )
  ),
  
  tar_target(
    name = df_obs_mean,
    command = do_obs_means(df_obs_raw)
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY & HAUN STAGES (Universal)
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_pheno_raw,
    command = get_pheno_dates_from_DOY_wide_obs(
      df_obs_mean,
      config$date_DOY_ref
    )
  ),
  
  tar_target(
    name = df_pheno_int,
    command = create_interp_pheno_dates(
      df_pheno_raw,
      config$btwStgPerc
    )
  ),
  
  tar_target(
    name = df_haun,
    command = get_column_var_from_observ(
      df       = df_obs_mean,
      std_cols = c("SimulationName", "Clock.Today"),
      col_name = "Wheat.Phenology.HaunStage"
    )
  ),
  
  tar_target(
    name = df_pheno_haun,
    command = derive_pheno_stages_from_haun(
      df_input       = df_haun,
      max_leaf_limit = config$max_leaf_limit
    )
  ),
  
  tar_target(
    name = df_pheno_final,
    command = merge_and_qc_pheno(
      df_raw  = df_pheno_raw,
      df_haun = df_pheno_haun,
      df_int  = df_pheno_int
    )
  ),
  
  tar_target(
    name = df_pheno_input_param,
    command = format_apsim_pheno_params(df_pheno_final)
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: OBSERVATION FORMATTING & INTEGRATION
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_obs_plus_pheno,
    command = add_new_var_to_obs(
      df_obs          = df_obs_mean,
      df_new_data     = df_pheno_final,
      target_col_name = "Wheat.Phenology.Stage"
    )
  ),
  
  tar_target(
    name = df_obs_plus_pheno_harv,
    command = add_harv_into_obs(
      df            = df_obs_plus_pheno,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"),
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  tar_target(
    name = track_mapping_csv,
    command = file.path(config$folder_rawData, config$file_name_mapping_csv),
    format = "file" 
  ),
  
  tar_target(
    name = df_obs_plus_pheno_harv_renamed,
    command = apply_name_corrections_Grass24(
      df_obs           = df_obs_plus_pheno_harv,
      mapping_csv_path = track_mapping_csv
    )
  ),
  
  tar_target(
    name = df_obs_plus_pheno_harv_renamed_corrected,
    command  = apply_corrections_Grass24(
      df_obs = df_obs_plus_pheno_harv_renamed
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: VALIDATION & EXPORT  
  # ----------------------------------------------------------------------------
  # THE QC GATEKEEPER
  tar_target(
    name = qc_apsim_observed_harv,
    command = check_obs_health(df_obs_plus_pheno_harv_renamed_corrected)
  ),
  
  # Haun Check (Enforces DAG dependency on the QC Gate)
  tar_target(
    name = haun_input_checked,
    command = check_manual_params(
      config$folder_inputs,
      config$file_name_input_haun,
      qc_apsim_observed_harv
    )
  ),
  
  tar_target(
    name = msg_pheno_param_saved,
    command = save_df_into_csv(
      df       = df_pheno_input_param,
      folder   = config$folder_inputs,
      filename = config$file_name_input_pheno
    ),
    format = "file"
  ),
  
  tar_target(
    name = msg_obs_saved,
    command = save_df_to_excel(
      df          = qc_apsim_observed_harv,
      folder_path = config$folder_observed,
      file_name   = config$file_saved_obs_excel,
      sheet_name  = config$sheet_name_observed
    ),
    format = "file"
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE G: SECURITY & ZIPPING
  # ----------------------------------------------------------------------------
  tar_target(
    name = tracked_excel_files,
    command = list.files(config$folder_observed, pattern = "\\.xls[mx]?$", full.names = TRUE),
    format = "file"
  ),
  
  tar_target(
    name = encrypted_zip_artifact,
    command = {
      force(tracked_excel_files)
      
      secure_zip_folder(
        input_folder = config$folder_observed,
        output_zip   = config$file_zip_out,
        pass_file    = config$file_pass
      )
      
      config$file_zip_out # Satisfies format = "file"
    },
    format = "file"
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE H: PRE-FLIGHT & DEPENDENCY CHECKS
  # ----------------------------------------------------------------------------
  tar_target(
    name = check_depend,
    command = {
      force(validated_met_file) # Ensure met file exists before checking
      msg_obs_saved
      msg_pheno_param_saved
      haun_input_checked
      
      check_project_dependencies(
        met_name   = config$file_name_new_met,
        projects   = config$proj_name,
        dir_met    = config$folder_met,
        dir_inputs = config$folder_inputs,
        dir_obs    = config$folder_observed
      )
    }
  ),
  
  tar_target(
    name = verify_data_backup,
    command = {
      force(check_depend)
      
      check_archive_sync(
        target_folder = config$folder_observed,
        zip_file      = config$file_zip_out
      )
    }
  )
)