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
  
  # Path to the Excel file
  file_path <- file.path(thisFolder, thisExcelFile)
  
  if (!file.exists(file_path)) {
    stop("Weather file not found: ", file_path)
  }
  
  # Read sheet "Weather"
  Weather_raw <- readxl::read_excel(
    path = file_path,
    sheet = thisSheet,
    col_types = "text"   # read everything as text first to control date parsing
  )
  
  # Convert first column to Date (dd/mm/yyyy) and clean data
  Weather_worked <- Weather_raw %>%
    dplyr::mutate(
      Clock.Today = as.Date(lubridate::ymd("1899-12-30") + as.numeric(Date)),
      year = lubridate::year(Clock.Today),
      day = lubridate::yday(Clock.Today)
    ) %>%
    dplyr::rename(
      radn = !! rlang::sym(names(.)[stringr::str_detect(names(.), stringr::regex("radiation", ignore_case = TRUE))]),
      maxt = !! rlang::sym(names(.)[stringr::str_detect(names(.), stringr::regex("maximum", ignore_case = TRUE))]),
      mint = !! rlang::sym(names(.)[stringr::str_detect(names(.), stringr::regex("minimum", ignore_case = TRUE))]),
      rain = !! rlang::sym(names(.)[stringr::str_detect(names(.), stringr::regex("rain", ignore_case = TRUE))])
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
  
  # Select the final columns for APSIM
  met_out <- Weather_worked %>%
    dplyr::select(year, day, radn, maxt, mint, rain)
  
  # Return a list containing the data and the calculated constants
  return(list(
    data = met_out,
    tav  = stats_average$tav,
    amp  = stats_average$amp
  ))
}