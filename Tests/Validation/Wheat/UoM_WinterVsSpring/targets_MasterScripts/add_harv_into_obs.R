#' Flag Final Harvest Dates in Observation Data (Asynchronous Logic)
#'
#' @description
#' Finds the most recent measurement date for EACH reference variable independently. 
#' It then flags ANY row where the date matches one of these final measurement dates 
#' across the entire dataset.
#'
#' @export
add_harv_into_obs <- function(df, ref_vars, new_col_name, new_col_value) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("tidyr", quietly = TRUE)) stop("Package 'tidyr' required.")
  
  # ---- 1. DEFENSIVE CHECKS & TYPE LOCKING ----
  if (!"SimulationName" %in% names(df) || !"Clock.Today" %in% names(df)) {
    stop("CRITICAL [add_harv_into_obs]: Missing 'SimulationName' or 'Clock.Today'.")
  }
  
  # Temporarily cast to Date for max() math
  df_clean <- df %>% dplyr::mutate(Clock.Today = as.Date(Clock.Today))
  
  if (!new_col_name %in% names(df_clean)) {
    df_clean[[new_col_name]] <- NA_character_
  } else {
    df_clean[[new_col_name]] <- as.character(df_clean[[new_col_name]])
  }
  
  # THE SHIELD: Only search for variables that actually exist in the dataframe
  actual_ref_vars <- base::intersect(ref_vars, names(df_clean))
  
  if (length(actual_ref_vars) == 0) {
    warning("None of the requested reference variables exist in the data. Returning original df.")
    return(df_clean %>% dplyr::mutate(Clock.Today = as.character(Clock.Today)))
  }
  
  # ---- 2. INDEPENDENT MAX DATE SEARCH (Bug Patched) ----
  harv_dates <- df_clean %>%
    dplyr::select(SimulationName, Clock.Today, dplyr::any_of(actual_ref_vars)) %>%
    tidyr::pivot_longer(cols = dplyr::any_of(actual_ref_vars), names_to = "Variable", values_to = "Value") %>%
    dplyr::filter(!is.na(Value)) %>%
    dplyr::group_by(SimulationName, Variable) %>%
    dplyr::summarise(MaxDate = as.Date(max(Clock.Today, na.rm = TRUE)), .groups = "drop")
  
  if (nrow(harv_dates) == 0) {
    warning("No data found for any of the provided reference variables. Returning original data.")
    return(df_clean %>% dplyr::mutate(Clock.Today = as.character(Clock.Today)))
  }
  
  unique_harv_dates <- harv_dates %>%
    dplyr::distinct(SimulationName, MaxDate) %>%
    dplyr::mutate(IsHarvestFlag = TRUE)
  
  # ---- 3. MERGE AND FLAG ----
  df_final <- df_clean %>%
    dplyr::left_join(unique_harv_dates, by = c("SimulationName" = "SimulationName", "Clock.Today" = "MaxDate")) %>%
    dplyr::mutate(
      !!new_col_name := dplyr::if_else(
        IsHarvestFlag == TRUE & !is.na(IsHarvestFlag),
        new_col_value,
        .data[[new_col_name]]
      )
    ) %>%
    dplyr::select(-IsHarvestFlag) %>%
    dplyr::arrange(SimulationName, Clock.Today) %>%
    # APSIM SAFETY LOCK: Convert the date back to a clean string before export
    dplyr::mutate(Clock.Today = as.character(Clock.Today))
  
  # ---- 4. THE DIAGNOSTIC ALARM ----
  min_date <- min(harv_dates$MaxDate, na.rm = TRUE)
  max_date <- max(harv_dates$MaxDate, na.rm = TRUE)
  spread_days <- as.numeric(difftime(max_date, min_date, units = "days"))
  
  message("\n", strrep("=", 60))
  message(sprintf(" \u26A0\uFE0F  HARVEST DATES ASSIGNED: %s \u26A0\uFE0F ", toupper(new_col_value)))
  message(strrep("=", 60))
  message(" -> STRATEGY     : Asynchronous Max Date Search")
  message(sprintf(" -> OVERALL SPAN : %s to %s", min_date, max_date))
  
  if (spread_days > 0) {
    message(sprintf(" -> WARNING      : Max difference between harvest variables is %d days.", spread_days))
    message(" -> BREAKDOWN    : Latest recorded date per variable across the trial:")
    
    var_breakdown <- harv_dates %>%
      dplyr::group_by(Variable) %>%
      dplyr::summarise(OverallMax = max(MaxDate, na.rm = TRUE))
    
    for (i in 1:nrow(var_breakdown)) {
      message(sprintf("      - %-25s : %s", var_breakdown$Variable[i], var_breakdown$OverallMax[i]))
    }
  } else {
    message(" -> STATUS       : All reference variables share the exact same harvest date.")
  }
  message(strrep("-", 60), "\n")
  
  return(df_final)
}