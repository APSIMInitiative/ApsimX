#' Inject Harvest Stage into Existing Observations
#'
#' @description
#' Identifies the final measurement date of a specified reference variable 
#' (e.g., grain weight) for each simulation, and injects a new stage value 
#' (e.g., "HarvestRipe") into a target column on that exact existing row.
#'
#' @details
#' Unlike previous versions, this function does **not** append new rows using `bind_rows`. 
#' It safely mutates the existing data frame in place. If the target column does not 
#' exist, it initializes it. If it does exist, it seamlessly updates only the targeted rows 
#' while preserving any existing data in that column.
#'
#' @param df Data frame containing the final formatted observations.
#' @param ref_var Character. The column name used to find the last valid measurement 
#'   (e.g., "Wheat.Grain.Wt").
#' @param new_col_name Character. The name of the column to hold the stage name 
#'   (e.g., "Wheat.Phenology.CurrentStageName").
#' @param new_col_value Character. The stage name to assign (e.g., "HarvestRipe").
#'
#' @return A data frame with the updated rows.
#'
#' @importFrom dplyr filter mutate group_by slice_max ungroup select left_join if_else
#' @importFrom lubridate parse_date_time
#' @importFrom rlang sym `:=` .data
#' @export
add_harv_into_obs <- function(df, ref_var, new_col_name, new_col_value) {
  
  require(dplyr)
  require(lubridate)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!"SimulationName" %in% names(df) || !"Clock.Today" %in% names(df)) {
    stop("CRITICAL: 'df' must contain 'SimulationName' and 'Clock.Today' columns.")
  }
  
  if (!ref_var %in% names(df)) {
    stop(sprintf("CRITICAL: Reference variable '%s' not found in dataframe.", ref_var))
  }
  
  # ------------------------------------------------------------------
  # 2. IDENTIFY TARGET HARVEST DATES
  # ------------------------------------------------------------------
  # Build a lookup table of exactly which Simulation + Date combos need the tag
  harvest_lookup <- df %>%
    # Rule 1: Only consider rows where the reference variable actually has data
    dplyr::filter(!is.na(!!rlang::sym(ref_var))) %>%
    
    # Rule 2: Ensure dates are valid
    dplyr::filter(!is.na(Clock.Today), Clock.Today != "") %>%
    dplyr::mutate(
      .temp_date = suppressWarnings(
        lubridate::parse_date_time(Clock.Today, orders = c("dmy HMS", "ymd HMS", "dmy", "ymd"))
      )
    ) %>%
    
    dplyr::group_by(SimulationName) %>%
    # Rule 3: Find the last chronological date for the reference variable
    dplyr::slice_max(.temp_date, n = 1, with_ties = FALSE) %>%
    dplyr::ungroup() %>%
    
    # Keep only the keys needed for the join, and create a targeting flag
    dplyr::select(SimulationName, Clock.Today) %>%
    dplyr::mutate(.is_harvest_target = TRUE)
  
  # ------------------------------------------------------------------
  # 3. INITIALIZE TARGET COLUMN (IF NEEDED)
  # ------------------------------------------------------------------
  if (!new_col_name %in% names(df)) {
    df <- df %>% dplyr::mutate(!!new_col_name := NA_character_)
  }
  
  # ------------------------------------------------------------------
  # 4. INJECT VALUES INTO EXISTING ROWS
  # ------------------------------------------------------------------
  df_final <- df %>%
    # Join the targeting flag to the main dataframe
    dplyr::left_join(harvest_lookup, by = c("SimulationName", "Clock.Today")) %>%
    
    # Inject the value only where the flag is TRUE, otherwise keep existing data
    dplyr::mutate(
      !!new_col_name := dplyr::if_else(
        !is.na(.is_harvest_target), 
        new_col_value, 
        .data[[new_col_name]]
      )
    ) %>%
    
    # Clean up the temporary flag
    dplyr::select(-.is_harvest_target)
  
  message(sprintf("Successfully injected '%s' into '%s' across %d simulations based on the final '%s' measurement.", 
                  new_col_value, new_col_name, nrow(harvest_lookup), ref_var))
  
  return(df_final)
}