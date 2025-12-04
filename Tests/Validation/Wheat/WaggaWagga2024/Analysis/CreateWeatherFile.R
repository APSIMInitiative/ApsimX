library(readxl)
library(dplyr)
library(lubridate)
library(glue)
library(rstudioapi)
library(stringr)

# Path to the Excel file
script_dir <- dirname(getActiveDocumentContext()$path)
file_path <- file.path(script_dir, "InputFilesFromCloud", "2024_WaggaWagga_PHDA24WARI2.xlsx")

# Read sheet "Weather"
Weather_raw <- read_excel(
  path = file_path,
  sheet = "Weather",
  col_types = "text"   # read everything as text first to control date parsing
)

# Convert first column to Date (dd/mm/yyyy)
Weather_worked <- Weather_raw %>%
  mutate(
    Clock.Today = as.Date(ymd("1899-12-30") + as.numeric(Date)),
    year=year(Clock.Today),
    day=yday(Clock.Today)
  ) %>%
  dplyr::rename(
    radn = !! rlang::sym(names(.)[str_detect(names(.), regex("radiation", ignore_case = TRUE))]),
    maxt = !! rlang::sym(names(.)[str_detect(names(.), regex("maximum", ignore_case = TRUE))]),
    mint = !! rlang::sym(names(.)[str_detect(names(.), regex("minimum", ignore_case = TRUE))]),
    rain = !! rlang::sym(names(.)[str_detect(names(.), regex("rain", ignore_case = TRUE))])
    
  ) %>%
  mutate(radn = as.numeric(as.character(radn)),
         maxt = as.numeric(as.character(maxt)),
         mint = as.numeric(as.character(mint)),
         rain = as.numeric(as.character(rain)),
         tav=((maxt+mint)*0.5),
         amp=maxt-mint)

# Calculate weather stats
stats_average <- Weather_worked %>%
  summarise(tav=round(mean(tav, 0)), amp=round(mean(amp), 0))
  
met_out <- Weather_worked %>%
  dplyr::select(year, day, radn, maxt,mint,rain)

# WaggaWagga  
lat <- -35.041
lon <- 147.319


# Build header text with dynamic tav and amp
header_met <- glue("
[weather.met.weather]
latitude = {lat}  (DECIMAL DEGREES)
longitude = {lon}   (DECIMAL DEGREES)
tav = {stats_average$tav} (oC) ! Annual average ambient temperature. Based on this met file.
amp = {stats_average$amp} (oC) ! Annual amplitude in mean monthly temperature. Based on this met file.

year  day  radn  maxt  mint  rain
()    ()   ()    ()    ()    ()
")

# Append weather data with no column names, no row names, space-separated
met_dir <- file.path(script_dir, "..", "..", "met") |> normalizePath()
outfileName <- file.path(met_dir, "WaggaWagga2024.met")

# Write header
writeLines(header_met, outfileName)


# Write file
write.table(
  met_out,
  file = outfileName,
  append = TRUE,
  sep = " ",
  quote = FALSE,
  col.names = FALSE,
  row.names = FALSE
)


