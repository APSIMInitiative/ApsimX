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
# source("R/createWeatherFile.R")
# source("R/compile_all_observed.R")
# source("R/read_observed_func.R")
# source("R/apply_corrections.R")
# source("R/prepare_final_observed.R")
# source("R/save_df_final.R")
# source("R/filter_and_extract_pcds.R")
# source("R/interpolate_obs_phenoStages.R")
# source("R/create_synthetic_pheno_dates.R")
# source("R/findDateStageTarget.R")
# source("R/doAPSIMStageInput.R")
# source("R/saveInputParam.R")
# source("R/doStageObsData.R")
# source("R/add_to_observed_clean.R")
# source("R/read_soil_water.R")
# source("R/soil_water_in_json.R")
# source("R/check_manual_params.R")
# source("R/check_project_dependencies.R")
# source("R/save_met_file.R")
# source("R/add_harv_into_obs.R")
# source("R/find_max_leaf_date.R")
# source("R/derive_haun_pheno_dates.R")
# source("R/updatePhenoStageInput.R")
# source("R/secure_zip_folder.R")

# Load master scripts
targets::tar_source("../targets_MasterScripts")

# Load THIS project's specific local scripts (e.g., local fixes)
targets::tar_source("R")
# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "WaggaWagga2024"

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
      folder_rawData          = here::here(proj_name), # Cloud source
      folder_apsimx           = here::here(), 
      folder_met              = here::here("Met"),
      folder_inputs           = here::here("Inputs"),
      folder_observed         = file.path(here::here(), "Observed"),
      file_rawData_excel      = "2024_WaggaWagga_PHDA24WARI2.xlsx", 
      file_saved_obs_excel    = paste0(proj_name, "_Observed.xlsx"), 
      file_SimNameByCultivar  = paste0(proj_name, "_CultivarToSimName.csv"), 
      file_metaData_observed  = paste0(proj_name, "_observed_data_requirements.csv"),
      
      # Excel sheet names used from raw data
      sheetExcel_weather      = "Weather",
      sheetExcel_haun         = "Haun stage ", # Note: retains raw data typo " "
      sheetExcel_soilWater    = "GravimetricMoistureNearSowing",
      
      # Security
      file_zip_out               = file.path(here::here(), "Observed.zip"), 
      file_pass                  = file.path(here::here(), "secret_pass.txt"),
      
      # Model parameters
      coord_thisLatLon        = data.frame(lat = -35.041, lon = 147.319),
      target_stagePerc        = 50,     # % of stage development when event date is retrieved
      target_betwStages       = 50,     # % of period between adjacent events for synthetic dates
      var_name_stage          = "apsim_stage_raw",       # Synthetic var with observed PCSD data
      varName_addedToObserv   = "Wheat.Phenology.Stage", # Synthetic var added into observations
      max_leaf_limit          = 0.95,   # Fractional max leaves assumed when terminal spikelet is set
      pcd_stages_to_extract   = c("pcds_3_emergPlants","pcds_6_flagLeaf", "pcds_8_anthesis"),
      
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
  
  # ----------------------------------------------------------------------------
  # PHASE B: SOIL & WEATHER PROCESSING
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_soil_water, 
    command = read_soil_water(
      config$folder_rawData, 
      config$file_rawData_excel, 
      config$sheetExcel_soilWater
    )
  ),
  
  tar_target(
    name = json_soil_water, 
    command = soil_water_in_json(df_soil_water)
  ),
  
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
  
  # tar_target(
  #   name = list_observed_dfs,
  #   command = compile_all_observed(
  #     folder           = config$folder_rawData,
  #     excel_file       = config$file_rawData_excel,
  #     df_obs_info      = df_obs_meta_data,
  #     df_simNameByCult = df_simNameByCult
  #   )
  # ),
  
  # 4. THE UNIVERSAL COMPILER
  tar_target(
    name = list_observed_dfs,
    command = {
      force(tracked_raw_excel) 
      
      compile_all_observed(
        folder      = config$folder_rawData,
        excel_files = config$file_rawData_excel, 
        df_obs_info = df_obs_meta_data,
        df_simNames = df_simNameByCult
      )
    }
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY STAGE SYNTHESIS
  # ----------------------------------------------------------------------------
  tar_target(
    name = list_pcds_extracted,
    command = filter_and_extract_pcds(
      list_observed_dfs = list_observed_dfs,
      pcd_stages        = config$pcd_stages_to_extract
    )
  ),
  
  tar_target(
    name = df_PCDS_int, 
    command = interpolate_obs_phenoStages(list_pcds_extracted)
  ),
  
  tar_target(
    name = df_dateStageTargetReached, 
    command = findDateStageTarget(df_PCDS_int, config$target_stagePerc)
  ),
  
  tar_target(
    name = df_apsimStageInput, 
    command = doAPSIMStageInput(df_dateStageTargetReached, config$target_betwStages)
  ),
  
  tar_target(
    name = df_maxLeafDate, 
    command = find_max_leaf_date(list_observed_dfs, config$max_leaf_limit)
  ),
  
  tar_target(
    name = df_haun_pheno_dates, 
    command = derive_haun_pheno_dates(
      compiled_obs   = list_observed_dfs, 
      max_leaf_limit = config$max_leaf_limit
    )
  ),
  
  tar_target(
    name = df_apsimStageInput_haunBased,
    command = updatePhenoStageInput(
      obsIntPheno = df_apsimStageInput,  # Interpolated observations
      haunPheno   = df_haun_pheno_dates  # Haun stage priority
    )
  ),
  
  tar_target(
    name = df_stages_Observ, 
    command = doStageObsData(
      df_haunBased = df_apsimStageInput_haunBased, 
      var_name     = config$varName_addedToObserv  
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
  
  # tar_target(
  #   name = df_final_observed, 
  #   command = prepare_final_observed(list_observed_clean_final)
  # ), 
  
  
  tar_target(
    name = df_final_observed,
    command = prepare_apsim_observed(
      compiled_obs = list_observed_clean_final,
      dfs_out      = config$pcd_stages_to_extract # Datasets to exclude
    )
  ),
  
  tar_target(
    name = df_final_observed_harv, 
    command = add_harv_into_obs(
      df            = df_final_observed,
      ref_var       = "Wheat.Grain.Wt", 
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
  # PHASE F: OUTPUT GENERATION & VALIDATION
  # ----------------------------------------------------------------------------
  tar_target(
    name = msg_obs_saved, 
    command = save_df_final(
      df_final_observed_harv, 
      config$folder_observed, 
      config$file_saved_obs_excel
    )
  ),
  
  tar_target(
    name = msg_param_saved, 
    command = saveInputParam(
      df_apsimStageInput_haunBased, 
      config$folder_inputs, 
      config$file_name_input_pheno
    ),
    format = "file"
  ),
  
  tar_target(
    name = check_depend, 
    command = {
      # 1. Force dependency tracking
      msg_obs_saved
      msg_param_saved
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