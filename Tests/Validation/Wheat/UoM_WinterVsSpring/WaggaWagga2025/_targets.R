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
source("R/check_project_dependencies.R")
source("R/derive_haun_pheno_dates.R")
source("R/updatePhenoStageInput.R")
source("R/add_stages_to_obs.R")
source("R/secure_zip_folder.R")

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
      # Folders and file paths
      proj_name               = proj_name,
      folder_thisScript       = here::here(),
      folder_rawData          = here::here(proj_name),       # Cloud source
      folder_met                = here::here("Met"),
      folder_inputs             = here::here("Inputs"),
      folder_observed           = file.path(here::here(), "Observed"),
      folder_apsimx             = here::here(),   
      
      # Security
      file_zip_out               = file.path(here::here(), "Observed.zip"), 
      file_pass                  = file.path(here::here(), "secret_pass.txt"),
      
      # Target file names
      file_rawData_excel      = "2025_WaggaWagga_PHDA25WARI2.xlsx", 
      file_saved_obs_excel    = paste0(proj_name, "_Observed.xlsx"), 
      file_SimNameByCultivar  = paste0(proj_name, "_CultivarToSimName.csv"), 
      file_metaData_observed  = paste0(proj_name, "_observed_data_requirements.csv"),
      date_DOY_ref               = "01-01-2025", # Transform DOY output into ddmmyy
      
      # Excel sheet mappings
      sheetExcel_weather      = "Weather",
      sheetExcel_haun         = "Haun stage ", # Note: retains raw data typo " "
      sheetExcel_soilWater    = "GravimetricMoistureNearSowing",
      
      # Model calculation parameters
      coord_thisLatLon        = data.frame(lat = -35.041, lon = 147.319),
      target_stagePerc        = 50,    # % of stage development when event date is retrieved
      target_betwStages       = 50,    # % of period between adjacent events for synthetic dates
      max_leaf_limit          = 0.95,  # Fractional max leaves assumed when terminal spikelet is set
      
      # Variable tracking
      var_name_stage          = "apsim_stage_raw",       # Synthetic var with observed PCSD data
      varName_addedToObserv   = "Wheat.Phenology.Stage", # Synthetic var added into observations
      
      # Output file names
      file_name_input_pheno   = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun    = paste0(proj_name, "_HaunStagesInput.csv")
    )
  ),
  
  # Load mapping dictionary: Cultivar -> Simulation Name
  tar_target(
    name = df_simNameByCult,
    command = read.csv2(
      file.path(config$folder_rawData, config$file_SimNameByCultivar),
      header           = TRUE, 
      stringsAsFactors = FALSE, 
      sep              = ","
    )
  ),
  
  # Load tracking list of variables to fetch from observations
  tar_target(
    name = df_obs_meta_data,
    command = read.csv2(
      file.path(config$folder_rawData, config$file_metaData_observed),
      header           = TRUE, 
      stringsAsFactors = FALSE, 
      sep              = ","
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE B: WEATHER PROCESSING
  # ----------------------------------------------------------------------------
  # Parse the weather sheet into list format
  tar_target(
    name = processed_met_data,
    command = createWeatherFile(
      thisFolder    = config$folder_rawData,
      thisExcelFile = config$file_rawData_excel,
      thisSheet     = config$sheetExcel_weather
    )
  ),
  
  # Export the APSIM-formatted .met file
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
  # Read and map all observation sheets to simulations
  tar_target(
    name = list_observed_dfs_raw,
    command = compile_all_observed(
      folder      = config$folder_rawData,
      excel_files = config$file_rawData_excel,
      df_obs_info = df_obs_meta_data,
      df_simNames = df_simNameByCult   # Inject the mapping table here
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY SYNTHESIS
  # ----------------------------------------------------------------------------
  # 1. Filter and extract the continuous PCDS pheno-stages from raw data
  tar_target(
    name = df_list_PCDS, 
    command = filter_and_extract_pcds(list_observed_dfs_raw)
  ),
  
  # 2. Mathematically interpolate stages across dates
  tar_target(
    name = df_PCDS_int, 
    command = interpolate_obs_phenoStages(df_list_PCDS, config$date_DOY_ref)
  ),
  
  # 3. Locate exact dates where target completion percentage is reached
  tar_target(
    name = df_dateStageTargetReached, 
    command = findDateStageTarget(
      df_PCDS_int, 
      config$target_stagePerc
    )
  ),
  
  # 4. Generate the base APSIM stage input file
  tar_target(
    name = df_apsimStageInput, 
    command = doAPSIMStageInput(df_dateStageTargetReached, 
                                config$target_betwStages)
  ),
  
  # 5. Build an observed version of these discrete stages for the master dataframe
  tar_target(
    name = df_stages_Observ, 
    command = doStageObsData(
      df_dateStageTargetReached,
      config$varName_addedToObserv
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: FINAL OBSERVATION FORMATTING & HAUN PRIORITY MERGE
  # ----------------------------------------------------------------------------
  # Apply raw data corrections
  tar_target(
    name = list_observed_clean, 
    command = apply_corrections(
      list_observed_dfs_raw, 
      df_stages_Observ
    )
  ),
  
  # Append stage data securely into the nested tibbles
  tar_target(
    name = list_observed_clean_final, 
    command = add_to_observed_clean(
      list_observed_clean,
      df_stages_Observ,
      config$var_name_stage
    )
  ),
  
  # Derive advanced phenology milestones directly from Haun stage records
  tar_target(
    name = df_haun_pheno_dates, 
    command = derive_haun_pheno_dates(
      compiled_obs   = list_observed_clean_final, 
      max_leaf_limit = config$max_leaf_limit
    )
  ),
  
  # Overwrite base interpolated dates with higher-priority Haun dates
  tar_target(
    name = df_apsimStageInput_haunBased,
    command = updatePhenoStageInput(
      obsIntPheno = df_apsimStageInput,
      haunPheno   = df_haun_pheno_dates
    )
  ),
  
  # Flatten nested tibbles into a clean, wide dataframe, scrubbing ghost rows
  tar_target(
    name = df_final_observed,
    command = prepare_final_observed(list_observed_clean_final)
  ),
  
  # Flag the final measurement dates as "HarvestRipe"
  tar_target(
    name = df_final_observed_harv, 
    command = add_harv_into_obs(
      df            = df_final_observed,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt"), 
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  # Re-inject the finalized, Haun-prioritized discrete event dates into the timeline
  tar_target(
    name = df_obs_mean_harv_pheno,
    command = add_stages_to_obs(
      df_obs       = df_final_observed_harv, 
      df_pheno     = df_apsimStageInput_haunBased,
      new_var_name = "Wheat.Phenology.Stage"
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: EXPORT & VALIDATION
  # ----------------------------------------------------------------------------
  # Validate that the manual Haun CSV is present and properly formatted
  tar_target(
    name = haun_input_checked, 
    command = check_manual_params(
      config$folder_inputs,
      config$file_name_input_haun,
      df_final_observed
    )
  ),
  
  # Export final Phenology parameterization file for APSIM
  tar_target(
    name = msg_param_saved, 
    command = saveInputParam(
      df_apsimStageInput_haunBased,
      config$folder_inputs,
      config$file_name_input_pheno
    ),
    format = "file"
  ),
  
  # Export the final master Observations Excel file for APSIM
  tar_target(
    name = msg_obs_saved,
    command = save_df_final(
      df_obs_mean_harv_pheno,
      config$folder_observed,
      config$file_saved_obs_excel
    ),
    format = "file"
  ),
  
  # Post-flight dependency check for APSIM (Verifies all files exist correctly)
  tar_target(
    name = check_depend, 
    command = {
      # 1. Force dependency tracking
      msg_obs_saved
      msg_param_saved
      msg_met_saved
      haun_input_checked
      
      # 2. Execute validation
      check_project_dependencies(
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
  # If any file changes, this target invalidates.
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
      
      # CRITICAL FIX: Return the file string so targets can hash it!
      config$file_zip_out
    },
    format = "file"
  )
  
)