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
targets::tar_source("../targets_MasterScripts")

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
      proj_name               = proj_name,
      folder_thisScript       = here::here(),
      folder_rawData          = here::here(proj_name), # Cloud source
      folder_apsimx           = here::here(), 
      folder_met              = here::here("Met"),
      folder_inputs           = here::here("Inputs"),
      folder_observed         = file.path(here::here(), "Observed"),
      file_rawData_excel      = "Turretfield_RawData_2024.xlsx", 
      file_saved_obs_excel    = paste0(proj_name, "_Observed.xlsx"), 
      file_SimNameByCultivar  = paste0(proj_name, "_CultivarToSimName.csv"), 
      file_metaData_observed  = paste0(proj_name, "_observed_data_requirements.csv"),
      
      # Excel sheet names used from raw data
      sheetExcel_weather      = "Weather",
      sheetExcel_haun         = "Haun stage ", # Note: retains raw data typo " "
      sheetExcel_soilWater    = "Soil sampling",
      
      # Security
      file_zip_out               = file.path(here::here(), "Observed.zip"), 
      file_pass                  = file.path(here::here(), "secret_pass.txt"),
      
      # Model parameters
      coord_thisLatLon        = data.frame(lat = -34.5435, lon = 138.8444),
      target_stagePerc        = 50,     # % of stage development when event date is retrieved
      target_betwStages       = 50,     # % of period between adjacent events for synthetic dates
      var_name_stage          = "apsim_stage_raw",       # Synthetic var with observed PCSD data
      varName_addedToObserv   = "Wheat.Phenology.Stage", # Synthetic var added into observations
      max_leaf_limit          = 0.95,   # Fractional max leaves assumed when terminal spikelet is set
      
      # Output file names
      file_name_input_pheno   = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun    = paste0(proj_name, "_HaunStagesInput.csv")
    )
  ),
  
  # Metadata ingestion
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
  # PHASE: SOIL PROFILE PROCESSING
  # ----------------------------------------------------------------------------
  
  # 1. SOIL CONFIGURATION
  # Keeping these parameters in a dedicated list prevents your main config from getting cluttered.
  tar_target(
    name = config_soil,
    command = list(
      folder_rawData  = config$folder_rawData,       # Inheriting from your main config
      file_excel      = config$file_rawData_excel, # Update with your actual filename
      sheet_name      = "Soil sampling",
      rep_col         = "Block",
      col_depth_from  = "Depth From",
      col_depth_to    = "Depth To",
      # The exact columns you want to extract and average for APSIM Soil
      target_vars     = c("Bulk density",	"LL",	
                          "Soil moisture",	"Available water", 
                          "Nitrate Nitrogen","Ammonium Nitrogen") 
    )
  ),
  
  # 2. TRACK THE RAW SOIL FILE
  # If someone updates the bulk density in the Excel file, this guarantees the pipeline catches it.
  tar_target(
    name = raw_soil_tracker,
    command = file.path(config_soil$folder_rawData, config_soil$file_excel),
    format = "file"
  ),
  
  # 3. EXECUTE THE PROCESSING FUNCTION
  tar_target(
    name = df_soil_profile_clean,
    command = {
      # Explicitly bind to the file tracker
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
  
  #---------------------
  # Weather file creation
  #------------------------
  tar_target(
    name = processed_met_data,
    command = createWeatherFile(
      thisFolder    = config$folder_rawData,
      thisExcelFile = config$file_rawData_excel,
      thisSheet     = config$sheetExcel_weather
    )
  ), # <--- THE FIX: Closes the first target and adds a comma for the list!
  
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
  )
  
 # <--- Closes the main list() for the entire pipeline
  # 
  # # ----------------------------------------------------------------------------
  # # PHASE C: RAW OBSERVATION INGESTION
  # # ----------------------------------------------------------------------------
  # tar_target(
  #   name = df_obs_info,
  #   command = read.csv2(
  #     file.path(config$folder_rawData, config$file_metaData_observed),
  #     header = TRUE, 
  #     stringsAsFactors = FALSE, 
  #     sep = ","
  #   )
  # ),
  # 
  # tar_target(
  #   name = list_observed_dfs,
  #   command = compile_all_observed(
  #     folder           = config$folder_rawData,
  #     excel_file       = config$file_rawData_excel,
  #     df_obs_info      = df_obs_info,
  #     df_simNameByCult = df_simNameByCult
  #   )
  # ),
  # 
  # # ----------------------------------------------------------------------------
  # # PHASE D: PHENOLOGY STAGE SYNTHESIS
  # # ----------------------------------------------------------------------------
  # tar_target(
  #   name = df_list_PCDS, 
  #   command = filter_and_extract_pcds(list_observed_dfs)
  # ),
  # 
  # tar_target(
  #   name = df_PCDS_int, 
  #   command = interpolate_obs_phenoStages(df_list_PCDS)
  # ),
  # 
  # tar_target(
  #   name = df_dateStageTargetReached, 
  #   command = findDateStageTarget(df_PCDS_int, config$target_stagePerc)
  # ),
  # 
  # tar_target(
  #   name = df_apsimStageInput, 
  #   command = doAPSIMStageInput(df_dateStageTargetReached, config$target_betwStages)
  # ),
  # 
  # tar_target(
  #   name = df_maxLeafDate, 
  #   command = find_max_leaf_date(list_observed_dfs, config$max_leaf_limit)
  # ),
  # 
  # tar_target(
  #   name = df_haun_pheno_dates, 
  #   command = derive_haun_pheno_dates(
  #     compiled_obs   = list_observed_dfs, 
  #     max_leaf_limit = config$max_leaf_limit
  #   )
  # ),
  # 
  # tar_target(
  #   name = df_apsimStageInput_haunBased,
  #   command = updatePhenoStageInput(
  #     obsIntPheno = df_apsimStageInput,  # Interpolated observations
  #     haunPheno   = df_haun_pheno_dates  # Haun stage priority
  #   )
  # ),
  # 
  # tar_target(
  #   name = df_stages_Observ, 
  #   command = doStageObsData(
  #     df_haunBased = df_apsimStageInput_haunBased, 
  #     var_name     = config$varName_addedToObserv  
  #   )
  # ),
  # 
  # # ----------------------------------------------------------------------------
  # # PHASE E: FINAL OBSERVATION FORMATTING
  # # ----------------------------------------------------------------------------
  # tar_target(
  #   name = list_observed_clean, 
  #   command = apply_corrections(list_observed_dfs, df_stages_Observ)
  # ),
  # 
  # tar_target(
  #   name = list_observed_clean_final, 
  #   command = add_to_observed_clean(
  #     list_observed_clean,
  #     df_stages_Observ,
  #     config$var_name_stage
  #   )
  # ),
  # 
  # tar_target(
  #   name = df_final_observed, 
  #   command = prepare_final_observed(list_observed_clean_final)
  # ), 
  # 
  # tar_target(
  #   name = df_final_observed_harv, 
  #   command = add_harv_into_obs(
  #     df            = df_final_observed,
  #     ref_var       = "Wheat.Grain.Wt", 
  #     new_col_name  = "Wheat.Phenology.CurrentStageName",
  #     new_col_value = "HarvestRipe"
  #   )
  # ),
  # 
  # tar_target(
  #   name = haun_input_checked, 
  #   command = check_manual_params(
  #     config$folder_inputs,
  #     config$file_name_input_haun,
  #     df_final_observed
  #   )
  # ),
  # 
  # # ----------------------------------------------------------------------------
  # # PHASE F: OUTPUT GENERATION & VALIDATION
  # # ----------------------------------------------------------------------------
  # tar_target(
  #   name = msg_obs_saved, 
  #   command = save_df_final(
  #     df_final_observed_harv, 
  #     config$folder_observed, 
  #     config$file_saved_obs_excel
  #   )
  # ),
  # 
  # tar_target(
  #   name = msg_param_saved, 
  #   command = saveInputParam(
  #     df_apsimStageInput_haunBased, 
  #     config$folder_inputs, 
  #     config$file_name_input_pheno
  #   ),
  #   format = "file"
  # ),
  # 
  # tar_target(
  #   name = check_depend, 
  #   command = {
  #     # 1. Force dependency tracking
  #     msg_obs_saved
  #     msg_param_saved
  #     msg_met_saved
  #     
  #     # 2. Execute validation
  #     check_project_dependencies(
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
  #     
  #     # CRITICAL FIX: Return the file string so targets can hash it!
  #     config$file_zip_out
  #   },
  #   format = "file"
  # )
  # 
)