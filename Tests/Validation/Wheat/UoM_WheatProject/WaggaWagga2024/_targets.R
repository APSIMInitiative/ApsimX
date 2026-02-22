library(targets)
library(rstudioapi)
library(here)

#-------------------------
# Define required packages
#-------------------------

tar_option_set(packages = c("tidyverse", "lubridate","purrr", 
                            "openxlsx", "readxl", "glue", "rstudioapi",
                            "stringr", "tidyr", "jsonlite"))
#----------------------
# Define used functions
#----------------------

source("R/createWeatherFile.R")
source("R/compile_all_observed.R")
source("R/read_observed_func.R")
source("R/apply_corrections.R")
source("R/prepare_final_observed.R")
source("R/save_df_final.R")
source("R/filter_and_extract_pcds.R")
source("R/interpolate_obs_phenoStages.R")
source("R/create_synthetic_pheno_dates.R")
source("R/findDateStageTarget.R")
source("R/doAPSIMStageInput.R")
source("R/saveInputParam.R")
source("R/doStageObsData.R")
source("R/add_to_observed_clean.R")
source("R/read_soil_water.R")
source("R/soil_water_in_json.R")
source("R/check_manual_params.R")

#----------------
# Define targets
#----------------

targets <- list(
  
  # define global configuration of parameters
  tar_target(
    config,
    list(
      # folders and file names
      folder_thisScript           = here::here(),
      folder_rawData              = here::here("WaggaWagga2024/InputFilesFromCloud"), # this will be from Cloud
      folder_inputs               = here::here("..", "inputs"),
      folder_apsimx               = here::here(), # a level up from where Analysis is
      file_rawData_excel          = "2024_WaggaWagga_PHDA24WARI2.xlsx", # raw observed data (pre-defined file name)
      file_input_name_saved       = "WaggaWagga2024_PhenoDatesInput.csv", # name for forced pheno-dates file
      fileNameForAPSIM_observData = "WaggaWagga2024_Observed.xlsx", # name of observation file created for APSIM to read
      file_SimNameByCultivar      = "WaggaWagga2024/WaggaWagga2024_CultivarToSimName.csv", # lookup table of sim by treat names (handmade metadata)
      file_metaData_observed      = "WaggaWagga2024/WaggaWagga2024_observed_data_requirements.csv",  # What obs variables to fetch? (handmade metadata)
      
      # Excel sheet names used from 2024_WaggaWagga_PHDA24WARI2.xlsx
      sheetExcel_weather          = "Weather",
      sheetExcel_haun             = "Haun stage ", # Note the extra space as-is in typo in observations (!!!!)
      sheetExcel_soilWater        = "GravimetricMoistureNearSowing",
      
      # Other function parameters
      coord_thisLatLon            = data.frame(lat = -35.041, lon = 147.319),
      target_stagePerc            = 50, # % of a stage development when event date is retrieved
      target_betwStages           = 50, # % of period between two adjacent events when a synthetic event date is assumed
      var_name_stage              = "apsim_stage_raw", # name of synthetic var with observed PCSD data
      varName_addedToObserv       = "Wheat.Phenology.Stage", # new synthetic variable to be added into observations
      file_name_input_haun        = "WaggaWagga2024_HaunRelatedInput.csv"
    )
  ),
  
  # Config: Get simulation names per treatment from APSIM file (via APSIM-UI) - NOTE: func this might have to change with exp
  tar_target(df_simNameByCult,read.csv2(file.path(config$folder_thisScript, 
                                                  config$file_SimNameByCultivar),
                                        header = TRUE, stringsAsFactors = FALSE, sep = ",")),
  
  ### ------------------------------------------------
  ### Read soil parameters to be used in UI
  ### ------------------------------------------------
  
  tar_target(df_soil_water, read_soil_water(config$folder_rawData, 
                                     config$file_rawData_excel, 
                                     config$sheetExcel_soilWater)),
  
  tar_target(json_soil_water, soil_water_in_json(df_soil_water)),
  
  ### ------------------------------------------------
  ### Create met file to run APSIM
  ### ------------------------------------------------
  
  tar_target(df_met, createWeatherFile(config$folder_rawData, 
                                       config$file_rawData_excel, 
                                       config$sheetExcel_weather, 
                                       config$coord_thisLatLon)),
  
  ### -------------------------------------------------------------------------------
  ### Prepare excel data with observation in APSIM format to compare with simulations
  ### -------------------------------------------------------------------------------
  
  # check which observed data is needed to use based on a hand-made csv meta-data file
  tar_target(df_obs_info,read.csv2(file.path(config$folder_thisScript, 
                                             config$file_metaData_observed),
             header = TRUE, stringsAsFactors = FALSE, sep = ",")),
  
  # Reads excel raw observations based on meta data above (raw as-is) and appends them into a single list of dfs 
  tar_target(list_observed_dfs,compile_all_observed(config$folder_rawData,
                                                    config$file_rawData_excel,
                                                    df_obs_info)),
  
  ### ----------------------------------------------------------------------------------------
  ### Create APSIM stage parameters as FORCED input - AND add it as synthetic data to observations (stages_raw)
  ### ----------------------------------------------------------------------------------------
  
  # Filter and extract the PCDS pheno-stages observed from excel raw data
  tar_target(df_list_PCDS, filter_and_extract_pcds(list_observed_dfs)),
  
  #' Interpolates observed PCDS observed variables across Date
  tar_target(df_PCDS_int, interpolate_obs_phenoStages(df_list_PCDS)),
  
  # Finds a date when a target % for each stage is reached
  tar_target(df_dateStageTargetReached, findDateStageTarget(df_PCDS_int, 
                                                            config$target_stagePerc)),
  
  # Create synthetic in-between pheno stages within a APSIM format input file
  tar_target(df_apsimStageInput, doAPSIMStageInput(df_dateStageTargetReached, 
                                                   df_simNameByCult, 
                                                   config$target_betwStages)),
  
  # Create Observed data of pheno-satges to be added to observations (as cross-check)
  tar_target(df_stages_Observ, doStageObsData(df_dateStageTargetReached,
                                              df_simNameByCult,
                                              config$varName_addedToObserv)),
  
  ### ----------------------------------------------------------------------------------------
  ### Finish observation file to be read by APSIM
  ### ----------------------------------------------------------------------------------------
  
  
  # Makes by-hand data corrections as needed to fix raw excel data (see apply_corrections() for details)
  tar_target(list_observed_clean, apply_corrections(list_observed_dfs, df_stages_Observ)),
  

  tar_target(list_observed_clean_final, add_to_observed_clean(list_observed_clean,
                                                              df_stages_Observ,
                                                        config$var_name_stage)),
  
  
  # Prepare the format of a APSIM observation standard file
  tar_target(df_final_observed, 
             prepare_final_observed(list_observed_clean_final,
                                    df_simNameByCult)), 
  
  # check if manual parameters are correct
  tar_target(haun_input_checked, check_manual_params(config$folder_inputs,
                                                     config$file_name_input_haun,
                                                     df_final_observed)),
  
  
  #### ---------------------------------------
  ### Save files that need to be read by APSIM
  #### ----------------------------------------
  
  
  # save the output as APSIM likes to read it
  tar_target(msg_obs_saved, 
             save_df_final(df_final_observed, 
                           config$folder_apsimx, 
                           config$fileNameForAPSIM_observData)),
  
  
  
  # Save parameter input file with forced pheno-dates into /input
  tar_target(msg_param_saved, saveInputParam(df_apsimStageInput, 
                                           config$folder_inputs, 
                                           config$file_input_name_saved),
    format = "file" 
  )
)

#  return a pipeline object.
tar_pipeline(targets)

