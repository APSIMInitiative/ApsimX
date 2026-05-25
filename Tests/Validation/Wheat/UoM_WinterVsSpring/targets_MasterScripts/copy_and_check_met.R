#' Quality Check and Copy APSIM MET Files
#'
#' @description
#' Reads an existing APSIM .met file, extracts the data matrix to run agronomical 
#' quality checks (e.g., NAs, Min > Max temps, negative radiation), and if it passes, 
#' copies the original file to a new destination with a new name.
#'
#' @param sourceFolder Character. Directory containing the original .met file.
#' @param targetFolder Character. Directory where the checked .met file should be saved.
#' @param orig_file_name Character. Name of the original file (e.g., "raw_weather.met").
#' @param new_file_name Character. Name for the output file (e.g., "GrassPatch_2025.met").
#'
#' @return The file path of the newly copied .met file, or stops with an error if checks fail.
#' @export
copy_and_check_met <- function(sourceFolder, targetFolder, orig_file_name, new_file_name) {
  
  require(dplyr)
  
  # 1. Define Paths
  source_path <- file.path(sourceFolder, orig_file_name)
  target_path <- file.path(targetFolder, new_file_name)
  
  if (!file.exists(source_path)) {
    stop(sprintf("CRITICAL: Original MET file not found at '%s'", source_path))
  }
  
  # 2. Extract Data Safely from the .met Text File
  # We read raw lines so we can skip the APSIM header variables (tav, amp, lat, etc.)
  met_lines <- readLines(source_path, warn = FALSE)
  
  # Find the row containing the column names (usually starts with "year")
  header_idx <- grep("^\\s*year\\s+day", met_lines, ignore.case = TRUE)
  
  if (length(header_idx) == 0) {
    stop("CRITICAL: Could not find standard 'year day' column headers in the MET file.")
  }
  
  # The data starts 2 lines after the header (Header -> Units -> Data)
  data_lines <- met_lines[(header_idx[1] + 2):length(met_lines)]
  
  # Read the raw data strings into a dataframe
  df_met <- read.table(text = data_lines, header = FALSE)
  
  # Extract and clean the column names from the header line
  raw_headers <- unlist(strsplit(trimws(met_lines[header_idx[1]]), "\\s+"))
  names(df_met) <- tolower(raw_headers)
  
  # 3. FIREWALL: Agronomical Quality Checks
  # Ensure the core columns actually exist before checking them
  req_cols <- c("maxt", "mint", "radn", "rain")
  if (!all(req_cols %in% names(df_met))) {
    stop(sprintf("CRITICAL: MET file is missing core columns. Expected: [%s]. Found: [%s]", 
                 paste(req_cols, collapse = ", "), 
                 paste(names(df_met), collapse = ", ")))
  }
  
  # Check 1: Missing Values
  if (any(is.na(df_met[req_cols]))) {
    stop("QUALITY FAIL: The MET file contains missing (NA) values in the core weather columns.")
  }
  
  # Check 2: Max Temp vs Min Temp
  bad_temps <- df_met %>% dplyr::filter(mint >= maxt)
  if (nrow(bad_temps) > 0) {
    stop(sprintf("QUALITY FAIL: Found %d day(s) where Minimum Temperature is >= Maximum Temperature.", nrow(bad_temps)))
  }
  
  # Check 3: Impossible Solar Radiation
  bad_radn <- df_met %>% dplyr::filter(radn < 0)
  if (nrow(bad_radn) > 0) {
    stop(sprintf("QUALITY FAIL: Found %d day(s) with negative solar radiation.", nrow(bad_radn)))
  }
  
  # Check 4: Impossible Rainfall
  bad_rain <- df_met %>% dplyr::filter(rain < 0)
  if (nrow(bad_rain) > 0) {
    stop(sprintf("QUALITY FAIL: Found %d day(s) with negative rainfall.", nrow(bad_rain)))
  }
  
  # 4. EXECUTE COPY
  # If the script makes it here, the data is clean.
  message("✅ MET data passed all quality checks.")
  
  if (!dir.exists(targetFolder)) {
    dir.create(targetFolder, recursive = TRUE)
  }
  
  # Remove the old file if it exists to ensure a clean overwrite
  if (file.exists(target_path)) {
    file.remove(target_path)
  }
  
  was_copied <- file.copy(from = source_path, to = target_path, overwrite = TRUE)
  
  if (!was_copied) {
    stop("CRITICAL: Quality checks passed, but Windows refused to copy the file. Check folder permissions or file locks.")
  }
  
  message(sprintf("Successfully verified and copied to: %s", new_file_name))
  return(target_path)
}