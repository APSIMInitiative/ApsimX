# ==============================================================================
# APSIM-X DATA PIPELINE: WaggaWagga2025
# ==============================================================================
# Description: Pipeline to process Wheat phenology, Haun stages, and weather.
# Goal: Interpolate missing stages, enforce parameters, format for APSIM,
#       and output clean Weather, Input, and Observation files.
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
source("R/createWeatherFile.R")
source("R/save_met_file.R")
source("R/compile_all_observed.R")
source("R/read_observed_func.R")
source("R/filter_and_extract_pcds.R")
source("R/interpolate_obs_phenoStages.R")
source("R/findDateStageTarget.R")
source("R/doAPSIMStageInput.R")
source("R/doStageObsData.R")
source("R/apply_corrections.R")
source("R/add_to_observed_clean.R")
source("R/prepare_final_observed.R")
source("R/check_manual_params.R")
source("R/saveInputParam.R")
source("R/save_df_final.R")
source("R/add_harv_into_obs.R")

# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "WaggaWagga2025"

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
      folder_apsimx           = here::here(),                
      folder_met              = here::here("..", "met"),
      
      file_rawData_excel      = "2025_WaggaWagga_PHDA25WARI2.xlsx", 
      file_saved_obs_excel    = paste0(proj_name, "_Observed.xlsx"), 
      file_SimNameByCultivar  = paste0(proj_name, "_CultivarToSimName.csv"), 
      file_metaData_observed  = paste0(proj_name, "_observed_data_requirements.csv"),
      
      # Excel sheet names used from raw data
      sheetExcel_weather      = "Weather",
      sheetExcel_haun         = "Haun stage ", # Note: retains raw data typo " "
      sheetExcel_soilWater    = "GravimetricMoistureNearSowing",
      
      # Model parameters
      coord_thisLatLon        = data.frame(lat = -35.041, lon = 147.319),
      target_stagePerc        = 50, # % of stage development when event date is retrieved
      target_betwStages       = 50, # % of period between adjacent events for synthetic dates
      
      # Column names
      var_name_stage          = "apsim_stage_raw",       # Synthetic var with observed PCSD data
      varName_addedToObserv   = "Wheat.Phenology.Stage", # Synthetic var added into observations
      
      # Output file names
      file_name_input_pheno   = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun    = paste0(proj_name, "_HaunStagesInput.csv")
    )
  ),
  
  tar_target(
    name = df_simNameByCult,
    command = read.csv2(
      file.path(config$folder_rawData, config$file_SimNameByCultivar),
      header = TRUE, 
      stringsAsFactors = FALSE, 
      sep = ","
    )
  ),
  
  tar_target(
    name = df_obs_info,
    command = read.csv2(
      file.path(config$folder_rawData, config$file_metaData_observed),
      header = TRUE, 
      stringsAsFactors = FALSE, 
      sep = ","
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE B: WEATHER PROCESSING
  # ----------------------------------------------------------------------------
  tar_target(
    name = processed_met_data,
    command = createWeatherFile(
      thisFolder    = config$folder_rawData,
      thisExcelFile = config$file_rawData_excel,
      thisSheet     = config$sheetExcel_weather
    )
  ),
  
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
  
  # ----------------------------------------------------------------------------
  # PHASE C: RAW OBSERVATION INGESTION
  # ----------------------------------------------------------------------------
  tar_target(
    name = list_observed_dfs,
    command = compile_all_observed(
      folder           = config$folder_rawData,
      excel_file       = config$file_rawData_excel,
      df_obs_info      = df_obs_info
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY SYNTHESIS
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_list_PCDS, 
    command = filter_and_extract_pcds(list_observed_dfs)
  ),
  
  tar_target(
    name = df_PCDS_int, 
    command = interpolate_obs_phenoStages(df_list_PCDS)
  ),
  
  tar_target(
    name = df_dateStageTargetReached, 
    command = findDateStageTarget(
      df_PCDS_int, 
      config$target_stagePerc
    )
  ),
  
  tar_target(
    name = df_apsimStageInput, 
    command = doAPSIMStageInput(
      df_dateStageTargetReached,
      df_simNameByCult,
      config$target_betwStages
    )
  ),
  
  tar_target(
    name = df_stages_Observ, 
    command = doStageObsData(
      df_dateStageTargetReached,
      df_simNameByCult,
      config$varName_addedToObserv
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: FINAL OBSERVATION FORMATTING
  # ----------------------------------------------------------------------------
  tar_target(
    name = list_observed_clean, 
    command = apply_corrections(list_observed_dfs, df_stages_Observ)
  ),
  
  tar_target(
    name = list_observed_clean_final, 
    command = add_to_observed_clean(
      list_observed_clean,
      df_stages_Observ,
      config$var_name_stage
    )
  ),
  
  tar_target(
    name = df_final_observed,
    command = prepare_final_observed(
      list_observed_clean_final,
      df_simNameByCult
    )
  ),
  
  tar_target(
    name = df_final_observed_harv, 
    command = add_harv_into_obs(
      df            = df_final_observed,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"), 
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  tar_target(
    name = haun_input_checked, 
    command = check_manual_params(
      config$folder_inputs,
      config$file_name_input_haun,
      df_final_observed
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: EXPORT & VALIDATION
  # ----------------------------------------------------------------------------
  tar_target(
    name = msg_param_saved, 
    command = saveInputParam(
      df_apsimStageInput,
      config$folder_inputs,
      config$file_name_input_pheno
    ),
    format = "file"
  ),
  
  tar_target(
    name = msg_obs_saved,
    command = save_df_final(
      df_final_observed_harv,
      config$folder_apsimx,
      config$file_saved_obs_excel
    ),
    format = "file"
  ),
  
  # Post-flight dependency check for APSIM
  tar_target(
    name = check_depend, 
    command = {
      # 1. Force dependency tracking
      msg_obs_saved
      msg_param_saved
      msg_met_saved
      
      # 2. Execute validation
      check_project_dependencies(
        met_name   = paste0(config$proj_name, ".met"), # <-- THE FIX
        projects   = config$proj_name,
        dir_met    = config$folder_met,
        dir_inputs = config$folder_inputs,
        dir_obs    = config$folder_apsimx
      )
    }
  )
  
)