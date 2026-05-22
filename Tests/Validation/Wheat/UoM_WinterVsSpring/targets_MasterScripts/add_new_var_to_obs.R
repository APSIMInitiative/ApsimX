#' Merge Any Time-Series Variable into Master Observations (Universal Engine)
#'
#' @description
#' Securely appends any finalized, long-format tracking dataset (e.g., phenology stages, 
#' thermal time, calculated stress indices) into the continuous field observations data frame. 
#'
#' @details
#' **Duplicate Date Collapsing:** APSIM-X will often crash or throw interpolation errors 
#' if the Observed data file contains two rows for the exact same `Clock.Today`. This 
#' function uses a smart aggregation pass after `bind_rows()` to squash same-day events 
#' together. If a discrete event (like a stage change) and a physical sampling cut occur 
#' on the exact same day, they are safely merged into a single row.
#'
#' @param df_obs Data frame. The main observations containing continuous field data.
#' @param df_new_data Data frame. The incoming 3-column dataset: \code{(SimulationName, Clock.Today, Value)}.
#' @param target_col_name Character. The name of the incoming variable column 
#'   to be integrated into the master sheet (e.g., \code{"Wheat.Phenology.Stage"}).
#'
#' @return A unified data frame sorted chronologically, with the new variable cleanly 
#'   integrated and single-day duplicates collapsed.
#' @export
add_new_var_to_obs <- function(df_obs, df_new_data, target_col_name) {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df_obs) || nrow(df_obs) == 0) {
    stop("Error [add_new_var_to_obs]: Main observation data frame 'df_obs' is missing or empty.")
  }
  if (missing(df_new_data) || nrow(df_new_data) == 0) {
    stop("Error [add_new_var_to_obs]: Incoming data frame 'df_new_data' is missing or empty.")
  }
  if (missing(target_col_name) || is.null(target_col_name) || target_col_name == "") {
    stop("Error [add_new_var_to_obs]: Must specify the 'target_col_name' string to map the new variable.")
  }
  
  req_cols <- c("SimulationName", "Clock.Today", target_col_name)
  missing_cols <- setdiff(req_cols, names(df_new_data))
  if (length(missing_cols) > 0) {
    stop(paste("Error [add_new_var_to_obs]: df_new_data does not match the standard 3-column schema. Missing:", 
               paste(missing_cols, collapse = ", ")))
  }
  
  # ---- 2. TYPE ALIGNMENT ----
  # Ensure strict typing on SimulationName and Dates before the bind
  df_obs_clean <- df_obs %>%
    dplyr::mutate(
      SimulationName = as.character(SimulationName),
      Clock.Today    = as.Date(Clock.Today)
    )
  
  df_new_clean <- df_new_data %>%
    dplyr::mutate(
      SimulationName = as.character(SimulationName),
      Clock.Today    = as.Date(Clock.Today)
    )
  
  # ---- 3. BIND & COLLAPSE (The APSIM-X Crash Prevention Layer) ----
  df_combined <- dplyr::bind_rows(df_obs_clean, df_new_clean) %>%
    dplyr::arrange(SimulationName, Clock.Today) %>%
    dplyr::group_by(SimulationName, Clock.Today) %>%
    # Squash multiple rows on the same day into a single row. 
    # Takes the first non-NA value for every column on that specific date.
    dplyr::summarise(
      dplyr::across(dplyr::everything(), ~ dplyr::first(stats::na.omit(.x))), 
      .groups = "drop"
    )
  
  # ---- 4. PIPELINE COMPLETION NOTIFICATION ----
  message(sprintf("Success [add_new_var_to_obs]: Appended and collapsed %d events into master observations under column '%s'.", 
                  nrow(df_new_clean), target_col_name))
  
  return(df_combined)
}