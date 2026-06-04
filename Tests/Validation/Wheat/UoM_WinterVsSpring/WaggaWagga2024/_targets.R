# ==============================================================================
# APSIM-X DATA PIPELINE - WAGGA WAGGA 2024
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
# Load master scripts (Universal Functions)
targets::tar_source("../targets_MasterScripts")

# Load THIS project's specific local scripts (Wagga local fixes)
source("R/apply_corrections_Wagga24.R")

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
      folder_rawData          = here::here(proj_name),
      folder_apsimx           = here::here(), 
      folder_met              = here::here("Met"),
      folder_inputs           = here::here("Inputs"),
      folder_observed         = file.path(here::here(), "Observed"),
      
      file_rawData_excel      = "2024_WaggaWagga_PHDA24WARI2.xlsx", 
      file_saved_obs_excel    = paste0(proj_name, "_Observed.xlsx"), 
      file_SimNameByCultivar  = paste0(proj_name, "_CultivarToSimName.csv"), 
      file_metaData_observed  = paste0(proj_name, "_observed_data_requirements.csv"),
      ref_date                = "01/01/2024", # Target year anchor
      
      # Excel sheet names used from raw data
      sheetExcel_weather      = "Weather",
      sheetExcel_haun         = "Haun stage ", # Retains raw data typo
      sheetExcel_soilWater    = "GravimetricMoistureNearSowing",
      sheet_name_observed     = "Observed",
      
      # Security
      file_zip_out            = file.path(here::here(), "Observed.zip"), 
      file_pass               = file.path(here::here(), "secret_pass.txt"),
      
      # Model parameters
      coord_thisLatLon        = data.frame(lat = -35.041, lon = 147.319),
      target_stagePerc        = 50,     # % of stage development when event date is retrieved
      target_betwStages       = 0.5,     # fraction of period between adjacent events for synthetic dates
      var_df_name_stage       = "apsim_stage_raw",       # Synthetic var with observed PCSD data
      max_leaf_limit          = 0.95,   # Fractional max leaves assumed when terminal spikelet is set
      pcd_stages_to_extract   = c("pcds_3_emergPlants_Perc","pcds_6_flagLeaf", "pcds_8_anthesis"),
      
      # Output file names
      file_name_input_pheno   = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun    = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_new_met       = paste0(proj_name, ".met") # Added to prevent Phase H crash
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
    command = read_soil_water_data(
      config$folder_rawData, 
      config$file_rawData_excel, 
      config$sheetExcel_soilWater
    )
  ),
  
  tar_target(
    name = json_soil_water, 
    command = soil_water_into_json(df_soil_water)
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
      file_name   = config$file_name_new_met,
      lat         = config$coord_thisLatLon$lat,
      lon         = config$coord_thisLatLon$lon
    ),
    format = "file"
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE C: RAW OBSERVATION INGESTION
  # ----------------------------------------------------------------------------
  tar_target(
    name = tracked_raw_excel,
    command = file.path(config$folder_rawData, config$file_rawData_excel),
    format = "file"
  ),
  
  tar_target(
    name = df_obs_meta_data,
    command = read.csv(
      file.path(config$folder_rawData, config$file_metaData_observed),
      header = TRUE, 
      stringsAsFactors = FALSE, 
      sep = ","
    )
  ),
  
  tar_target(
    name = list_observed_dfs_raw,
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
  
  tar_target(
    name = list_observed_dfs,
    command = calc_emerg_perc(
      df_tbl            = list_observed_dfs_raw,  # <--- Change this from df_list to df_tbl
      df_input_var_name = "pcds_3_emergPlants",
      df_new_var_name   = "pcds_3_emergPlants_Perc"
    )
  ),
  
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY STAGE SYNTHESIS (Universal)
  # ----------------------------------------------------------------------------
  tar_target(
    name = list_pcds_extracted,
    command = filter_and_extract_pcds(
      list_observed_dfs = list_observed_dfs,
      pcd_stages        = config$pcd_stages_to_extract
    )
  ),
  
  tar_target(
    name = df_pheno_raw,
    command = get_pheno_dates_from_pcd_list(list_pcds_extracted, config$target_stagePerc)
  ),
  
  tar_target(
    name = df_pheno_int, 
    command = create_interp_pheno_dates(
      df_raw     = df_pheno_raw, 
      btwStgPerc = config$target_betwStages
    )
  ),
  
  tar_target(
    name = df_pheno_haun, 
    command = derive_pheno_stages_from_haun(
      df_input       = list_observed_dfs, 
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
  # PHASE E: FINAL OBSERVATION FORMATTING & QC
  # ----------------------------------------------------------------------------
  tar_target(
    name = list_observed_clean,
    command = apply_corrections_Wagga24(
      df_tbl         = list_observed_dfs, 
      df_pheno_final = df_pheno_final
    )
  ),
  
  tar_target(
    name = df_obs_wide,
    command = prepare_apsim_observed(
      compiled_obs = list_observed_clean,
      dfs_out      = config$pcd_stages_to_extract # Exclude phenology extraction datasets
    )
  ),
  
  tar_target(
    name = df_obs_plus_pheno,
    command = add_new_var_to_obs(
      df_obs          = df_obs_wide,
      df_new_data     = df_pheno_final,
      target_col_name = "Wheat.Phenology.Stage"
    )
  ),
  
  tar_target(
    name = df_obs_plus_pheno_with_amounts,
    command = calc_nutrient_absolute_amounts(
      df           = df_obs_plus_pheno, 
      crop_prefix  = "Wheat",
      organs       = c("Leaf.Live", "Leaf.Dead", "Stem.Live", "Spike.Live"), 
      conc_targets = c("N" = "NConc", "WSC" = "WSCc"), 
      mass_suffix  = "Wt",
      ag_name      = "Wheat.AboveGround",
      divisor      = 1  # Note: Assuming your conc values are already fractions (g/g). If they are %, change to 100!
    )
  ),
  
  tar_target(
    name = df_obs_plus_pheno_harv,
    command = add_harv_into_obs(
      df            = df_obs_plus_pheno_with_amounts,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt", "WSCs", "Nconc","HarvestIndex"),
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  tar_target(
    name = qc_apsim_observed_harv,
    command = check_obs_health(df_obs_plus_pheno_harv)
  ),
  
  tar_target(
    name = haun_input_checked,
    command = check_manual_params(
      config$folder_inputs,
      config$file_name_input_haun,
      qc_apsim_observed_harv
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: OUTPUT GENERATION
  # ----------------------------------------------------------------------------
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
  
  tar_target(
    name = msg_pheno_param_saved,
    command = save_df_into_csv(
      df       = df_pheno_input_param,
      folder   = config$folder_inputs,
      filename = config$file_name_input_pheno
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
      config$file_zip_out
    },
    format = "file"
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE H: PRE-FLIGHT & DEPENDENCY CHECKS
  # ----------------------------------------------------------------------------
  tar_target(
    name = check_depend,
    command = {
      force(msg_met_saved)
      force(msg_obs_saved)
      force(msg_pheno_param_saved)
      force(haun_input_checked)
      
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