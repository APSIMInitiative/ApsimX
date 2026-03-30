#' Save Processed Weather Data to APSIM .met format
#'
#' @param met_list List. Output from createWeatherFile() containing data, tav, and amp.
#' @param folder_path Character. Directory to save the .met file.
#' @param file_name Character. Target file name (e.g., "WaggaWagga2024.met").
#' @param lat Numeric. Latitude for the header.
#' @param lon Numeric. Longitude for the header.
#' @return Character. The full path to the saved file.
#' @export
save_met_file <- function(met_list, folder_path, file_name, lat, lon) {
  
  dir.create(folder_path, showWarnings = FALSE, recursive = TRUE)
  outfileName <- file.path(folder_path, file_name)
  
  # Build header text using the stats from our processed list
  header_met <- glue::glue("
[weather.met.weather]
latitude = {lat}  (DECIMAL DEGREES)
longitude = {lon}   (DECIMAL DEGREES)
tav = {met_list$tav} (oC) ! Annual average ambient temperature. Based on this met file.
amp = {met_list$amp} (oC) ! Annual amplitude in mean monthly temperature. Based on this met file.

year  day  radn  maxt  mint  rain
()    ()   ()    ()    ()    ()
")
  
  # Write the header
  writeLines(header_met, outfileName)
  
  # Append the actual dataframe
  write.table(
    met_list$data,
    file = outfileName,
    append = TRUE,
    sep = " ",
    quote = FALSE,
    col.names = FALSE,
    row.names = FALSE
  )
  
  return(outfileName)
}