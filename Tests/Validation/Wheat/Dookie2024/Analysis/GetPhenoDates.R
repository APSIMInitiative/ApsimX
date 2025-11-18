# Load necessary packages
library(dplyr)
library(readr)
library(lubridate)
library(tidyr)
library(DBI)
library(RSQLite)
library(here)

#--------------------------------------------------------------------------------
# ------------- Get the original dates of phenological events --------------------
# Run .apsimx with SetPhenologyParams = DookieEVA2024_parameterOverwrites.csv 
# and      SetPhenologyParams = DookieWWHI2024_parameterOverwrites.csv
# -------------------------------------------------------------------------------

# Define the folder path
folder_db_path <- file.path("C:/github/ApsimX/Tests/Validation/Wheat/Dookie2024/Dookie2024_originalParameters.db")
#db_path <- here("Tests", "Validation", "Wheat", "Dookie2024", "Dookie2024_originalParameters.db")
#folder_db_path <- "../../Dookie2024/Dookie2024_originalParameters.db"

# Connect to the SQLite database
con <- dbConnect(RSQLite::SQLite(), folder_db_path)

#  List all available tables (optional, for checking)
dbListTables(con)

# Read the desired table into a dataframe
ReportPhenoDates <- dbReadTable(con, "ReportPhenoDates")
SimulationsMetaData <- dbReadTable(con, "_Simulations")

# ResultDB
originalPhenoDatesDB <- ReportPhenoDates %>%
  inner_join(SimulationsMetaData, by = c("SimulationID" = "ID")) %>%
  dplyr::rename(SimulationName = Name) %>%
 # mutate(across(where(is.character), as.factor)) %>%
  mutate(DateFormated=ymd_hms(Clock.Today))

# Close the connection
dbDisconnect(con)

# View the dataframe
head(originalPhenoDatesDB)

#--------------------------------------------------------------
# ------------- Prepare Input Parameters with DATES of pheno events
# -------------------------------------------------------------

# Define the folder paths
folder_wheat_path <- "C:/github/ApsimX/Tests/Validation/Wheat/inputs"
#folder_wheat_path <- "../../Wheat/inputs"
# folder_base_path <- "Dookie2024/Analysis"
# folder_inputData_path <- "/PhenoDatesExtracted"
# phenoData_file <- "PhenoPhaseList.csv"


# Read Pheno Stages from CSV - test version ---------------------
# List all CSV files in the folder
# ------------------------------------------------------

# file_list <- list.files(path = file.path(folder_wheat_path, 
#                                          folder_base_path,
#                                          folder_inputData_path), pattern = "\\.csv$", 
#                         full.names = TRUE)
# file_list

# Merge all data frames by matching column names
# These dates of APSIM Stages were obtained with original external parameteriation from Hamish
# by copy/paste from APSIM GUI
# merged_data <- file_list %>%
#   lapply(read_csv, show_col_types = FALSE) %>%
#   bind_rows() %>%
#   mutate(across(where(is.character), as.factor)) %>%
#   mutate(DateFormated=dmy(Clock.Today))

# ---------------------------
# Get metadata on pheno-stages
# ----------------------------

# pheno_stages <- read.csv2(file.path(folder_base_path, phenoData_file), 
#                           header = TRUE, stringsAsFactors = TRUE, sep = ",")

# Create list of variable names for set up
# Check these with Hamish
# list_of_phases <- data.frame(
#   Wheat.Phenology.Stage = c(2,4,5,6,7),
#   VariableName = c("[Wheat].Phenology.Emerging.DateToProgress",
#              "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress",
#              "[Wheat].Phenology.StemElongating.DateToProgress",
#              "[Wheat].Phenology.Heading.DateToProgress",
#              "[Wheat].Phenology.Flowering.DateToProgress"
# ))
# 
# list_of_phases_range <- list_of_phases %>%
#   mutate(
#     StageMin = Wheat.Phenology.Stage,
#     StageMax = Wheat.Phenology.Stage + 1
#   )
# 

# ---------------------------------------
# Attribute parameter name to each Stage
# -------------------------------------

# data_with_phases <- merged_data %>% # test version
data_with_phases <- originalPhenoDatesDB %>%
  mutate(DateFormated = ymd_hms(Clock.Today),
         Wheat.Phenology.Stage = as.numeric(Wheat.Phenology.Stage)) %>%
  mutate(
    ParameterName = case_when(
      between(Wheat.Phenology.Stage, 3, 4) ~ "[Wheat].Phenology.Emerging.DateToProgress",#2/3
      between(Wheat.Phenology.Stage, 5, 6) ~ "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress",#4/5
      between(Wheat.Phenology.Stage, 6, 7) ~ "[Wheat].Phenology.StemElongating.DateToProgress",#5/6
      between(Wheat.Phenology.Stage, 7, 8) ~ "[Wheat].Phenology.Heading.DateToProgress",#6/7
      between(Wheat.Phenology.Stage, 8, 9) ~ "[Wheat].Phenology.Flowering.DateToProgress",#7/8
      # between(Wheat.Phenology.Stage, 2, 3) ~ "[Wheat].Phenology.Emerging.DateToProgress",#2/3
      # between(Wheat.Phenology.Stage, 4, 5) ~ "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress",#4/5
      # between(Wheat.Phenology.Stage, 5, 6) ~ "[Wheat].Phenology.StemElongating.DateToProgress",#5/6
      # between(Wheat.Phenology.Stage, 6, 7) ~ "[Wheat].Phenology.Heading.DateToProgress",#6/7
      # between(Wheat.Phenology.Stage, 7, 8) ~ "[Wheat].Phenology.Flowering.DateToProgress",#7/8
      TRUE ~ "Unknown" # Catch-all for any other values
    )) %>%
  filter(ParameterName != "Unknown")
  

# find data of phase occurrence
df_result <- data_with_phases %>%
  group_by(SimulationName, ParameterName) %>%
  summarise(
    DateToProgress = min(DateFormated, na.rm = TRUE),
   #DateToProgress = max(DateFormated, na.rm = TRUE),
    .groups = "drop"
  )

# reshape for APSIM
df_result_apsim <- df_result %>%
  mutate(DateToProgressAPSIM = format(DateToProgress, "%d-%b-%Y")) %>%
  dplyr::select(-DateToProgress) %>%
  pivot_wider(
    names_from = ParameterName,
    values_from = DateToProgressAPSIM
  ) %>%
  select(
    contains("Simulation"),
    contains("Emerging"),
    contains("Spike"),
    contains("Stem"),
    contains("Heading"),
    contains("Flowering"),
    everything()
  )

print(df_result_apsim)


write.csv(df_result_apsim, 
          file.path(folder_wheat_path,
                    "DookiePhenoDatesInput_SIM.csv"),
          row.names = FALSE, quote = FALSE)


#------------------------------------------------
#--- Merge observation dates when available -----
#-----------------------------------------------
# # read file
# df_obs_dates <- read.csv2(file.path(folder_wheat_path, "ObservedPhenoDates.csv"), 
#                               header = TRUE, stringsAsFactors = TRUE, sep = ",",
#                           , check.names = FALSE)
# 
# # do the merge (use observations when available)
# df_result_apsim_with_obs <- df_result_apsim %>%
#   full_join(df_obs_dates, by = "SimulationName", suffix = c("_A", "_B")) %>%
#   mutate(
#     `[Wheat].Phenology.Emerging.DateToProgress` =
#       coalesce(`[Wheat].Phenology.Emerging.DateToProgress_B`,
#                `[Wheat].Phenology.Emerging.DateToProgress_A`),
#     
#     `[Wheat].Phenology.StemElongating.DateToProgress` =
#       coalesce(`[Wheat].Phenology.StemElongating.DateToProgress_B`,
#                `[Wheat].Phenology.StemElongating.DateToProgress_A`),
#     
#     `[Wheat].Phenology.Flowering.DateToProgress` =
#       coalesce(`[Wheat].Phenology.Flowering.DateToProgress_B`,
#                `[Wheat].Phenology.Flowering.DateToProgress_A`)
#   ) %>%
#   # remove only the temporary columns ending with _A and _B
#   select(
#     -ends_with("_A"),
#     -ends_with("_B")
#   ) %>%
#   select(
#     contains("Simulation"),
#     contains("Emerging"),
#     contains("Spike"),
#     contains("Stem"),
#     contains("Heading"),
#     contains("Flowering"),
#     everything()
#   )
# 
# 
# write.csv(df_result_apsim_with_obs, 
#           file.path(folder_wheat_path,
#                     "DookiePhenoDatesInput_WithObs.csv"),
#                   #  "DookiePhenoDatesInput.csv"),
#           row.names = FALSE, quote = FALSE)
