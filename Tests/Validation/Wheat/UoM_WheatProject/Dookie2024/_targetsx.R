# _targets.R
library(targets)

# Source custom functions
source("R/functions.R")

# Set global options and required packages for the targets
tar_option_set(packages = c("readxl", "dplyr"))

# Define configuration parameters explicitly
config_params <- list(
  sheet = "WorkedForInput",
  col_range = "A:D", 
  date_cols = c(
    "[Wheat].Phenology.Emerging.DateToProgress",
    "[Wheat].Phenology.StemElongating.DateToProgress",
    "[Wheat].Phenology.Flowering.DateToProgress"
  )
)

# Define explicit relative paths
path_eva  <- "../../Dookie2024/DookieEVA2024.xlsx"
path_wwhi <- "../../Dookie2024/DookieWWHI2024.xlsx"
path_out  <- "../../inputs/Dookie2024PhenoDatesInput.csv" # from observations

# Pipeline Definition
list(
  # 1. Configuration Target
  tar_target(
    name = config,
    command = config_params
  ),
  
  # 2. Track Input Files (Updates pipeline if these files get modified)
  tar_target(
    name = file_eva,
    command = path_eva,
    format = "file"
  ),
  tar_target(
    name = file_wwhi,
    command = path_wwhi,
    format = "file"
  ),
  
  # 3. Process Data
  tar_target(
    name = df_eva,
    command = process_worked_input(file_eva, config)
  ),
  tar_target(
    name = df_wwhi,
    command = process_worked_input(file_wwhi, config)
  ),
  
  # 4. Combine and Export (Tracked Output File)
  tar_target(
    name = output_csv,
    command = combine_and_save(
      df_list = list(df_eva, df_wwhi), 
      output_path = path_out
    ),
    format = "file"
  )
)