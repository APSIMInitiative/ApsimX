# ==============================================================================
# APSIM-X DATA PIPELINE: Dookie2025
# ==============================================================================
# Description: Pipeline to process Wheat phenology, Haun stages, and weather.
# Goal: Interpolate missing stages, enforce parameters, format for APSIM,
#       and output clean Weather, Input, and Observation files.
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
source("R/createWeatherFile.R")
source("R/save_met_file.R")
source("R/compile_all_observed.R")
source("R/read_observed_func.R")
source("R/filter_and_extract_pcds.R")
source("R/attach_sim_names.R")
source("R/findDateStageTarget.R")
source("R/interpolate_obs_phenoStages.R")
source("R/doAPSIMStageInput.R")
source("R/saveInputParam.R")
source("R/doStageObsData.R")
source("R/add_to_observed.R")
source("R/prepare_observed_final.R")
source("R/save_df_final.R")
source("R/check_manual_params.R")
source("R/add_harv_into_obs.R")
source("R/do_entry_fixes.R")
source("R/check_project_dependencies.R")

# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "Dookie2025"

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
      folder_apsimx              = here::here(),                
      folder_rawData             = here::here(proj_name),  
      
      file_rawData_excel         = c(
        "UOM2312-001RTX 25 DOO JH EVA WHT.xlsx", 
        "UOM2312-001RTX 25 DOO JH WWHI WHT.xlsx"
      ),
      sheetExcel_weather         = "Met Data",
      coord_thisLatLon           = data.frame(lat = -36.39, lon = 145.70), 
      
      file_metaData_observed     = "Observed_data_requirements.csv",
      file_saved_obs_excel       = paste0(proj_name, "_Observed.xlsx"), 
      sheet_name_observed        = "Observed",
      
      # Model parameters
      date_DOY_ref               = "01-01-2025", # Transform DOY output into ddmmyy
      target_stageDatePerc       = 50,           # % of phenological-stage development
      target_btwStagesPerc       = 50,           # % of time in-between two pheno-stages
      
      # Output file names & Metadata
      file_name_input_pheno      = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun       = paste0(proj_name, "_HaunStagesInput.csv"),
      
      cols_to_extract            = c(
        "SimulationName",
        "Clock.Today",
        "Wheat.Phenology.HaunStage",
        "Wheat.Phenology.Stage"
      ),
      file_name_cult_by_sowDate  = "CultivarBySowingDatesTemplate.csv"
    )
  ),
  
  # Map simulations per treatment from APSIM file
  tar_target(
    name = df_simNameByCult,
    command = read.csv2(
      file.path(config$folder_rawData, config$file_name_cult_by_sowDate),
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
      # Search the array for "WWHI". [1] ensures it only takes the first match
      thisExcelFile = grep("WWHI", config$file_rawData_excel, value = TRUE)[1],
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
  # PHASE C: RAW OBSERVATION INGESTION & PROCESSING
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_obs_info,
    command = read.csv2(
      file.path(config$folder_rawData, config$file_metaData_observed),
      header = TRUE, 
      stringsAsFactors = FALSE, 
      sep = ","
    )
  ),
  
  tar_target(
    name = list_observed_dfs_raw,
    command = compile_all_observed(
      folder      = config$folder_rawData,
      excel_file  = config$file_rawData_excel,
      df_obs_info = df_obs_info
    )
  ),
  
  # Fix bad data entries (e.g., dates)
  tar_target(
    name = list_observed_dfs_raw_clean,
    command = do_entry_fixes(
      list_observed_dfs_raw,
      config$date_DOY_ref
    )
  ),
  
  # Map observations to Simulations
  tar_target(
    name = list_observed_dfs,
    command = attach_sim_names(
      list_observed_dfs_raw_clean, 
      df_simNameByCult
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY SYNTHESIS
  # ----------------------------------------------------------------------------
  # Filter and extract the PCDS pheno-stages observed from excel raw data
  tar_target(
    name = df_list_PCDS, 
    command = filter_and_extract_pcds(list_observed_dfs)
  ),
  
  # Interpolates observed PCDS variables across Date
  tar_target(
    name = df_PCDS_int, 
    command = interpolate_obs_phenoStages(df_list_PCDS)
  ),
  
  # Finds a date when a target % for each stage is reached
  tar_target(
    name = df_dateStageTargetReached, 
    command = findDateStageTarget(
      df_PCDS_int,
      config$target_stageDatePerc
    )
  ),
  
  tar_target(
    name = df_apsimStageInput,
    command = doAPSIMStageInput(
      df_dateWhenStageWasReached = df_dateStageTargetReached, 
      BtwStgPerc                 = config$target_btwStagesPerc, 
      fill_NAs_with_average      = TRUE # <--- CHANGE THIS TO FALSE FOR FINAL ANALYSIS
    )
  ),
  
  # Create Observed data of pheno-stages to be added to observations (as cross-check)
  tar_target(
    name = df_stages_Observ, 
    command = doStageObsData(
      df_dateStageTargetReached,
      "Wheat.Phenology.Stage"
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: FINAL OBSERVATION FORMATTING
  # ----------------------------------------------------------------------------
  # Add stage to list of observed data and clean metadata
  tar_target(
    name = list_observed_dfs_clean, 
    command = add_to_observed(
      list_observed_dfs,
      df_stages_Observ,
      "phenology_stage_raw"
    )
  ),
  
  # Prepare the format of an APSIM observation standard file
  tar_target(
    name = df_observed_wide, 
    command = prepare_observed_final(list_observed_dfs_clean)
  ),
  
  # Add HarvestRipe flags at final measurements
  tar_target(
    name = df_observed_wide_harv, 
    command = add_harv_into_obs(
      df            = df_observed_wide,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"), 
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  # Check if external/manual HAUN parameters are correct (and create template if not)
  tar_target(
    name = haun_input_checked, 
    command = check_manual_params(
      config$folder_inputs,
      config$file_name_input_haun,
      df_observed_wide
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: EXPORT & VALIDATION
  # ----------------------------------------------------------------------------
  # Save parameter input file with forced pheno-dates into /input
  tar_target(
    name = msg_param_saved, 
    command = saveInputParam(
      df_apsimStageInput, 
      config$folder_inputs, 
      config$file_name_input_pheno
    ),
    format = "file"
  ),
  
  # Save the output as APSIM likes to read it
  tar_target(
    name = msg_obs_saved, 
    command = save_df_final(
      df_observed_wide_harv, 
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
      haun_input_checked
      
      # 2. Execute validation (Removed 'met_name' to match this project's function)
      check_project_dependencies(
        projects   = config$proj_name,
        dir_met    = config$folder_met,
        dir_inputs = config$folder_inputs,
        dir_obs    = config$folder_apsimx
      )
    }
  )
  
)