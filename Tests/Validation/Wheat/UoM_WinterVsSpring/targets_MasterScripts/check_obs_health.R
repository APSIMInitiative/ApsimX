#' Quality Control Gate for APSIM Observations
#'
#' @description
#' Runs strict diagnostic checks on the final APSIM dataframe before export. 
#' It checks for missing keys, duplicate dates, invalid time formats, and 
#' entirely empty data columns.
#'
#' @param df The final formatted observation dataframe.
#' @param numeric_lower_bound Numeric. Defaults to 0. Throws a warning if negative 
#'   values are found in biological data (e.g., negative biomass). Set to NULL to skip.
#'
#' @return The original dataframe (unaltered) if it passes all checks. 
#'   Stops the pipeline if critical errors are found.
#' @export
check_obs_health <- function(df, numeric_lower_bound = 0) {
  
  require(dplyr)
  require(stringr)
  
  # ====================================================================
  # 1. CORE COLUMN INTEGRITY
  # ====================================================================
  if (!all(c("SimulationName", "Clock.Today") %in% names(df))) {
    stop("QC FAILED: 'SimulationName' or 'Clock.Today' is missing from the final dataframe.")
  }
  
  if (any(is.na(df$SimulationName) | trimws(as.character(df$SimulationName)) == "")) {
    stop("QC FAILED: Blank or NA values found in 'SimulationName'.")
  }
  
  if (any(is.na(df$Clock.Today) | trimws(as.character(df$Clock.Today)) == "")) {
    stop("QC FAILED: Blank or NA values found in 'Clock.Today'.")
  }
  
  # ====================================================================
  # 2. THE APSIM TIMESTAMP FORMAT CHECK
  # ====================================================================
  # APSIM strictly expects: DD/MM/YYYY 00:00:00 (or similar time). 
  # This regex ensures it didn't accidentally stay as YYYY-MM-DD.
  valid_format <- stringr::str_detect(df$Clock.Today, "^\\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}:\\d{2}$")
  if (!all(valid_format)) {
    bad_dates <- head(df$Clock.Today[!valid_format], 3)
    stop(sprintf(
      "QC FAILED: 'Clock.Today' is not in the correct APSIM format (DD/MM/YYYY HH:MM:SS).\n  -> Example of bad format found: %s", 
      paste(bad_dates, collapse = ", ")
    ))
  }
  
  # ====================================================================
  # 3. THE DUPLICATE ROW CRASH PREVENTER
  # ====================================================================
  # APSIM will crash if a simulation has two observation rows for the exact same day.
  duplicates <- df %>%
    dplyr::group_by(SimulationName, Clock.Today) %>%
    dplyr::tally() %>%
    dplyr::filter(n > 1)
  
  if (nrow(duplicates) > 0) {
    bad_sims <- paste(head(paste(duplicates$SimulationName, duplicates$Clock.Today, sep=" on "), 3), collapse = ", ")
    stop(sprintf(
      "QC FAILED: Duplicate dates found for the same simulation. APSIM requires 1 row per date.\n  -> Duplicates found in: %s", 
      bad_sims
    ))
  }
  
  # ====================================================================
  # 4. PLAUSIBILITY & BOUNDARY CHECKS
  # ====================================================================
  # 4a. Check for columns that survived but are 100% empty
  empty_cols <- names(df)[purrr::map_lgl(df, ~ all(is.na(.x)))]
  if (length(empty_cols) > 0) {
    warning(sprintf(
      "QC WARNING: The following columns are entirely NA and will be exported blank: [%s]", 
      paste(empty_cols, collapse = ", ")
    ), call. = FALSE)
  }
  
  # 4b. Check for impossible negative numbers (Biomass, Height, Grain Yield)
  if (!is.null(numeric_lower_bound)) {
    num_cols <- names(df)[sapply(df, is.numeric)]
    for (col in num_cols) {
      if (any(df[[col]] < numeric_lower_bound, na.rm = TRUE)) {
        warning(sprintf(
          "QC WARNING: Found values below %s in column '%s'. Check for data entry errors.", 
          numeric_lower_bound, col
        ), call. = FALSE)
      }
    }
  }
  
  message("\u2705 QC PASSED: Observation dataframe is structurally sound and ready for APSIM.")
  
  # Return the data unaltered so it can pass through the pipeline
  return(df)
}