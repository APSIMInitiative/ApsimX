# ==============================================================================
# APSIM-X DATA PIPELINE: Dookie2024
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
    "here", "tidyverse", "lubridate", "readxl", "openxlsx", "purrr"
  )
)

# ------------------------------------------------------------------------------
# 2. SOURCE CUSTOM FUNCTIONS
# ------------------------------------------------------------------------------
# Load master scripts (Universal Functions)
targets::tar_source("../targets_MasterScripts")

# Load THIS project's specific local scripts
source("R/apply_corrections_Dookie24.R") 

# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "Dookie2024"

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
      # Folders
      proj_name                 = proj_name, 
      folder_thisScript         = here::here(),
      folder_met                = here::here("Met"),
      folder_inputs             = here::here("Inputs"),
      folder_observed           = file.path(here::here(), "Observed"),
      folder_apsimx             = here::here(),                
      folder_rawData            = here::here(proj_name),  
      
      # Input Files with raw data
      file_rawData_excel        = c(
        "UOM2312-001RTX 24 DOO JH EVA WHT.xlsx", 
        "UOM2312-001RTX 24 DOO JH WWHI WHT.xlsx"
      ),
      exp_key_by_rawData_file   = c("EVA","WWHI"),
      sheetExcel_weather        = "Weather",
      file_metaData_observed    = paste0(proj_name, "_observed_data_requirements.csv"),
      file_SimNameByCultivar    = paste0(proj_name, "_CultivarToSimName.csv"),
      
      # Location
      coord_thisLatLon          = data.frame(lat = -36.39, lon = 145.70), 
      
      # Output Files & Naming
      file_saved_obs_excel      = paste0(proj_name, "_Observed.xlsx"), 
      sheet_name_observed       = "Observed",
      file_name_input_pheno     = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun      = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_new_met         = paste0(proj_name, ".met"),
      
      # Security
      file_zip_out              = file.path(here::here(), "Observed.zip"), 
      file_pass                 = file.path(here::here(), "secret_pass.txt"), 
      
      # Model parameters
      date_DOY_ref              = "01-01-2024", # Transform DOY output into ddmmyy
      target_stagePerc          = 50,          # % of phenological-stage development
      target_betwStages         = 0.5,          # fraction of time in-between two pheno-stages
      max_leaf_limit            = 0.95,
      pcd_stages_to_extract     = c("pcds_3_emergPlants_Perc","pcds_6_flagLeaf", "pcds_8_anthesis")
    )
  ),
  
  # THE PIPELINE TRACEABILITY TARGET
  tar_target(
    name = log_active_config,
    command = {
      cat("\n======================================================================\n")
      cat(" ⚙️ ACTIVE PIPELINE CONFIGURATION \n")
      cat("======================================================================\n")
      
      print(config)
      
      cat("======================================================================\n\n")
      invisible(config)
    },
    cue = tar_cue(mode = "always")
  ),
  
  # Map simulations per treatment from APSIM file
  tar_target(
    name = df_simNameByCult,
    command = read.csv(
      file.path(config$folder_rawData, config$file_SimNameByCultivar),
      header = TRUE, 
      stringsAsFactors = FALSE, 
      sep = ","
    )
  ),
  
  # Load observation metadata requirements
  tar_target(
    name = df_obs_meta_data,
    command = read.csv(
      file.path(config$folder_rawData, config$file_metaData_observed),
      header = TRUE, 
      stringsAsFactors = FALSE, 
      sep = ","
    )
  ),
  
  #----
  # SOIL PARAMETERS FOR MODEL SET UP
  #---
  # Note: soil water was set in sim previous to raw data set up
  tar_target(
    name = soil_profile,
    command = read_soil_data(
      folder          = config$folder_rawData,
      file            = "UOM2312-001RTX 24 DOO JH WWHI WHT.xlsx",
      sheet           = "Soil sample",
      vars_to_extract = c("pH (1:5 Water)",	"Nitrate Nitrogen",
                          "Ammonium Nitrogen","C:N Ratio",	
                          "Soil Bulk Density",
                          "Total Carbon (Combustion)", "Gravel (>2mm) of whole sample", 
                          "Silt", "Clay", "Sand"),
      col_depth_from  = "Start depth (m)", # Optional if this matches the default
      col_depth_to    = "End depth (m)",   # Optional if this matches the default
      log_file_name   = paste0(config$proj_name,"_soil_profile.csv")
    )
  ),
  
  tar_target(
    name = soil_water,
    command = read_soil_data(
      folder          = config$folder_rawData,
      file            = "UOM2312-001RTX 24 DOO JH WWHI WHT.xlsx",
      sheet           = "McPoyles 1 APSIM",
      vars_to_extract = c("layer_thickness_mm","BD_Mg_m3",
                          "CLL_wheat_m3_m3","DUL_m3_m3","Sat","pH_CaCl2","OC_%","NO3_mg_kg"
                          ,"NH4_mg_kg","SW_20240425_m3_m3","PAW_20240425_mm"
      ),
      col_depth_from  = "start_depth_m", # Optional if this matches the default
      col_depth_to    = "end_depth_m",   # Optional if this matches the default
      log_file_name   = paste0(config$proj_name,"_soil_water.csv")
    )
  ),
  
  
  
  # ----------------------------------------------------------------------------
  # PHASE B: WEATHER PROCESSING
  # ----------------------------------------------------------------------------
  tar_target(
    name = processed_met_data, 
    command = createWeatherFile(
      thisFolder    = config$folder_rawData, 
      thisExcelFile = grep("WWHI", config$file_rawData_excel, value = TRUE)[1],
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
  # PHASE C: RAW OBSERVATION INGESTION & SCRUBBING
  # ----------------------------------------------------------------------------
  tar_target(
    name = tracked_raw_excel,
    command = file.path(config$folder_rawData, config$file_rawData_excel),
    format = "file"
  ),
  
  tar_target(
    name = list_observed_dfs_raw,
    command = {
      force(tracked_raw_excel)
      compile_all_observed(
        folder      = config$folder_rawData,
        excel_files = config$file_rawData_excel,
        exp_keys    = config$exp_key_by_rawData_file, 
        df_obs_info = df_obs_meta_data,
        df_simNames = df_simNameByCult
      )
    }
  ),
  
  tar_target(
    name = list_observed_dfs_raw_plus_emergPerc,
    command = calc_emerg_perc(
      df_tbl            = list_observed_dfs_raw,  
      df_input_var_name = "pcds_3_emergPlants",
      df_new_var_name   = "pcds_3_emergPlants_Perc"
    )
  ),
  
  # Dookie-specific fix: scrub broken dates before phenology synthesis
  tar_target(
    name = list_observed_clean,
    command = apply_corrections_Dookie24(
      df_tbl   = list_observed_dfs_raw_plus_emergPerc,
      ref_date = config$date_DOY_ref
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY STAGE SYNTHESIS (Universal)
  # ----------------------------------------------------------------------------
  tar_target(
    name = list_pcds_extracted,
    command = filter_and_extract_pcds(
      list_observed_dfs = list_observed_clean, 
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
      df_input       = list_observed_clean,    
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
  
  # 2. THE GATEKEEPER (The new Universal script)
  tar_target(
    name = qc_pheno_integrity,
    command = check_pheno_integrity(df_pheno_input_param)
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: FINAL OBSERVATION FORMATTING & QC
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_obs_wide,
    command = prepare_apsim_observed(
      compiled_obs = list_observed_clean,      
      dfs_out      = config$pcd_stages_to_extract
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
    name = df_obs_plus_pheno_hi,
    command = calc_harvest_index(
      df          = df_obs_plus_pheno,
      grain_col   = "Wheat.Grain.Wt",
      agb_col     = "Wheat.AboveGround.Wt",
      hi_col_name = "HarvestIndex"
    )
  ),
  
  tar_target(
    name = df_obs_plus_pheno_hi_with_amounts,
    command = calc_nutrient_absolute_amounts(
      df           = df_obs_plus_pheno_hi, 
      crop_prefix  = "Wheat",
      organs       = c("Leaf.Live", "Leaf.Dead", "Stem.Live", "Spike.Live"), 
      conc_targets = c("N" = "NConc", "WSC" = "WSCc"), 
      mass_suffix  = "Wt",
      ag_name      = "Wheat.AboveGround",
      divisor      = 1 
    )
  ),
  
  tar_target(
    name = df_obs_plus_pheno_harv,
    command = add_harv_into_obs(
      df            = df_obs_plus_pheno_hi_with_amounts,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt", "HarvestIndex","Wheat.Leaf.Live.NConc"),
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
  
  tar_target(
    name = manual_pheno_params,
    command = check_pheno_manual_parameters(
      folder_name  = config$folder_inputs,
      proj_name    = config$proj_name,
      sim_names_df = df_simNameByCult
    )
  ),
  
  tar_target(
    name = exported_pop_csv,
    command = print_csv_with_select_obs(
      df_in         = qc_apsim_observed_harv, # Simulated dependency: replace with your actual final df
      file_name_out = file.path(paste0(config$proj_name, "_population.csv")),
      select_vars   = c("[Wheat].Leaf.StemPopulation"),
      primary_key   = "SimulationName" # Explicitly utilizing the default we set up
    ),
    format = "file" # <--- Crucial: Tells {targets} to watch the physical CSV file!
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: EXPORT & VALIDATION
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
      df       = qc_pheno_integrity,
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
    command = {
      force(msg_obs_saved)
      list.files(config$folder_observed, pattern = "\\.xls[mx]?$", full.names = TRUE)
    },
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