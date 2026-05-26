#' Calculate and Append Harvest Index (Universal Asynchronous Engine)
#'
#' @description
#' Safely calculates the Harvest Index (HI) from Grain Weight and Total Above-Ground 
#' Biomass (AGB), appending it as a new column. It handles asynchronous lab data by 
#' finding the maximum values for Grain and AGB across the entire simulation timeline.
#'
#' @export
calc_harvest_index <- function(df, grain_col = "Wheat.Grain.Wt", agb_col = "Wheat.AboveGround.Wt", hi_col_name = "HarvestIndex") {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df) || !is.data.frame(df) || nrow(df) == 0) {
    stop("CRITICAL [calc_harvest_index]: Main observation dataframe is missing or empty.")
  }
  
  if (!all(c(grain_col, agb_col) %in% names(df))) {
    warning(sprintf(
      "Warning [calc_harvest_index]: Required biological columns '%s' or '%s' not found. Returning df unmodified.", 
      grain_col, agb_col
    ))
    return(df)
  }
  
  # ---- 2. ASYNCHRONOUS CALCULATION ENGINE ----
  df_out <- df %>%
    dplyr::group_by(SimulationName) %>%
    dplyr::mutate(
      temp_max_grain = suppressWarnings(max(.data[[grain_col]], na.rm = TRUE)),
      temp_max_agb   = suppressWarnings(max(.data[[agb_col]], na.rm = TRUE))
    ) %>%
    dplyr::ungroup() %>%
    dplyr::mutate(
      temp_max_grain = ifelse(is.infinite(temp_max_grain), NA, temp_max_grain),
      temp_max_agb   = ifelse(is.infinite(temp_max_agb), NA, temp_max_agb),
      
      !!hi_col_name := dplyr::if_else(
        !is.na(.data[[grain_col]]) & !is.na(temp_max_agb) & temp_max_agb > 0,
        temp_max_grain / temp_max_agb,
        NA_real_
      )
    ) %>%
    dplyr::select(-temp_max_grain, -temp_max_agb)
  
  # ---- 3. THE DIAGNOSTIC ALARM & QC CHECK ----
  # Isolate only the valid numbers to calculate the range safely
  valid_hi <- df_out[[hi_col_name]][!is.na(df_out[[hi_col_name]])]
  
  message("\n", strrep("=", 60))
  message(" \u26A0\uFE0F  CALCULATION COMPLETE: HARVEST INDEX \u26A0\uFE0F ")
  message(strrep("=", 60))
  
  if (length(valid_hi) > 0) {
    min_hi <- round(min(valid_hi), 3)
    max_hi <- round(max(valid_hi), 3)
    
    message(sprintf(" -> SUCCESS      : %d valid HI values generated.", length(valid_hi)))
    message(sprintf(" -> VALUE RANGE  : %.3f to %.3f", min_hi, max_hi))
    
    # Biological Plausibility Warning
    if (max_hi > 1.0) {
      message(" -> QC ALARM     : CRITICAL - HI values > 1.0 detected!")
      message(sprintf("                  Check '%s' and '%s' for lab data entry errors.", grain_col, agb_col))
    } else {
      message(" -> QC STATUS    : PASS (All HI values <= 1.0)")
    }
  } else {
    message(" -> STATUS       : No valid Harvest Index values could be calculated.")
    message(sprintf("                  Check if '%s' or '%s' are entirely NA.", grain_col, agb_col))
  }
  message(strrep("-", 60), "\n")
  
  return(df_out)
}