# ==============================================================================
# APSIM-X DATA PIPELINE
# ==============================================================================
# Description: {targets} pipeline for processing raw experimental data, extracting
# synthetic phenology stages, formatting soil/met inputs, and generating the 
# final '_Observed.xlsx' file for APSIM-X injection.
# ==============================================================================

library(targets)
library(rstudioapi)
library(here)

# ------------------------------------------------------------------------------
# 1. GLOBAL SETTINGS & PACKAGES
# ------------------------------------------------------------------------------
tar_option_set(
  packages = c(
    "tidyverse", "lubridate", "purrr", "openxlsx", 
    "readxl", "glue", "rstudioapi", "stringr", 
    "tidyr", "jsonlite"
  )
)

# ------------------------------------------------------------------------------
# 2. SOURCE CUSTOM FUNCTIONS
# ------------------------------------------------------------------------------
# Load master scripts
targets::tar_source("../targets_MasterScripts")
# Load THIS project's specific local scripts (e.g., local fixes)
targets::tar_source("R")


# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "Turretfield2024"

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
      proj_name              = proj_name,
      folder_main            = here::here(),
      folder_rawData         = here::here(proj_name),
      folder_met             = here::here("Met"),
      folder_inputs          = here::here("Inputs"),
      folder_observed        = file.path(here::here(), "Observed"),
      
      file_rawData_excel     = "Turretfield_RawData_2024.xlsx", 
      file_saved_obs_excel   = paste0(proj_name, "_Observed.xlsx"), 
      file_SimNameByCultivar = paste0(proj_name, "_CultivarToSimName.csv"), 
      file_metaData_observed = paste0(proj_name, "_observed_data_requirements.csv"),
      ref_date               = "01/01/2024", # Target year anchor
      
      # Excel sheet names used from raw data
      sheetExcel_weather     = "Weather",
      sheetExcel_haun        = "Haun stage ", # Retains raw data typo
      sheetExcel_soilWater   = "Soil sampling",
      
      # Security
      file_zip_out           = file.path(here::here(), "Observed.zip"), 
      file_pass              = file.path(here::here(), "secret_pass.txt"),
      
      # Model parameters
      coord_thisLatLon       = data.frame(lat = -34.5435, lon = 138.8444),
      target_stagePerc       = 50,     # % of stage development when event date is retrieved
      target_betwStages      = 50,     # % of period between adjacent events for synthetic dates
      var_name_stage         = "apsim_stage_raw",       # Synthetic var with observed PCSD data
      varName_addedToObserv  = "Wheat.Phenology.Stage", # Synthetic var added into observations
      max_leaf_limit         = 0.95,   # Fractional max leaves assumed when terminal spikelet is set
      
      # Output file names
      file_name_input_pheno  = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun   = paste0(proj_name, "_HaunStagesInput.csv")
    )
  ),
  
  tar_target(
    name = config_soil,
    command = list(
      folder_rawData  = config$folder_rawData,       
      file_excel      = config$file_rawData_excel, 
      sheet_name      = "Soil sampling",
      rep_col         = "Block",
      col_depth_from  = "Depth From",
      col_depth_to    = "Depth To",
      target_vars     = c("Bulk density", "LL", "Soil moisture", 
                          "Available water", "Nitrate Nitrogen", "Ammonium Nitrogen") 
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE B: SOIL & WEATHER PROCESSING
  # ----------------------------------------------------------------------------
  
  # Track the raw soil file independently
  tar_target(
    name = raw_soil_tracker,
    command = file.path(config_soil$folder_rawData, config_soil$file_excel),
    format = "file"
  ),
  
  tar_target(
    name = df_soil_profile_clean,
    command = {
      force(raw_soil_tracker)
      
      process_soil_profile(
        folder_name    = config_soil$folder_rawData,
        file_name      = config_soil$file_excel,
        sheet_name     = config_soil$sheet_name,
        var_list       = config_soil$target_vars,
        rep_name       = config_soil$rep_col,
        col_depth_from = config_soil$col_depth_from,
        col_depth_to   = config_soil$col_depth_to
      )
    }
  ),
  
  tar_target(
    name = processed_met_data,
    command = createWeatherFile(
      thisFolder    = config$folder_rawData,
      thisExcelFile = config$file_rawData_excel,
      thisSheet     = config$sheetExcel_weather
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE C: OBSERVATION DATA INGESTION
  # ----------------------------------------------------------------------------
  
  # 1. FILE TRACKING 
  tar_target(
    name = tracked_raw_excel,
    command = file.path(config$folder_rawData, config$file_rawData_excel),
    format = "file"
  ),
  
  # 2. LOAD METADATA
  tar_target(
    name = df_obs_meta_data,
    command = read.csv(
      file.path(config$folder_rawData, config$file_metaData_observed),
      header = TRUE, 
      stringsAsFactors = FALSE, 
      sep = ","
    )
  ),
  
  # 3. LOAD CULTIVAR MAPPING
  tar_target(
    name = df_simNameByCult,
    command = read.csv2(
      file.path(config$folder_rawData, config$file_SimNameByCultivar),
      header = TRUE, 
      stringsAsFactors = FALSE, 
      sep = ","
    )
  ),
  
  # 4. THE UNIVERSAL COMPILER
  tar_target(
    name = list_observed_dfs_raw,
    command = {
      # The force() command ensures {targets} rebuilds this if the raw Excel file changes on your computer
      force(tracked_raw_excel) 
      
      compile_all_obs_by_one_key(
        folder      = config$folder_rawData,
        excel_files = config$file_rawData_excel,
        df_obs_info = df_obs_meta_data,
        df_simNames = df_simNameByCult,
        unique_key  = "Plot"  # <--- Change this to "Plot" if running Fords2025!
      )
    }
  ),
  
  # 5. THE LOCAL INTERCEPTOR (Project-Specific Fixes)
  tar_target(
    name = list_observed_dfs_clean,
    command = apply_local_fixes(
      compiled_obs = list_observed_dfs_raw,
      df_obs_info  = df_obs_meta_data,
      ref_date     = config$ref_date
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY STAGE SYNTHESIS
  # ----------------------------------------------------------------------------
  # TO BE ADDED LATER - NO PHENO_STAGE DATA AVAILABLE YET (2026-05-17)
  
  # ----------------------------------------------------------------------------
  # PHASE E: FINAL OBSERVATION FORMATTING
  # ----------------------------------------------------------------------------
  tar_target(
    name = final_apsim_observed,
    command = prepare_apsim_observed(
      compiled_obs = list_observed_dfs_clean,
      dfs_out      = c("weather_qc_checks") # Datasets to exclude
    )
  ),
  
  tar_target(
    name = df_obs_plus_hi,
    command = calc_harvest_index(
      df          = final_apsim_observed,
      grain_col   = "Wheat.Grain.Wt",
      agb_col     = "Wheat.AboveGround.Wt",
      hi_col_name = "HarvestIndex"
    )
  ),
  
  tar_target(
    name = df_obs_plus_hi_amounts,
    command = calc_nutrient_absolute_amounts(
      df           = df_obs_plus_hi,
      crop_prefix  = "Wheat",
      organs       = c("Leaf.Live", "Leaf.Dead", "Stem.Live", "Spike.Live"),
      conc_targets = c("N" = "NConc", "WSC" = "WSCc"),
      mass_suffix  = "Wt",
      ag_name      = "Wheat.AboveGround",
      divisor      = 1
    )
  ),

  
  # Flag the final measurement dates as "HarvestRipe"
  tar_target(
    name = df_obs_plus_hi_amounts_harv, 
    command = add_harv_into_obs(
      df            = df_obs_plus_hi_amounts,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"), 
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  # THE QC GATEKEEPER
  tar_target(
    name = qc_apsim_observed,
    command = check_obs_health(df_obs_plus_hi_amounts_harv) # Stops the pipeline if it fails!
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: OUTPUT GENERATION & VALIDATION
  # ----------------------------------------------------------------------------
  
  # 1. EXPORT WEATHER TO MET FOLDER
  tar_target(
    name = msg_met_saved,
    command = save_met_file(
      met_list    = processed_met_data,
      folder_path = config$folder_met,
      file_name   = paste0(config$proj_name, ".met"),
      lat         = config$coord_thisLatLon$lat,
      lon         = config$coord_thisLatLon$lon
    ),
    format = "file"
  ),
  
  # 2. EXPORT OBSERVATIONS TO EXCEL
  tar_target(
    name = msg_obs_saved,
    command = save_obs_to_excel(
      df_final  = qc_apsim_observed, 
      obs_path  = config$folder_observed,
      file_name = config$file_saved_obs_excel,
      sheetName = "Observed"
    ),
    format = "file" 
  ),
  
  # 3. EXPORT INPUT PARAMETERS
  # TO BE IMPLEMENTED
  
  # ----------------------------------------------------------------------------
  # PHASE G: SECURITY & ZIPPING 
  # ----------------------------------------------------------------------------
  
  # 1. THE WATCHER: Track every Excel file in the folder.
  tar_target(
    name = tracked_excel_files,
    command = list.files(config$folder_observed, pattern = "\\.xls[mx]?$", full.names = TRUE),
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
      
      # Return the file string so targets can hash it
      config$file_zip_out
    },
    format = "file"
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE H: PRE-FLIGHT & DEPENDENCY CHECKS  
  # ----------------------------------------------------------------------------
  
  tar_target(
    name = verify_dependencies,
    command = {
      # 1. Force dependency tracking
      msg_obs_saved
      # msg_param_saved # TO BE IMPLEMENTED
      msg_met_saved
      
      # 2. Execute validation
      check_project_dependencies(
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
      force(verify_dependencies) 
      
      check_archive_sync(
        target_folder = config$folder_observed,
        zip_file      = config$file_zip_out
      )
    }
  )
)