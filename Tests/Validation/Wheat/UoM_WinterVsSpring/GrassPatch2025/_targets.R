# ==============================================================================
# APSIM-X DATA PIPELINE: GrassPatch2025
# ==============================================================================
# Description: Base simulation and observed data copied from FAR Australia.
# Goal: Average reps, force fit Haun stage and phenology dates as model inputs.
# Architecture: Single-Trial Wide Format with Hardcoded Sanitization
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

# Load local corrections specific to this Grass25 dataset
source("R/apply_corrections_Grass25.R")
source("R/apply_name_corrections_Grass25.R")

# ------------------------------------------------------------------------------
# 3. PROJECT DEFINITION
# ------------------------------------------------------------------------------
proj_name <- "GrassPatch2025"

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
      proj_name               = proj_name,
      folder_thisScript       = here::here(),
      folder_rawData          = here::here(proj_name),       
      folder_apsimx           = here::here(),                
      folder_met              = here::here("Met"),
      folder_inputs           = here::here("Inputs"),
      folder_observed         = file.path(here::here(), "Observed"),
      
      file_rawData_excel      = "Observed.xlsx",             
      file_workData_excel     = paste0(proj_name, "_Observed.xlsx"),
      sheet_name_observed     = "Observed",
      
      # Security
      file_zip_out            = file.path(here::here(), "Observed.zip"), 
      file_pass               = file.path(here::here(), "secret_pass.txt"),
      
      # Model parameters
      date_DOY_ref            = "01-01-2025", 
      btwStgPerc              = 0.5,          
      max_leaf_limit          = 0.95,         
      
      # Output file names & Metadata
      file_name_input_pheno   = paste0(proj_name, "_PhenoDatesInput.csv"),
      file_name_input_haun    = paste0(proj_name, "_HaunStagesInput.csv"),
      file_name_met           = "Grass Patch_-33.20_121.65.met",
      file_name_new_met       = paste0(proj_name, ".met"),
      file_name_mapping_csv   = paste0(proj_name, "_obs_var_new_names.csv")
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
  
  
  # ------------------------------------------------------------------
  # PHASE B: VALIDATE AND COPY MET FILE
  # ------------------------------------------------------------------
  tar_target(
    name = validated_met_file,
    command = copy_and_check_met(
      sourceFolder   = config$folder_rawData,  
      targetFolder   = config$folder_met,        
      orig_file_name = config$file_name_met,     
      new_file_name  = config$file_name_new_met  
    ),
    format = "file" 
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE C: RAW OBSERVATION INGESTION & SANITIZATION
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_obs, 
    command = read_excel_observed(
      file.path(config$folder_rawData, config$file_rawData_excel)
    )
  ),
  
  # THE SANITIZER: Replaces 'FAR WAE W25-49' before the phenology branch splits off
  tar_target(
    name = df_obs_corrected,
    command = apply_corrections_Grass25(df_obs)
  ),
  
  # THE COLUMN RENAMER: Applies the CSV mapping rules
  tar_target(
    name = track_mapping_csv,
    command = file.path(config$folder_rawData, config$file_name_mapping_csv),
    format = "file" 
  ),
  tar_target(
    name = df_obs_renamed,
    command = apply_name_corrections_Grass25(
      df_obs           = df_obs_corrected,
      mapping_csv_path = track_mapping_csv
    )
  ),
  
  tar_target(
    name = df_obs_mean, 
    command = do_obs_means(df_obs_renamed)
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE D: PHENOLOGY & HAUN STAGES
  # ----------------------------------------------------------------------------
  tar_target(
    name = df_pheno_raw, 
    command = get_pheno_dates_from_DOY_wide_obs(df_obs_mean, config$date_DOY_ref)
  ),
  tar_target(
    name = df_pheno_int, 
    command = create_interp_pheno_dates(df_pheno_raw, config$btwStgPerc)
  ),
  tar_target(
    name = df_haun, 
    command = get_column_var_from_observ(df_obs_mean, c("SimulationName", "Clock.Today"), "Wheat.Phenology.HaunStage")
  ),
  tar_target(
    name = df_pheno_haun,
    command = derive_pheno_stages_from_haun(df_haun, config$max_leaf_limit)
  ),
  tar_target(
    name = df_pheno_final,
    command = merge_and_qc_pheno(df_raw = df_pheno_raw, df_haun = df_pheno_haun, df_int = df_pheno_int)
  ),
  tar_target(
    name = df_pheno_input_param,
    command = format_apsim_pheno_params(df_pheno_final)
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE E: CALCULATIONS & RENAMING
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
    name = df_obs_plus_pheno_plus_hi,
    command = calc_harvest_index(
      df          = df_obs_plus_pheno, 
      grain_col   = "Wheat.Grain.Wt", 
      agb_col     = "Wheat.AboveGround.Wt", 
      hi_col_name = "HarvestIndex"
    )
  ),
  
  
  # Pipe straight from the Renamed data into Nutrient amounts
  # tar_target(
  #   name = df_obs_plus_pheno_plus_hi_amounts,
  #   command = calc_nutrient_absolute_amounts(
  #     df           = df_obs_plus_pheno_plus_hi, 
  #     crop_prefix  = "Wheat",
  #     organs       = c("Leaf.Live", "Leaf.Dead", "Stem.Live", "Spike.Live"), 
  #     conc_targets = c("N" = "NConc", "WSC" = "WSCc"), 
  #     mass_suffix  = "Wt",
  #     ag_name      = "Wheat.AboveGround",
  #     divisor      = 1 
  #   )
  # ),
  
  tar_target(
    name = df_obs_plus_pheno_plus_hi_amounts,
    command = calc_nutrient_absolute_amounts(
      df             = df_obs_plus_pheno_plus_hi, 
      crop_prefix    = "Wheat",
      organs         = c("Leaf.Live", "Leaf.Dead", "Stem.Live", "Spike.Live"), 
      conc_targets   = c("N" = "NConc", "WSC" = "WSCc"), 
      mass_suffix    = "Wt",
      ag_name        = "Wheat.AboveGround",
      divisor        = 1,
      error_log_path = file.path(paste0(config$proj_name, "_nutrient_calc_logs.csv"))
    )
  ),
  
  
  tar_target(
    name = df_obs_final,
    command = add_harv_into_obs(
      df            = df_obs_plus_pheno_plus_hi_amounts,
      ref_vars      = c("Wheat.AboveGround.Wt", "Wheat.Grain.Wt", 
                        "HarvestIndex", "Wheat.Spike.Live.Wt"),
      new_col_name  = "Wheat.Phenology.CurrentStageName",
      new_col_value = "HarvestRipe"
    )
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE F: EXPORT & VALIDATION
  # ----------------------------------------------------------------------------
  # 1. The QC Gatekeeper
  tar_target(
    name = qc_apsim_observed,
    command = check_obs_health(df_obs_final)
  ),
  tar_target(
    name = haun_input_checked, 
    command = check_manual_params(config$folder_inputs, config$file_name_input_haun, qc_apsim_observed)
  ),
  tar_target(
    name = msg_pheno_param_saved,
    command = save_df_into_csv(df_pheno_input_param, config$folder_inputs, config$file_name_input_pheno),
    format = "file"
  ),
  
  tar_target(
    name = exported_pop_csv,
    command = print_csv_with_select_obs(
      df_in         = qc_apsim_observed, # Simulated dependency: replace with your actual final df
      file_name_out = file.path(paste0(config$proj_name, "_population.csv")),
      select_vars   = c("Wheat.SowingData.Population"),
      primary_key   = "SimulationName" # Explicitly utilizing the default we set up
    ),
    format = "file" # <--- Crucial: Tells {targets} to watch the physical CSV file!
  ),
  
  tar_target(
    name = msg_obs_saved,
    command = save_df_to_excel(
      df          = qc_apsim_observed, 
      folder_path = config$folder_observed, 
      file_name   = config$file_workData_excel,
      sheet_name  = config$sheet_name_observed
    ),
    format = "file"
  ),
  
  tar_target(
    name = manual_pheno_params,
    command = check_pheno_manual_parameters(
      folder_name  = config$folder_inputs,
      proj_name    = config$proj_name,
      sim_names_df = qc_apsim_observed
    )
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
      secure_zip_folder(config$folder_observed, config$file_zip_out, config$file_pass)
      config$file_zip_out
    },
    format = "file"
  ),
  
  # ----------------------------------------------------------------------------
  # PHASE H: DEPENDENCY CHECKS
  # ----------------------------------------------------------------------------
  tar_target(
    name = check_depend, 
    command = {
      force(validated_met_file)
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
      check_archive_sync(config$folder_observed, config$file_zip_out)
    }
  )
)