#' Append Harvest Stage Row to Observed Data
#'
#' @description
#' Creates a terminal "Harvest" observation row for each unique simulation. 
#' It dynamically identifies the latest `Clock.Today` date for each simulation, 
#' creates a new row with that exact date, and injects a specified target 
#' value into a newly created column (e.g., setting `CurrentStageName` to `HarvestRipe`).
#'
#' @details
#' **Type Safety:** This function natively inherits the `<date>` class of the 
#' `Clock.Today` column to calculate the chronological maximum. It relies on 
#' native R date sorting and binding, avoiding type-mismatch errors during `bind_rows`.
#' APSIM string formatting should be applied downstream of this function.
#'
#' @param df A data.frame containing the wide format observation data. 
#'   Must contain `SimulationName` and `Clock.Today` columns (as Date or POSIXct).
#' @param col_name Character. The exact name of the new column to inject 
#'   (e.g., "Wheat.Phenology.CurrentStageName").
#' @param col_value Character. The string value to insert into the new column 
#'   for the terminal harvest rows (e.g., "HarvestRipe").
#'
#' @return A data.frame with the new terminal rows appended and sorted chronologically.
#'
#' @importFrom dplyr group_by summarise mutate bind_rows arrange
#' @importFrom rlang `:=` sym
#' @export
add_harv_into_obs <- function(df, col_name, col_value) {
  
  require(dplyr)
  require(rlang)
  
  # 1. Safety Checks
  if (!all(c("SimulationName", "Clock.Today") %in% names(df))) {
    stop("Dataframe must contain 'SimulationName' and 'Clock.Today' columns.")
  }
  
  # 2. Extract the terminal (max) date for each simulation
  # Because the input is a <date>, max() works natively and retains the type!
  df_terminal_rows <- df %>%
    dplyr::group_by(SimulationName) %>%
    dplyr::summarise(
      Clock.Today = max(Clock.Today, na.rm = TRUE),
      .groups = "drop"
    ) %>%
    dplyr::mutate(
      !!rlang::sym(col_name) := col_value
    )
  
  # 3. Stitch the new terminal rows onto the bottom of the original data
  df_final <- dplyr::bind_rows(df, df_terminal_rows)
  
  # 4. Sort the dataframe natively using the Date objects
  df_final <- df_final %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  return(df_final)
}