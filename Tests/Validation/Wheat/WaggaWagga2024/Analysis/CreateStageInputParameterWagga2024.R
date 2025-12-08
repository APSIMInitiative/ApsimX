# Load necessary packages
library(dplyr)
library(readr)
library(lubridate)
library(tidyr)
library(DBI)
library(RSQLite)
library(here)

script_dir <- dirname(getActiveDocumentContext()$path)
file_path <- file.path(script_dir, "InputFilesFromCloud", "2024_WaggaWagga_PHDA24WARI2.xlsx")

# Define the folder path for APSIM runs with original parameters
input_path <- file.path(script_dir, "..", "..", "inputs") |> normalizePath()

wagga_path <-  file.path(script_dir, "..", "Analysis") |> normalizePath()

# Read observed haun stages and APSIM simulation names
df_interp_obs_dates <- read.csv2(file.path(input_path, "Wagga2024PhenoStagesInterpolated.csv"),
                          header = TRUE, stringsAsFactors = TRUE, sep = ",",
                          , check.names = FALSE)


df_sim_names <- read.csv2(file.path(wagga_path, "CultivarToSimNameWaggaWagga2024.csv"),
                                 header = TRUE, stringsAsFactors = TRUE, sep = ",",
                                 , check.names = FALSE)


# View the dataframe with simulated Pheno Stages
head(df_interp_obs_dates)

#--------------------------------------------------------------
# ------------- Prepare Input Parameters with DATES of pheno events
# -------------------------------------------------------------

# ---------------------------------------
# Attribute parameter name to each Stage
# -------------------------------------
data_with_phases <- df_interp_obs_dates %>%
  mutate(DateFormated = ymd(Date),
         Wheat.Phenology.Stage = as.numeric(as.character(APSIM_Stage))) %>%
  mutate(
    ParameterName = case_when(
      between(Wheat.Phenology.Stage, 3, 4) ~ "[Wheat].Phenology.Emerging.DateToProgress",#2/3
      between(Wheat.Phenology.Stage, 5, 6) ~ "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress",#4/5
      between(Wheat.Phenology.Stage, 6, 7) ~ "[Wheat].Phenology.StemElongating.DateToProgress",#5/6
      between(Wheat.Phenology.Stage, 7, 8) ~ "[Wheat].Phenology.Heading.DateToProgress",#6/7
      between(Wheat.Phenology.Stage, 8, 9) ~ "[Wheat].Phenology.Flowering.DateToProgress",#7/8
      TRUE ~ "Unknown" # Catch-all for any other values
    )) %>%
  filter(ParameterName != "Unknown")

unique(data_with_phases$Cultivar)

# find first date of phase-range occurrence
df_result <- data_with_phases %>%
  group_by(Cultivar,Block, ParameterName) %>%
  summarise(
    DateToProgress = min(DateFormated, na.rm = TRUE),
    .groups = "drop"
  ) %>%
  ungroup() %>%
  group_by(Cultivar, ParameterName) %>%
  summarise(
    DateToProgress = min(DateToProgress, na.rm = TRUE),
    .groups = "drop"
  ) %>%
  arrange(DateToProgress)

# reshape for APSIM parameter input format
df_result_apsim <- df_result %>%
  mutate(DateToProgressAPSIM = format(DateToProgress, "%d-%b-%Y")) %>%
  dplyr::select(-DateToProgress) %>%
  
  inner_join(df_sim_names)%>%
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
  ) %>%
  mutate(across(contains("Flowering"),
                ~ ifelse(is.na(.), max(., na.rm = TRUE), .)))%>%
  dplyr::select(-Cultivar)

print(df_result_apsim)

# These are dates simulated with original parameterisation
write.csv(df_result_apsim, 
          file.path(input_path,
                    "DookiePhenoDatesInput_Wagga.csv"),
          row.names = FALSE, quote = FALSE)

