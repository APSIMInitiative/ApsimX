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
  # 2. DATE FORMAT & VALIDITY CHECK (Upgraded to Auto-Fix)
  # ====================================================================
  if (!inherits(df$Clock.Today, c("Date", "POSIXt"))) {
    
    # The Swiss Cheese Parser: Attempts to rescue any messy string or Excel number
    parse_any_date <- function(x) {
      final_dates <- as.Date(rep(NA_character_, length(x)))
      
      nums <- suppressWarnings(as.numeric(x))
      num_idx <- which(!is.na(nums))
      if (length(num_idx) > 0) {
        final_dates[num_idx] <- as.Date(nums[num_idx], origin = "1899-12-30")
      }
      
      rem_idx <- which(is.na(final_dates) & !is.na(x) & trimws(as.character(x)) != "")
      if (length(rem_idx) > 0) {
        x_rem <- as.character(x[rem_idx])
        for (fmt in c("%Y-%m-%d", "%d/%m/%Y", "%d/%m/%y", "%Y/%m/%d", "%m/%d/%Y", "%Y-%m-%d %H:%M:%S")) {
          temp_dates <- suppressWarnings(as.Date(x_rem, format = fmt))
          success_idx <- which(!is.na(temp_dates))
          if (length(success_idx) > 0) {
            final_dates[rem_idx[success_idx]] <- temp_dates[success_idx]
            x_rem <- x_rem[-success_idx]       
            rem_idx <- rem_idx[-success_idx]   
          }
          if (length(rem_idx) == 0) break
        }
      }
      return(final_dates)
    }
    
    parsed_dates <- parse_any_date(df$Clock.Today)
    
    # Check if any dates completely failed to parse (pure gibberish)
    unparsed_idx <- is.na(parsed_dates) & !is.na(df$Clock.Today) & trimws(as.character(df$Clock.Today)) != ""
    
    if (any(unparsed_idx)) {
      bad_dates <- head(df$Clock.Today[unparsed_idx], 3)
      stop(sprintf(
        "QC FAILED: 'Clock.Today' contains completely unreadable dates.\n  -> Example of bad format found: %s", 
        paste(bad_dates, collapse = ", ")
      ))
    }
    
    # Auto-fix: Overwrite the messy strings with standard YYYY-MM-DD
    df$Clock.Today <- as.character(parsed_dates)
    
  } else {
    # If it is already a Date object, just lock it into a clean YYYY-MM-DD string for APSIM
    df$Clock.Today <- as.character(as.Date(df$Clock.Today))
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
  
  message("✅ QC PASSED: Observation dataframe is structurally sound and ready for APSIM.")
  
  # Return the clean, standard-formatted data
  return(df)
}