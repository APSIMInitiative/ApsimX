#' Read and Process Weather Data from Excel
#'
#' Reads raw weather data, calculates daily values, extracts APSIM-required 
#' columns, and calculates annual average temperature (tav) and amplitude (amp).
#' Automatically detects and scrubs invalid "ghost rows" at the end of the file.
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
  # Protects against minor naming variations across different project files
  raw_cols <- names(Weather_raw)
  
  col_radn <- grep("rad",         raw_cols, ignore.case = TRUE, value = TRUE)[1]
  col_maxt <- grep("max",         raw_cols, ignore.case = TRUE, value = TRUE)[1]
  col_mint <- grep("min",         raw_cols, ignore.case = TRUE, value = TRUE)[1]
  col_rain <- grep("rain|precip", raw_cols, ignore.case = TRUE, value = TRUE)[1]
  
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
      radn = as.numeric(as.character(radn)),
      maxt = as.numeric(as.character(maxt)),
      mint = as.numeric(as.character(mint)),
      rain = as.numeric(as.character(rain)),
      tav  = ((maxt + mint) * 0.5),
      amp  = maxt - mint
    )
  
  # Calculate weather stats required for the APSIM header
  stats_average <- Weather_worked %>%
    dplyr::summarise(
      tav = round(mean(tav, na.rm = TRUE), 1), 
      amp = round(mean(amp, na.rm = TRUE), 1)
    )
  
  # ------------------------------------------------------------------
  # THE GHOST BUSTER: Filter and Warn
  # ------------------------------------------------------------------
  # First, select the target columns
  met_selected <- Weather_worked %>%
    dplyr::select(year, day, radn, maxt, mint, rain)
  
  # Next, aggressively filter out the ghost rows
  met_out <- met_selected %>%
    dplyr::filter(!is.na(year) & !is.na(day)) %>%
    dplyr::filter(!(is.na(radn) & is.na(maxt) & is.na(mint) & is.na(rain)))
  
  # Calculate exactly how many rows were destroyed
  ghost_count <- nrow(met_selected) - nrow(met_out)
  
  # If we killed any ghosts, sound the alarm!
  if (ghost_count > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " \u26A0\uFE0F GHOST ROWS DETECTED AND DELETED IN WEATHER DATA \u26A0\uFE0F ",
      "======================================================================",
      sprintf(" File: %s | Sheet: %s", thisExcelFile, thisSheet),
      sprintf(" Action: %d empty/invalid row(s) were scrubbed from the end of the file.", ghost_count),
      " Note: APSIM will now run safely, but check the bottom of your raw Excel",
      "       sheet for stray spaces or accidental keystrokes.",
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    
    # Trigger a native warning so targets flags it in tar_meta(fields = warnings)
    warning(sprintf("%d ghost rows were deleted from %s.", ghost_count, thisExcelFile), call. = FALSE)
  }
  
  # Return a list containing the data and the calculated constants
  return(list(
    data = met_out,
    tav  = stats_average$tav,
    amp  = stats_average$amp
  ))
}