library(targets)
library(rstudioapi)
library(here)

#-------------------------
# Define required packages
#-------------------------

tar_option_set(packages = c("tidyverse", "lubridate","purrr", 
                            "openxlsx", "readxl", "glue", "rstudioapi",
                            "stringr", "tidyr"))
#----------------------
# Define used functions
#----------------------

source("R/createWeatherFile.R")
source("R/interpolateHaunStages.R")
source("R/createStageInputParameter.R")
source("R/compile_all_observed.R")
source("R/read_observed_func.R")
#source("R/clean_observed.R")
source("R/apply_corrections.R")
source("R/prepare_final_observed.R")
source("R/save_df_final.R")
source("R/filter_and_extract_pcds.R")
source("R/interpolate_obs_phenoStages.R")
source("R/create_synthetic_pheno_dates.R")
source("R/findDateStageTarget.R")
source("R/doAPSIMStageInput.R")
source("R/saveInputParam.R")

#---------------------------------------------------
# Set function parameters
#---------------------------------------------------
#folder_rawData <- "InputFilesFromCloud"

#folder_thisScript <- here::here()
#folder_rawData <- here("InputFilesFromCloud")
folder_inputs <- here::here("..", "..", "inputs")
file_observ_excel<- "2024_WaggaWagga_PHDA24WARI2.xlsx"
sheetExcel_weather <- "Weather"
coord_thisLatLon <- data.frame(lat=-35.041, lon=147.319)
sheetExcel_haun <- "Haun stage "
file_input_name_saved <- "DookiePhenoDatesInput_Wagga.csv"

file.exists(file.path(folder_rawData, file_observ_excel))
#----------------
# Define targets
#----------------

targets <- list(
  
  # define global configuration of parameters
  tar_target(
    config,
    list(
      folder_thisScript   = here::here(),
      folder_rawData      = here::here("InputFilesFromCloud"),
      folder_inputs    = here::here("..", "..", "inputs"),
      file_observ_excel   = "2024_WaggaWagga_PHDA24WARI2.xlsx",
      sheetExcel_weather  = "Weather",
      sheetExcel_haun     = "Haun stage ",
      coord_thisLatLon    = data.frame(lat = -35.041, lon = 147.319),
      target_stagePerc    = 50, # % of a stage development when event date is retrieved
      target_betwStages   = 50,  # % of period between two adjacent events when a synthetic event date is assumed
      file_input_name_saved = "DookiePhenoDatesInput_Wagga.csv"
    )
  ),
  
  # create met file
  tar_target(df_met, createWeatherFile(config$folder_rawData, file_observ_excel, 
                                       sheetExcel_weather, coord_thisLatLon)),
  
  # interpolate Haun stages
  tar_target(df_haun_interp, interpolateHaunStages(config$folder_rawData, 
                                                   file_observ_excel, sheetExcel_haun)),
  
  # create APSIM Stages as model Input
  tar_target(df_stage_param, createStageInputParameter(config$folder_thisScript, df_haun_interp)),
  
  # check which results to get based on csv meta-data file
  tar_target(df_obs_info,read.csv2(file.path(config$folder_thisScript, "observed_data_requirements.csv"),
             header = TRUE, stringsAsFactors = FALSE, sep = ",")),
  
  # Reads excel observations based on meta data above (raw as-is) and appends them into a single list of dfs 
  tar_target(list_observed_dfs,compile_all_observed(config$folder_rawData,file_observ_excel,df_obs_info)),
  
  ### Makes by-hand data corrections as needed to fix raw excel data (see func for details)
  tar_target(list_observed_clean, apply_corrections(list_observed_dfs)),
  
  ### Get sim names
  tar_target(df_simNameByCult,read.csv2(file.path(config$folder_thisScript, "CultivarToSimNameWaggaWagga2024.csv"),
                                   header = TRUE, stringsAsFactors = FALSE, sep = ",")),
  
  tar_target(df_final_observed, 
             prepare_final_observed(list_observed_clean,df_simNameByCult)), 
  
  tar_target(df_saved, 
             save_df_final(df_final_observed, config$folder_thisScript, 
                           "DookieWaggaWagga2024.xlsx")),
  
  # Target to filter and extract the PCDS data frames
  tar_target(df_list_PCDS, filter_and_extract_pcds(list_observed_dfs)),
  
  #' Interpolates observed PCDS observed variables across Date
  tar_target(df_PCDS_int, interpolate_obs_phenoStages(df_list_PCDS)),
  
  tar_target(StageTargetPerc, 50), # as Percentage
  
  # Finds a date when a target % for each stage is reached (BUG: name cut here)
  tar_target(df_dateStageTargetReached, findDateStageTarget(df_PCDS_int, StageTargetPerc)),
  
  # (i) Define the fraction for the distance between observed stages (0 to 1)
  # This replaces the hard-coded PercNewStages/0.50 from previous iterations.
  tar_target(BtwStgPerc, 50), # Assuming 50% (midpoint) is the default distance
  
  # Create synthetic in-between pheno stages within a APSIM format input file
  tar_target(df_apsimStageInput, doAPSIMStageInput(df_dateStageTargetReached, df_simNameByCult, BtwStgPerc)),
  
  tar_target(msgInputSaved, saveInputParam(df_apsimStageInput, folder_inputs, file_input_name_saved),
    format = "file" 
  )
)

#  return a pipeline object.
tar_pipeline(targets)

