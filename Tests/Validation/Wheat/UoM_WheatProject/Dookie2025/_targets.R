library(targets)
library(here)
# Dookie 2025

# libraries
tar_option_set(packages = c("here", "tidyverse", "lubridate", "readxl"))

# functions

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


# source("R/read_and_merge_phenology_observed.R")
# source("R/check_manual_params.R")
# source("R/read_csv_sowDates.R")
# source("R/find_first_stage_dates.R")
# source("R/interpolate_and_create_phenoStages.R")
# source("R/spread_and_csv_save.R")
# source("R/read_and_merge_obs_files.R")
# source("R/save_df_to_excel.R")
# source("R/check_project_dependencies.R")




# Read Wheat.Phenology.Stage from Observed (2 sets)
# Organise in spread format
# interpolate for non-available phases
# set a haun manual-input check


# ADD NEW MET DATA TO THIS ONE!!!!!!!!!!!!!!!!!!!!!!!!!!! - Same for EVA and WWHI
# FIND EMERGENCE AND POPULATION FOR EACH TREAT TO BE ADDED INTO APSIMX SIM

#-------------------

proj_name <- "Dookie2025"


# target objects
list(
  # all configuration parameters
  tar_target(
    config,
    list(
      # folders and file names
      proj_name                   = proj_name, 
      folder_thisScript           = here::here(),
      folder_inputs               = here::here("..", "inputs"),
      folder_met                  = here::here("..", "met"),
      folder_apsimx               = here::here(), # a level up from where Analysis is
      folder_rawData              = here::here(proj_name),  
      file_rawData_excel          = c("UOM2312-001RTX 25 DOO JH EVA WHT.xlsx", 
                                      "UOM2312-001RTX 25 DOO JH WWHI WHT.xlsx"), # raw observed data (pre-defined file name),
      sheetExcel_weather          = "Met Data",
      coord_thisLatLon            = data.frame(lat = -36.39, lon = 145.70), # To check
      file_metaData_observed      = paste0("Observed_data_requirements.csv"),# pre-defined list of obs vars to fetch
      file_saved_obs_excel         = paste0(proj_name,"_Observed.xlsx"), #produced observed data (pre-defined file name)
      sheet_name_observed         = "Observed",
      date_DOY_ref                = "01-01-2025", # date to transform DOY output into ddmmyy within simulations
      target_stageDatePerc        = 50, # Percentage of phenological-stage development when its date is taken
      target_btwStagesPerc        = 50, # Percentage of time in-between two pheno-stages when we assume a missing stage for interpolation
      file_name_input_pheno       = paste0(proj_name,"_PhenoDatesInput.csv"),
      file_name_input_haun        = paste0(proj_name,"_HaunStagesInput.csv"),
      cols_to_extract             = c("SimulationName",
                                         "Clock.Today",
                                         "Wheat.Phenology.HaunStage",
                                         "Wheat.Phenology.Stage"),
      file_name_cult_by_sowDate  = "CultivarBySowingDatesTemplate.csv"
        )),
  
  # # Config: Get pre-defined simulation names per treatment from APSIM file (via APSIM-UI) - NOTE: func this might have to change with exp
  tar_target(df_simNameByCult,read.csv2(file.path(config$folder_rawData,
                                                  config$file_name_cult_by_sowDate),
                                        header = TRUE, stringsAsFactors = FALSE, sep = ",")),
  
  # 1. Process the weather data
  tar_target(
    name = processed_met_data, 
    command = createWeatherFile(
      thisFolder = config$folder_rawData, 
     # thisExcelFile = config$file_rawData_excel[[2]], # data only in WWHI excel file
     # Search the array for "WWHI". The [1] ensures it only takes the first match
     # just in case multiple files accidentally contain that string.
      thisExcelFile = grep("WWHI", config$file_rawData_excel, value = TRUE)[1],
      thisSheet = config$sheetExcel_weather
    )
  ),
  
  # 2. Save the weather file
  tar_target(
    name = msg_met_saved,
    command = save_met_file(
      met_list = processed_met_data,
      folder_path = config$folder_met,
      file_name = paste0(config$proj_name, ".met"),
      lat = config$coord_thisLatLon$lat,
      lon = config$coord_thisLatLon$lon
    ),
    format = "file" # Track the saved output!
  ),
  
  ### -------------------------------------------------------------------------------
  ### Prepare excel data with observation in APSIM format to compare with simulations
  ### -------------------------------------------------------------------------------
  
  # check which observed data is needed to use based on a hand-made csv meta-data file
  tar_target(df_obs_info,read.csv2(file.path(config$folder_rawData, 
                                             config$file_metaData_observed),
                                   header = TRUE, stringsAsFactors = FALSE, sep = ",")),
  
  # Reads excel raw observations based on meta data above (raw as-is) and appends them into a single list of dfs 
  tar_target(list_observed_dfs_raw,compile_all_observed(config$folder_rawData,
                                                    config$file_rawData_excel,
                                                    df_obs_info)),

  # All reading ok up until here
  
  # 2. Map observations to Simulations
  tar_target(
    name = list_observed_dfs,
    command = attach_sim_names(list_observed_dfs_raw, df_simNameByCult)
  ),
  
  ### ----------------------------------------------------------------------------------------
  ### Create APSIM stage parameters as FORCED input - AND add it as synthetic data to observations (stages_raw)
  ### ----------------------------------------------------------------------------------------
  
  # Filter and extract the PCDS pheno-stages observed from excel raw data
  tar_target(df_list_PCDS, filter_and_extract_pcds(list_observed_dfs)),
  
  #' Interpolates observed PCDS observed variables across Date
  tar_target(df_PCDS_int, interpolate_obs_phenoStages(df_list_PCDS)),
  
  # Finds a date when a target % for each stage is reached
  tar_target(df_dateStageTargetReached, findDateStageTarget(df_PCDS_int,
                                                            config$target_stageDatePerc)),
  tar_target(
    name = df_apsimStageInput,
    command = doAPSIMStageInput(
      df_dateWhenStageWasReached = df_dateStageTargetReached, 
      BtwStgPerc = config$target_btwStagesPerc, 
      fill_NAs_with_average = TRUE #<--- CHANGE THIS TO FALSE FOR FINAL ANALYSIS
    ) ),
  
  # Save parameter input file with forced pheno-dates into /input
  tar_target(msg_param_saved, saveInputParam(df_apsimStageInput, 
                                             config$folder_inputs, 
                                             config$file_name_input_pheno),
             format = "file"),
  
  
  # Create Observed data of pheno-stages to be added to observations (as cross-check)
  tar_target(df_stages_Observ, doStageObsData(df_dateStageTargetReached,
                                              "Wheat.Phenology.Stage")),
  
  
  ### ----------------------------------------------------------------------------------------
  ### Finish observation file to be read by APSIM
  ### ----------------------------------------------------------------------------------------
  
  # Add stage to list of observed data and clean metadata
  tar_target(list_observed_dfs_clean, add_to_observed(list_observed_dfs,
                                                              df_stages_Observ,"phenology_stage_raw")),
  
    # Prepare the format of a APSIM observation standard file
  tar_target(df_observed_wide, 
             prepare_observed_final(list_observed_dfs_clean)),
  
  #Add Wheat.Phenology.CurrentStageName as new variable with value HarvestRipe for 1:1 graph analysis
  tar_target(df_observed_wide_harv, add_harv_into_obs(df_observed_wide,
                                                     "Wheat.Phenology.CurrentStageName",
                                                     "HarvestRipe")),
    
  # save the output as APSIM likes to read it
  tar_target(msg_obs_saved, 
             save_df_final(df_observed_wide_harv, 
                           config$folder_apsimx, 
                           config$file_saved_obs_excel)),
  
  
  ### -------------------------------------------------------------------------------
  ### Extract key parameters for manual addition into .apsimx simulation
  ### -------------------------------------------------------------------------------
  
  # check if external/manual HAUN parameters are correct (and create template if not)
  tar_target(haun_input_checked, check_manual_params(config$folder_inputs,
                                                     config$file_name_input_haun,
                                                     df_observed_wide))
  
  
  # Find emergence dates and population
  # # Config: Get pre-defined simulation names per treatment from APSIM file (via APSIM-UI) - NOTE: func this might have to change with exp
  #tar_target(df_em_pop,get_em_pop())
  # 
  # 
  # # read observations of phenology and haun
  # tar_target(file_pheno_haun_obs, read_and_merge_phenology_observed(config$folder_rawData,
  #                                              config$file_rawData_excel, 
  #                                              config$sheet_name_observed,
  #                                              config$cols_to_extract)),
  # 
  # 
  # 
  # # check if haun manual-parameters are correct
  # tar_target(msg_haun_input_checked, check_manual_params(config$folder_inputs,
  #                                                    config$file_name_input_haun,
  #                                                    file_pheno_haun_obs)),
  #   
  # # prepare the phenology input 
  # tar_target(df_sowDates_by_sim, read_csv_sowDates(config$folder_rawData,
  #                                            config$file_name_cult_by_sowDate)),
  # 
  #  tar_target(df_pheno_start_date, 
  #             find_first_stage_dates(df_sowDates_by_sim, 
  #                                                file_pheno_haun_obs, 
  #                                                sow_date_col = "SowingDate")),
  # tar_target(df_pheno_interp, 
  #            interpolate_and_create_phenoStages(df_pheno_start_date, config$btwStgFrac)),
  # 
  # tar_target(msg_pheno_input_saved, spread_and_csv_save(config$folder_inputs, 
  #                                                              config$file_name_input_pheno, 
  #                                                  df_pheno_interp)),
  # 
  # tar_target(df_all_obs_files, read_and_merge_obs_files(file.path(config$folder_rawData),
  #                                                       config$file_rawData_excel,
  #                                                       config$sheet_name_observed)),
  # 
  # tar_target(msg_obs_saved,save_df_to_excel(config$folder_apsimx,
  #                                            config$file_saved_obs_excel, 
  #                                            config$sheet_name_observed, 
  #                                            df_all_obs_files)),
  # 
  # # Post-flight dependency check for APSIM
  # tar_target(
  #   name = check_depend, 
  #   command = {
  #     # 1. List the targets here to force `{targets}` to wait for them
  #     msg_obs_saved
  #     msg_haun_input_checked
  #     msg_pheno_input_saved
  #     
  #     # 2. Now run the actual function
  #     check_project_dependencies(
  #       projects = config$proj_name,
  #       dir_met = config$folder_met,
  #       dir_inputs = config$folder_inputs,
  #       dir_obs = config$folder_apsimx
  #     )
  #   }
  # )
  
  
)
