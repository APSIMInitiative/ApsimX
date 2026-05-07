#' Read and Process Weather Data from Excel
#'
#' Reads raw weather data, calculates daily values, extracts APSIM-required 
#' columns, and calculates annual average temperature (tav) and amplitude (amp).
#'
#' @param thisFolder Character. Directory containing the raw Excel file.
#' @param thisExcelFile Character. Name of the Excel file.
#' @param thisSheet Character. Name of the weather sheet.
#' @return A list containing `data` (the met dataframe), `tav`, and `amp`.
#' @export
createWeatherFile <- function(thisFolder, thisExcelFile, thisSheet) {
  
  require(dplyr)
  require(lubridate)
  require(stringr)
  require(readxl)
  require(rlang)
  
  # Path to the Excel file
  file_path <- file.path(thisFolder, thisExcelFile)
  
  if (!file.exists(file_path)) {
    stop("CRITICAL: Weather file not found: ", file_path)
  }
  
  # Read sheet "Weather"
  Weather_raw <- readxl::read_excel(
    path = file_path,
    sheet = thisSheet,
    col_types = "text"   # read everything as text first to control date parsing
  )
  
  # ------------------------------------------------------------------
  # THE FIREWALL: Safe & Generic Column Detection
  # ------------------------------------------------------------------
  # We use broad, generic regex to catch variations in column naming 
  # (e.g., "max", "tmax", "maximum", "rad", "radiation", "precip", "rain").
  raw_cols <- names(Weather_raw)
  
  col_radn <- grep("rad",         raw_cols, ignore.case = TRUE, value = TRUE)[1]
  col_maxt <- grep("max",         raw_cols, ignore.case = TRUE, value = TRUE)[1]
  col_mint <- grep("min",         raw_cols, ignore.case = TRUE, value = TRUE)[1]
  col_rain <- grep("rain|precip", raw_cols, ignore.case = TRUE, value = TRUE)[1]
  
  # Verify we actually found all required columns
  if (any(is.na(c(col_radn, col_maxt, col_mint, col_rain)))) {
    stop("CRITICAL: Missing required weather columns in the raw Excel data. Ensure Rad, Max, Min, and Rain/Precip are present.")
  }
  
  # ------------------------------------------------------------------
  # DATA PROCESSING
  # ------------------------------------------------------------------
  # Convert first column to Date (dd/mm/yyyy) and clean data
  Weather_worked <- Weather_raw %>%
    dplyr::mutate(
      Clock.Today = as.Date(lubridate::ymd("1899-12-30") + as.numeric(Date)),
      year = lubridate::year(Clock.Today),
      day = lubridate::yday(Clock.Today)
    ) %>%
    dplyr::rename(
      radn = !!rlang::sym(col_radn),
      maxt = !!rlang::sym(col_maxt),
      mint = !!rlang::sym(col_mint),
      rain = !!rlang::sym(col_rain)
    ) %>%
    dplyr::mutate(
      radn = as.numeric(radn),
      maxt = as.numeric(maxt),
      mint = as.numeric(mint),
      rain = as.numeric(rain),
      tav  = ((maxt + mint) * 0.5),
      amp  = maxt - mint
    )
  
  # Calculate weather stats required for the APSIM header
  stats_average <- Weather_worked %>%
    dplyr::summarise(
      tav = round(mean(tav, na.rm = TRUE), 1), 
      amp = round(mean(amp, na.rm = TRUE), 1)
    )
  
  # Select the final columns for APSIM and drop ghost rows
  met_out <- Weather_worked %>%
    dplyr::select(year, day, radn, maxt, mint, rain) %>%
    tidyr::drop_na()
  
  # Return a list containing the data and the calculated constants
  return(list(
    data = met_out,
    tav  = stats_average$tav,
    amp  = stats_average$amp
  ))
}