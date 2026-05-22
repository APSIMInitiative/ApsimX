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
    "here", "tidyverse", "lubridate", "readxl"
  )
)

# ------------------------------------------------------------------------------
# 2. SOURCE CUSTOM FUNCTIONS
# ------------------------------------------------------------------------------
# source("R/read_excel_observed.R")
# source("R/do_obs_means.R")
# source("R/save_df_into_excel.R")
# source("R/get_pheno_dates.R")
# source("R/add_interp_pheno_dates.R")
# source("R/save_df_into_csv.R")
# source("R/add_stages_to_obs.R")
# source("R/get_column_var_from_observ.R")
# source("R/check_manual_params.R")
# source("R/check_project_dependencies.R")
# source("R/add_harv_into_obs.R")
# source("R/derive_haun_pheno_dates.R")
# source("R/updatePhenoStageInput.R")
# source("R/copy_and_check_met.R")
# source("R/secure_zip_folder.R")
# source("R/apply_name_corrections_Grass24.R")
# source("R/apply_corrections_Grass24.R")

# Load master scripts (Universal Functions)
targets::tar_source("../targets_MasterScripts")

# Load THIS project's specific local scripts (Wagga local fixes & legacy pheno)
source("R/apply_corrections_Grass24.R")

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
      file_zip_out               = file.path(here::here(), "Observed.zip"), 
      file_pass                  = file.path(here::here(), "secret_pass.txt"), 
      
      file_rawData_excel      = "Observed.xlsx",             # Raw observed data
      file_workData_excel     = paste0(proj_name, "_Observed.xlsx"),
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
  # 2. VALIDATE AND COPY MET FILE
  # ------------------------------------------------------------------
  tar_target(
    name = validated_met_file,
    command = copy_and_check_met(
      sourceFolder   = config$folder_rawData,    # Automatically grabs "data/raw_weather"
      targetFolder   = config$folder_met,        # Where you want it to go
      orig_file_name = config$file_name_met,     # Automatically grabs "GrassPatch_original.met"
      new_file_name  = config$file_name_new_met  # Your new clean name
    ),
    format = "file" # CRITICAL: Tells targets the output is a physical file, not an R object
  ),

  # ----------------------------------------------------------------------------
  # PHASE B: RAW OBSERVATION INGESTION & PROCESSING
  # ----------------------------------------------------------------------------
  # Read raw observations
  tar_target(
    name = df_obs_raw,
    command = read_excel_observed(
      file.path(config$folder_rawData, config$file_rawData_excel)
    )
  ),
  # 
  # Average the replicates
  tar_target(
    name = df_obs_mean,
    command = do_obs_means(df_obs_raw)
  ),
  # 
  # ----------------------------------------------------------------------------
  # PHASE C: PHENOLOGY & HAUN STAGES
  # ----------------------------------------------------------------------------
  
  # Retrieve measured pheno dates from DOY observations of pheno stages
  tar_target(
    name = df_pheno_raw,
    command = get_pheno_dates_from_DOY_wide_obs(
      df_obs_mean,
      config$date_DOY_ref
    )
  ),
  
  # Create and add interpolated pheno-dates not measured in-between
  tar_target(
    name = df_pheno_int,
    command = create_interp_pheno_dates(
      df_pheno_raw,
      config$btwStgPerc
    )
  ),
  
  # Get Haun stage data for enforced parameters
  tar_target(
    name = df_haun,
    command = get_column_var_from_observ(
      df       = df_obs_mean,
      std_cols = c("SimulationName", "Clock.Today"),
      col_name = "Wheat.Phenology.HaunStage"
    )
  ),
  
  # Derive pheno-stages' dates from haun
  tar_target(
    name = df_pheno_haun,
    command = derive_pheno_stages_from_haun(
      df             = df_haun,
      max_leaf_limit = config$max_leaf_limit
    )
  ),
  
  # Merge, quality check and 
  tar_target(
    name = df_pheno_final,
    command = merge_and_qc_pheno(
      df_raw = df_pheno_raw,
      df_haun = df_pheno_haun,
      df_int = df_pheno_int
    )
  ),
  
  # Step 5A: Format into APSIM Wide Parameters
  tar_target(
    name = df_pheno_input_param,
    command = format_apsim_pheno_params(df_pheno_final)
  ),
  
  # Step 5B: Save the Parameter Data to Disk
  tar_target(
    name = file_pheno_param_csv,
    command = {
      out_path <- file.path(config$folder_inputs, config$file_name_input_pheno)
      readr::write_csv(df_pheno_input_param, out_path, na = "")
      out_path
    },
    format = "file"
  )
  #
  # 
  # # # Create and add interpolated pheno-dates not measured in-between
  # # tar_target(
  # #   name = df_pheno_dates_paramInput,
  # #   command = add_interp_pheno_dates(
  # #     df_obs_pheno_dates,
  # #     config$btwStgPerc
  # #   )
  # # )
  # 

  # # 

  # 
  
  # 
  # # Join interpolated and haun-based pheno dates (haun has priority)
  # tar_target(
  #   name = df_apsimStageInput_haunBased,
  #   command = updatePhenoStageInput(
  #     obsIntPheno = df_pheno_dates_paramInput,  # Interpolated observations
  #     haunPheno   = df_haun_pheno_dates         # Haun stage has priority
  #   )
  # ),
  # 
  # # ----------------------------------------------------------------------------
  # # PHASE D: OBSERVATION FORMATTING & INTEGRATION
  # # ----------------------------------------------------------------------------
  # # Add HarvestRipe flags at final measurements for 1:1 graph analysis
  # tar_target(
  #   name = df_observed_wide_harv, 
  #   command = add_harv_into_obs(
  #     df            = df_obs_mean,
  #     ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"), 
  #     new_col_name  = "Wheat.Phenology.CurrentStageName",
  #     new_col_value = "HarvestRipe"
  #   )
  # ),
  # 
  # # Convert pheno dates into obs file friendly input and add to obs timeline
  # tar_target(
  #   name = df_obs_mean_harv_pheno,
  #   command = add_stages_to_obs(
  #     df_obs       = df_observed_wide_harv, 
  #     df_pheno     = df_apsimStageInput_haunBased,
  #     new_var_name = "Wheat.Phenology.Stage"
  #   )
  # ),
  # 
  # # 1. Tell targets to explicitly watch the file on disk for changes
  # tar_target(
  #   name = track_mapping_csv,
  #   command = file.path(config$folder_rawData, config$file_name_mapping_csv),
  #   format = "file" # <- CRITICAL: Tracks the file content hash
  # ),
  # 
  # # 2. Pass the tracked file target path into your function
  # tar_target(
  #   name = df_final_observed_renamed,
  #   command = apply_name_corrections_Grass24(
  #     df_obs           = df_obs_mean_harv_pheno,
  #     mapping_csv_path = track_mapping_csv
  #   )
  # ),
  # 
  # # 2. Pass the tracked file target path into your function
  # tar_target(
  #   name = df_final_observed_renamed_corrected,
  #   command  = apply_corrections_Grass24(
  #     df_obs = df_final_observed_renamed
  #   )
  # ),
  # 
  # # Check if Haun manual parameters are correct
  # tar_target(
  #   name = haun_input_checked, 
  #   command = check_manual_params(
  #     config$folder_inputs,
  #     config$file_name_input_haun,
  #     df_obs_mean
  #   )
  # ),
  # 
  # # ----------------------------------------------------------------------------
  # # PHASE E: EXPORT & VALIDATION
  # # ----------------------------------------------------------------------------
  # # Save pheno-date input into CSV
  # tar_target(
  #   name = msg_pheno_param_saved,
  #   command = save_df_into_csv(
  #     df       = df_apsimStageInput_haunBased, 
  #     folder   = config$folder_inputs, 
  #     filename = config$file_name_input_pheno
  #   ),
  #   format = "file"
  # ),
  # 
  # # Save new mean observed data into Excel
  # tar_target(
  #   name = msg_obs_saved,
  #   command = save_df_into_excel(
  #     df        = df_final_observed_renamed_corrected, 
  #     folder    = config$folder_observed, 
  #     filename  = config$file_workData_excel,
  #     sheetname = config$sheet_name_observed
  #   ),
  #   format = "file"
  # ),
  # 
  # # Post-flight dependency check for APSIM
  # tar_target(
  #   name = check_depend, 
  #   command = {
  #     # 1. Force dependency tracking
  #     msg_obs_saved
  #     msg_pheno_param_saved
  #     haun_input_checked
  #     
  #     # 2. Execute validation
  #     check_project_dependencies(
  #       met_name   = config$file_name_new_met,
  #       projects   = config$proj_name,
  #       dir_met    = config$folder_met,
  #       dir_inputs = config$folder_inputs,
  #       dir_obs    = config$folder_observed
  #     )
  #   }
  # ),
  # 
  # # ----------------------------------------------------------------------------
  # # PHASE G: SECURITY & ZIPPING 
  # # ----------------------------------------------------------------------------
  # 
  # # 1. THE WATCHER: Track every Excel file in the folder.
  # # If any file changes, this target invalidates.
  # tar_target(
  #   name = tracked_excel_files,
  #   command = list.files(config$folder_observed, pattern = "\\.xls[mx]?$", full.names = TRUE),
  #   format = "file"
  # ),
  # 
  # # 2. THE ZIPPER: Only runs if 'tracked_excel_files' detects a change.
  # tar_target(
  #   name = encrypted_zip_artifact,
  #   command = {
  #     force(tracked_excel_files) 
  #     
  #     secure_zip_folder(
  #       input_folder = config$folder_observed, 
  #       output_zip   = config$file_zip_out, 
  #       pass_file    = config$file_pass
  #     )
  #   },
  #   format = "file"
  # )
  # 
)