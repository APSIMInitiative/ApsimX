#' Flag Final Harvest Dates in Observation Data
#'
#' @description
#' Identifies the final observation date for each simulation based on the presence 
#' of key reference variables (e.g., Grain Wt, AboveGround Wt) and flags that 
#' specific row with a target string (e.g., "HarvestRipe").
#'
#' @details
#' **Strict Type Preservation:** Safely calculates the maximum date without stripping 
#' the POSIXct/Date class attributes, ensuring compatibility with downstream APSIM QC gates.
#'
#' @param df Data frame. The continuous observation timeline.
#' @param ref_vars Character vector. Columns to check for final physical measurements.
#' @param new_col_name Character. The column where the flag should be written.
#' @param new_col_value Character. The string flag to insert (e.g., "HarvestRipe").
#'
#' @return A data frame with the harvest flag appended to the correct dates.
#' @export
add_harv_into_obs <- function(df, ref_vars, new_col_name, new_col_value) {
  
  # ---- 1. DEFENSIVE CHECKS & TYPE LOCKING ----
  if (!"SimulationName" %in% names(df) || !"Clock.Today" %in% names(df)) {
    stop("Error [add_harv_into_obs]: Missing 'SimulationName' or 'Clock.Today'.")
  }
  
  # Force Date class to prevent numeric coercion
  df_clean <- df %>%
    dplyr::mutate(Clock.Today = as.Date(Clock.Today))
  
  # Ensure the target column exists and is a character vector
  if (!new_col_name %in% names(df_clean)) {
    df_clean[[new_col_name]] <- NA_character_
  } else {
    df_clean[[new_col_name]] <- as.character(df_clean[[new_col_name]])
  }
  
  # ---- 2. ISOLATE THE FINAL MEASUREMENT DATES ----
  # Find the maximum date per simulation where ANY of the ref_vars actually have data
  harv_dates <- df_clean %>%
    dplyr::select(SimulationName, Clock.Today, dplyr::any_of(ref_vars)) %>%
    # RowSums checks if there is at least one non-NA value in the reference columns
    dplyr::mutate(has_data = rowSums(!is.na(dplyr::select(., dplyr::any_of(ref_vars)))) > 0) %>%
    dplyr::filter(has_data == TRUE) %>%
    dplyr::group_by(SimulationName) %>%
    # Keep the max date and explicitly tell R it is a Date
    dplyr::summarise(HarvestDate = as.Date(max(Clock.Today, na.rm = TRUE)), .groups = "drop")
  
  # ---- 3. MERGE AND FLAG ----
  df_final <- df_clean %>%
    dplyr::left_join(harv_dates, by = "SimulationName") %>%
    dplyr::mutate(
      # If the row's date matches the simulation's max data date, flag it!
      !!new_col_name := dplyr::if_else(
        Clock.Today == HarvestDate & !is.na(HarvestDate),
        new_col_value,
        .data[[new_col_name]]
      )
    ) %>%
    # Clean up the temporary calculation column
    dplyr::select(-HarvestDate) %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  message(sprintf("Success [add_harv_into_obs]: Inserted '%s' flag into column '%s' for %d simulations.", 
                  new_col_value, new_col_name, nrow(harv_dates)))
  
  return(df_final)
}