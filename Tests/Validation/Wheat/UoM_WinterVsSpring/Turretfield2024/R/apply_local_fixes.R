#' Apply Project-Specific Data Fixes (Turretfield)
#' 
#' Intercepts the raw compiled observations, patches missing dates using metadata,
#' and enforces strict year-matching against a reference date to catch Excel typos.
#'
#' @param compiled_obs The nested tibble from compile_all_observed().
#' @param df_obs_info The metadata dataframe containing 'SampleDateApprox'.
#' @param ref_date Character. A reference date (e.g., "15/05/2024") used to define 
#'   the definitive project year. Any dates with mismatched years are corrected.
#' @return The corrected nested tibble
#' @export
apply_local_fixes <- function(compiled_obs, df_obs_info, ref_date) {
  
  require(dplyr)
  require(purrr)
  require(lubridate)
  
  # ====================================================================
  # 0. THE FLEXIBLE DATE PARSER (Elevated for universal use)
  # ====================================================================
  parse_flexible_date <- function(date_string) {
    x <- trimws(as.character(date_string))
    nums <- suppressWarnings(as.numeric(x))
    if (!is.na(nums)) return(as.Date(nums, origin = "1899-12-30"))
    
    formats_to_try <- c("%d/%m/%Y", "%Y-%m-%d", "%d-%m-%Y", "%Y/%m/%d", "%d/%m/%y", "%m/%d/%Y")
    for (fmt in formats_to_try) {
      d <- suppressWarnings(as.Date(x, format = fmt))
      if (!is.na(d)) return(d)
    }
    return(as.Date(NA))
  }
  
  # ====================================================================
  # 1. DEFENSIVE CHECKS & REFERENCE YEAR EXTRACTION
  # ====================================================================
  if (!"SampleDateApprox" %in% names(df_obs_info)) {
    warning("No 'SampleDateApprox' column found in metadata. Missing dates will not be imputed.", call. = FALSE)
  }
  
  # Parse the reference date to extract the "Target Year"
  parsed_ref_date <- parse_flexible_date(ref_date)
  if (is.na(parsed_ref_date)) {
    stop(sprintf("CRITICAL: Could not parse ref_date '%s'. Please provide a valid date.", ref_date))
  }
  target_year <- lubridate::year(parsed_ref_date)
  
  # Setup trackers for our audit logs
  patched_missing_vars <- c()
  year_correction_logs <- c()
  
  # ====================================================================
  # 2. JOIN METADATA & PROCESS
  # ====================================================================
  final_obs <- compiled_obs %>%
    dplyr::left_join(
      df_obs_info %>% dplyr::select(df_name, column_name, dplyr::any_of("SampleDateApprox")), 
      by = "df_name"
    ) %>%
    dplyr::mutate(
      data = purrr::pmap(
        list(data, column_name, if("SampleDateApprox" %in% names(.)) SampleDateApprox else NA), 
        function(df, var_name, approx_date_raw) {
          
          if (is.null(df) || nrow(df) == 0 || !"Date" %in% names(df)) return(df)
          
          # -------------------------------------------------------------
          # ACTION A: Patch Missing Dates (NAs)
          # -------------------------------------------------------------
          if (any(is.na(df$Date)) && !is.na(approx_date_raw) && trimws(as.character(approx_date_raw)) != "") {
            approx_date <- parse_flexible_date(approx_date_raw)
            if (!is.na(approx_date)) {
              df <- df %>% dplyr::mutate(Date = dplyr::if_else(is.na(Date), approx_date, Date))
              patched_missing_vars <<- c(patched_missing_vars, var_name)
            } else {
              warning(sprintf("Could not parse SampleDateApprox '%s' for '%s'.", approx_date_raw, var_name), call. = FALSE)
            }
          }
          
          # -------------------------------------------------------------
          # ACTION B: Correct Bad Years
          # -------------------------------------------------------------
          # Find any dates where the year does not match our target_year
          bad_year_idx <- which(!is.na(df$Date) & lubridate::year(df$Date) != target_year)
          
          if (length(bad_year_idx) > 0) {
            # Capture the old dates for logging before we overwrite them
            old_dates_str <- format(df$Date[bad_year_idx], "%d/%m/%Y")
            
            # Force the year to match the reference year
            lubridate::year(df$Date[bad_year_idx]) <- target_year
            
            # Capture the newly corrected dates
            new_dates_str <- format(df$Date[bad_year_idx], "%d/%m/%Y")
            
            # Create distinct log messages so we don't spam the console
            unique_changes <- unique(paste("incorrect date", old_dates_str, "was corrected to", new_dates_str))
            for (change in unique_changes) {
              year_correction_logs <<- c(year_correction_logs, sprintf("Variable '%s' has an %s", var_name, change))
            }
          }
          
          return(df)
        }
      )
    ) %>%
    dplyr::select(df_name, data)
  
  # ====================================================================
  # 3. THE CONSOLIDATED AUDIT WARNINGS
  # ====================================================================
  if (length(patched_missing_vars) > 0) {
    message(paste(c(
      "",
      "======================================================================",
      " \u26A0\uFE0F APPROXIMATE SAMPLE DATES APPLIED \u26A0\uFE0F",
      "======================================================================",
      " Missing dates were patched using the 'SampleDateApprox' column.",
      " Variables Affected:",
      paste("   ->", unique(patched_missing_vars)),
      "======================================================================",
      ""
    ), collapse = "\n"))
  }
  
  if (length(year_correction_logs) > 0) {
    message(paste(c(
      "",
      "======================================================================",
      sprintf(" \u26A0\uFE0F YEAR MISMATCHES CORRECTED (Target Year: %s) \u26A0\uFE0F", target_year),
      "======================================================================",
      " Dates with mismatched years were found and forced to the target year:",
      paste("   ->", year_correction_logs),
      "======================================================================",
      ""
    ), collapse = "\n"))
  }
  
  return(final_obs)
}