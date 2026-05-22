#' Extract Phenology Dates from DOY Wide Observations (Step 1 - Grass Engine)
#'
#' @description
#' A specialized Step 1 pipeline component that extracts Day-of-Year (DOY) phenology entries 
#' from a wide-format data frame. It applies an absolute reference calendar date offset to calculate 
#' true calendar dates, melts the dataset vertical, and maps the events to standard numeric codes.
#'
#' @details
#' **Interface Standard Compliance:** This function enforces strict output typing and column selection 
#' to match your universal intermediate schema: \code{(SimulationName, Clock.Today, Wheat.Phenology.Stage)}. 
#' This ensures its output can be passed directly into Step 2 (Linear Interpolation) or Step 4 (Master Merge).
#'
#' @param df Data frame. The wide consolidated raw observation data frame (\code{df_obs_mean}).
#' @param dateDOY Character string. The explicit baseline calendar year reference marker 
#'   in day-month-year formatting (e.g., \code{"31-12-2023"} or \code{"01-01-2024"}).
#'
#' @return A validated tidy data frame matching the intermediate interface standard.
#' @export
get_pheno_dates_from_DOY_wide_obs <- function(df, dateDOY) {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df) || is.null(df) || nrow(df) == 0) {
    stop("Error [get_pheno_dates_from_DOY_wide_obs]: Input data frame asset 'df' is missing or empty.")
  }
  if (is.null(dateDOY) || dateDOY == "") {
    stop("Error [get_pheno_dates_from_DOY_wide_obs]: Absolute reference base string 'dateDOY' must be specified.")
  }
  if (!"SimulationName" %in% names(df)) {
    stop("Error [get_pheno_dates_from_DOY_wide_obs]: Source table lacks mandatory 'SimulationName' anchor index.")
  }
  
  # Isolate and verify that your system contains DOY tracking vectors
  doy_cols <- names(df)[grepl("Wheat\\.Phenology.*DOY", names(df), ignore.case = TRUE)]
  if (length(doy_cols) == 0) {
    stop("Error [get_pheno_dates_from_DOY_wide_obs]: Failed to locate any variables matching pattern 'Wheat.Phenology.*DOY'.")
  }
  
  # ---- 2. REFERENCE CALENDAR ALIGNMENT ----
  # Transform input string securely into a true R Date coordinate
  ref_date <- suppressWarnings(lubridate::dmy(dateDOY))
  if (is.na(ref_date)) {
    # Try a secondary common fallback layout just in case configurations shift
    ref_date <- suppressWarnings(lubridate::ymd(dateDOY))
    if (is.na(ref_date)) {
      stop(sprintf("Error [get_pheno_dates_from_DOY_wide_obs]: Unable to parse reference string '%s' into a valid Date.", dateDOY))
    }
  }
  
  # ---- 3. CALCULATE CALENDAR DATES & MELT VERTICAL ----
  df_long <- df %>%
    dplyr::select(SimulationName, dplyr::all_of(doy_cols)) %>%
    # Use across to execute element-wise date arithmetic safely
    dplyr::mutate(dplyr::across(
      .cols = dplyr::all_of(doy_cols), 
      .fns = ~ {
        val_numeric <- as.integer(.x)
        # Add day intervals minus 1 because DOY 1 is the baseline reference date itself
        if_else(is.na(val_numeric) | val_numeric <= 0, as.Date(NA), ref_date + lubridate::days(val_numeric - 1))
      },
      .names = "{.col}.Date"
    )) %>%
    dplyr::select(SimulationName, dplyr::ends_with(".Date")) %>%
    
    # Pivot down to standard narrow format
    tidyr::pivot_longer(
      cols = dplyr::ends_with(".Date"),
      names_to = "PhenoEvent",
      values_to = "Clock.Today" # FIXED: Replaced PhenoDate with Clock.Today to conform to the blueprint
    ) %>%
    # Exclude entries that completely lack data
    dplyr::filter(!is.na(Clock.Today))
  
  # ---- 4. CLEAN TEXT METADATA & MAP CODES ----
  df_final <- df_long %>%
    dplyr::mutate(
      # Strip out namespaces to isolate the core milestone string
      PhenoEvent = stringr::str_remove_all(PhenoEvent, "Wheat|Phenology|Estimated|DOY|Date|\\."),
      
      # Execute strict type-safe double-precision numeric stage assignments
      Wheat.Phenology.Stage = dplyr::case_when(
        grepl("Emergence", PhenoEvent, ignore.case = TRUE) ~ 3,
        grepl("PCDS6",     PhenoEvent, ignore.case = TRUE) ~ 6,
        grepl("PCDS8",     PhenoEvent, ignore.case = TRUE) ~ 8,
        grepl("PCDS10",    PhenoEvent, ignore.case = TRUE) ~ 10,
        TRUE                                               ~ NA_real_
      )
    ) %>%
    # Strip records that don't match your active physiological components
    dplyr::filter(!is.na(Wheat.Phenology.Stage)) %>%
    
    # ---- 5. INTERFACE COMPLIANCE CHECK ----
  dplyr::select(SimulationName, Clock.Today, Wheat.Phenology.Stage) %>%
    dplyr::distinct() %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  # ---- 6. PIPELINE NOTIFICATION LOG ----
  message(sprintf("Success [get_pheno_dates_from_DOY_wide_obs]: Extracted %d standardized raw records from wide DOY entries.", 
                  nrow(df_final)))
  
  return(df_final)
}