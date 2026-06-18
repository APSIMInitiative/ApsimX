#' Fetch and Patch Missing Meteorological Data (Direct SILO API)
#'
#' @description
#' Connects directly to the Queensland Government SILO API to request daily 
#' weather records for an Australian coordinate string. Includes full overlap protection.
#'
#' @param current_met Dataframe containing your current weather data.
#' @param lon Numeric. Longitude of the site.
#' @param lat Numeric. Latitude of the site.
#' @param start_date Character. Start of the missing gap (Format: "YYYY-MM-DD" or "YYYYMMDD").
#' @param end_date Character. End of the missing gap (Format: "YYYY-MM-DD" or "YYYYMMDD").
#' @return A complete, deduplicated weather dataframe ready for APSIM.
#' @export
patch_missing_met_gap <- function(current_met, lon, lat, start_date, end_date) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("lubridate", quietly = TRUE)) stop("Package 'lubridate' required.")
  
  # 1. Format dates cleanly for the SILO API (expects YYYYMMDD string format)
  s_date <- format(as.Date(start_date), "%Y%m%d")
  e_date <- format(as.Date(end_date), "%Y%m%d")
  
  message(sprintf("📡 Contacting SILO API directly for coords: [%.2f, %.2f]...", lon, lat))
  
  # 2. Construct the direct SILO URL request for APSIM format
  # We use the 'point' endpoint which interpolates data for a specific lat/lon
  silo_url <- sprintf(
    "https://silo.longpaddock.qld.gov.au/cgi-bin/silo/DownloadSiloData.pl?start=%s&stop=%s&format=apsim&lat=%.2f&lon=%.2f",
    s_date, e_date, lat, lon
  )
  
  # Create a secure temporary file spot to dump the download
  tmp_file <- tempfile(fileext = ".txt")
  
  # Download the file directly from the government servers
  tryCatch({
    utils::download.file(silo_url, destfile = tmp_file, quiet = TRUE)
  }, error = function(e) {
    stop("CRITICAL: Failed to connect to the SILO server. Check your internet connection or coordinates.")
  })
  
  # 3. Read the file into R, skipping the standard APSIM metadata headers
  # SILO files typically have headers that end where the [Constants] or data matrix begins
  raw_lines <- readLines(tmp_file)
  header_dash_line <- grep("^\\s*year\\s+day", raw_lines)
  
  if (length(header_dash_line) == 0) {
    stop("CRITICAL: SILO did not return a valid data matrix. Double check your latitude/longitude bounds.")
  }
  
  # Read table starting right from the column headers line
  fetched_df <- utils::read.table(
    text = paste(raw_lines[header_dash_line:length(raw_lines)], collapse = "\n"), 
    header = TRUE, 
    comment.char = "!"
  )
  
  # Clean up the temporary file from disk
  unlink(tmp_file)
  
  # Standardize column headers to lowercase to ensure absolute compatibility
  names(fetched_df) <- tolower(names(fetched_df))
  
  # Create our standard tracking date field
  fetched_df <- fetched_df %>%
    dplyr::select(year, day, radn, maxt, mint, rain) %>%
    dplyr::mutate(Date_Temp = as.Date(paste(year, day, sep = "-"), format = "%Y-%j"))
  
  # 4. Ensure the current data has a matching temporary date column
  if (!"Date_Temp" %in% names(current_met)) {
    current_met <- current_met %>%
      dplyr::mutate(Date_Temp = as.Date(paste(year, day, sep = "-"), format = "%Y-%j"))
  }
  
  # ==========================================================
  # OVERLAP PROTECTION
  # ==========================================================
  fetched_df_clean <- fetched_df %>%
    dplyr::filter(!Date_Temp %in% current_met$Date_Temp)
  
  overlap_count <- nrow(fetched_df) - nrow(fetched_df_clean)
  if (overlap_count > 0) {
    message(sprintf("⚠️  OVERLAP PREVENTED: Filtered out %d days that already exist in your file.", overlap_count))
  }
  
  # 5. Bind, Sort, and Clean Up
  complete_met <- dplyr::bind_rows(current_met, fetched_df_clean) %>%
    dplyr::arrange(year, day) %>%
    dplyr::select(-Date_Temp) 
  
  message(sprintf("✅ SUCCESS: Successfully stitched %d new records from SILO.", nrow(fetched_df_clean)))
  
  return(complete_met)
}