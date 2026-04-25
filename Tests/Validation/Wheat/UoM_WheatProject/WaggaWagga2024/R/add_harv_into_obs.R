#' Add Harvest Stage to Observations (Clock.Today Version)
#'
#' @description
#' Appends a final observation row for each simulation based on the latest 
#' recorded date for that specific simulation.
#'
#' @param df Data frame containing the final formatted observations.
#' @param col_name Character. The name of the new column to hold the stage name.
#' @param stg_name Character. The stage name to assign (e.g., "HarvestRipe").
#'
#' @return A data frame with the appended harvest rows.
#'
#' @importFrom dplyr filter mutate group_by slice_max ungroup transmute bind_rows
#' @importFrom lubridate parse_date_time
#' @importFrom rlang `:=`
#' @export
add_harv_into_obs <- function(df, col_name, stg_name) {
  
  require(dplyr)
  require(lubridate)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS (Now looking for Clock.Today)
  # ------------------------------------------------------------------
  if (!"SimulationName" %in% names(df) || !"Clock.Today" %in% names(df)) {
    stop("CRITICAL: 'df' must contain 'SimulationName' and 'Clock.Today' columns.")
  }
  
  # ------------------------------------------------------------------
  # 2. SAFELY EXTRACT FINAL DATES
  # ------------------------------------------------------------------
  harvest_rows <- df %>%
    # Filter out missing dates
    dplyr::filter(!is.na(Clock.Today), Clock.Today != "") %>%
    
    # Parse the APSIM string back to a temp date for safe sorting
    dplyr::mutate(
      .temp_date = suppressWarnings(
        lubridate::parse_date_time(Clock.Today, orders = c("dmy HMS", "ymd HMS", "dmy", "ymd"))
      )
    ) %>%
    
    dplyr::group_by(SimulationName) %>%
    # Safely extract the max date without triggering the -Inf bug
    dplyr::slice_max(.temp_date, n = 1, with_ties = FALSE) %>%
    dplyr::ungroup() %>%
    
    # Build the strict, sparse row containing only the essentials
    dplyr::transmute(
      SimulationName = SimulationName,
      Clock.Today = Clock.Today, 
      !!col_name := stg_name
    )
  
  # ------------------------------------------------------------------
  # 3. APPEND TO MASTER
  # ------------------------------------------------------------------
  df_final <- dplyr::bind_rows(df, harvest_rows)
  
  message(sprintf("Successfully appended %d '%s' rows to the Clock.Today timeline.", 
                  nrow(harvest_rows), stg_name))
  
  return(df_final)
}