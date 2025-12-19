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

base_path <- file.path("C:/github")

# Define the folder path for APSIM runs with original parameters
folder_db_path <- file.path(base_path, 
                            "ApsimX/Tests/Validation/Wheat/Dookie2024/Dookie2024_originalParameters.db")

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
  mutate(DateFormated=ymd_hms(Clock.Today))

# Close the connection
dbDisconnect(con)

# View the dataframe with simulated Pheno Stages
head(originalPhenoDatesDB)

#--------------------------------------------------------------
# ------------- Prepare Input Parameters with DATES of pheno events
# -------------------------------------------------------------

# Define the folder paths
input_path <- file.path(base_path,"ApsimX/Tests/Validation/Wheat/inputs")


# ---------------------------------------
# Attribute parameter name to each Stage
# -------------------------------------
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
      TRUE ~ "Unknown" # Catch-all for any other values
    )) %>%
  filter(ParameterName != "Unknown")
  

# find first date of phase-range occurrence
df_result <- data_with_phases %>%
  group_by(SimulationName, ParameterName) %>%
  summarise(
    DateToProgress = min(DateFormated, na.rm = TRUE),
    .groups = "drop"
  )

# reshape for APSIM parameter input format
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

# These are dates simulated with original parameterisation
write.csv(df_result_apsim, 
          file.path(input_path,
                    "DookiePhenoDatesInput_SIM.csv"),
          row.names = FALSE, quote = FALSE)

# --------------------------------------------------------------
# 1) Replace APSIM dates with observed dates where available
# --------------------------------------------------------------

# read observed phenological dates file
df_obs_dates <- read.csv2(file.path(input_path, "DookiePhenoDates_Observed.csv"),
                          header = TRUE, stringsAsFactors = TRUE, sep = ",",
                          , check.names = FALSE)

# NOTE: We often have Pheno stages in simulation running "ahead" of date of next observation input
# We fix this here by interpolating the in-between observed phases when these are ahead of observed

# midpoint function
midpoint = function(a, b) as.Date(a + (b - a)/2)


# Let's merge observed values giving priority to Observations when available
# If a previous phase has a date ahead, take the mid-point between observations instead
df_result_apsim_with_obs <- df_result_apsim %>%
  full_join(df_obs_dates, by = "SimulationName", suffix = c("_A", "_B")) %>%
  

mutate(
  `[Wheat].Phenology.Emerging.DateToProgress` =
    coalesce(`[Wheat].Phenology.Emerging.DateToProgress_B`,
             `[Wheat].Phenology.Emerging.DateToProgress_A`),
  
  `[Wheat].Phenology.StemElongating.DateToProgress` =
    coalesce(`[Wheat].Phenology.StemElongating.DateToProgress_B`,
             `[Wheat].Phenology.StemElongating.DateToProgress_A`),
  
  `[Wheat].Phenology.Flowering.DateToProgress` =
    coalesce(`[Wheat].Phenology.Flowering.DateToProgress_B`,
             `[Wheat].Phenology.Flowering.DateToProgress_A`)
) %>%
  
  # Remove A/B temporary duplicates
  select(-ends_with("_A"), -ends_with("_B")) %>%
  
  # order for checking
  dplyr::select(
    contains("Simulation"),
    contains("Emerging"),
    contains("Spike"),
    contains("Stem"),
    contains("Heading"),
    contains("Flowering"),
    everything()
  ) %>%
  
  # --------------------------------------------------------------
# 2) PHENOLOGY CONSISTENCY CHECK
# --------------------------------------------------------------
rowwise() %>%
  mutate(
    Spike_fixed = {
      emerg <- as.Date(`[Wheat].Phenology.Emerging.DateToProgress`, format = "%d-%b-%Y") 
      spike  <- as.Date(`[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress`, format = "%d-%b-%Y")
      stem   <- as.Date(`[Wheat].Phenology.StemElongating.DateToProgress`, format = "%d-%b-%Y")
      if (!is.na(spike) && !is.na(stem) && spike > stem) midpoint(emerg,stem) else spike
    },
    Heading_fixed = {
      stem   <- as.Date(`[Wheat].Phenology.StemElongating.DateToProgress`, format = "%d-%b-%Y")
      heading   <- as.Date(`[Wheat].Phenology.Heading.DateToProgress`, format = "%d-%b-%Y")
      flowering <- as.Date(`[Wheat].Phenology.Flowering.DateToProgress`, format = "%d-%b-%Y")
      if (!is.na(heading) && !is.na(flowering) && heading > flowering) midpoint(stem,flowering) else heading
    }
  ) %>%
  ungroup() %>%
  
  # Convert fixed dates back to the dd-mmm-yyyy format
  mutate(
    `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress` =
      format(Spike_fixed, "%d-%b-%Y"),
    `[Wheat].Phenology.Heading.DateToProgress` =
      format(Heading_fixed, "%d-%b-%Y")
  ) %>%
  
  # Drop helper columns
  select(-Spike_fixed, -Heading_fixed) %>%
  
  # --------------------------------------------------------------
# 3) Final ordering
# --------------------------------------------------------------
select(
  contains("Simulation"),
  contains("Emerging"),
  contains("Spike"),
  contains("Stem"),
  contains("Heading"),
  contains("Flowering"),
  everything()
)

# Save input parameter dates with obsvations and synthetic values
write.csv(df_result_apsim_with_obs,
          file.path(input_path,
                    "DookiePhenoDatesInput.csv"),
          row.names = FALSE, quote = FALSE)
