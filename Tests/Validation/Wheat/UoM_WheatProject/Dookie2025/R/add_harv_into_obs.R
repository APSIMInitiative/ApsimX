#' Append Harvest Stage to Observed Data
#'
#' @description
#' Automatically appends a final observation row for each unique simulation, 
#' assigning a specified categorical value (e.g., "HarvestRipe") to a new 
#' column on the latest recorded date for that simulation.
#'
#' @details
#' The function parses the APSIM-formatted `Clock.Today` column, determines 
#' the chronological maximum date for each `SimulationName`, and generates a 
#' new row with the specified column name and value. `dplyr::bind_rows` seamlessly 
#' integrates this, automatically populating `NA` for all historic rows in the new column, 
#' and `NA` for all other variables in the new row.
#'
#' @param df A data.frame containing the observed data. Must include 
#'   `SimulationName` and `Clock.Today`.
#' @param col_name Character. The name of the new column to create (e.g., "Wheat.Phenology.CurrentStageName").
#' @param col_value Character. The value to insert into the new column (e.g., "HarvestRipe").
#'
#' @return A data.frame with the original data plus one newly appended row per 
#'   simulation, chronologically sorted.
#'
#' @importFrom dplyr group_by summarise mutate select bind_rows arrange
#' @importFrom lubridate dmy_hms
#' @importFrom rlang `:=` `!!`
#' @export
add_harv_into_obs <- function(df, col_name, col_value) {
  
  require(dplyr)
  require(lubridate)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. STRICT SAFETY CHECKS
  # ------------------------------------------------------------------
  stopifnot(
    is.data.frame(df),
    "SimulationName" %in% names(df),
    "Clock.Today" %in% names(df),
    is.character(col_name),
    is.character(col_value),
    length(col_name) == 1,
    length(col_value) == 1
  )
  
  # ------------------------------------------------------------------
  # 2. ISOLATE LATEST DATES & CREATE NEW ROWS
  # ------------------------------------------------------------------
  df_new_rows <- df %>%
    dplyr::group_by(SimulationName) %>%
    dplyr::summarise(
      # Parse the APSIM string format to POSIXct to find the true chronological max
      max_date_val = max(lubridate::dmy_hms(Clock.Today), na.rm = TRUE),
      .groups = "drop"
    ) %>%
    dplyr::mutate(
      # Format it back to the strict APSIM string requirement
      Clock.Today = format(max_date_val, "%d/%m/%Y 00:00:00"),
      # Inject the new column and value dynamically
      !!col_name := col_value
    ) %>%
    dplyr::select(-max_date_val)
  
  # ------------------------------------------------------------------
  # 3. BIND AND SORT
  # ------------------------------------------------------------------
  # bind_rows automatically adds the new column to the original rows with NA
  df_final <- dplyr::bind_rows(df, df_new_rows)
  
  # Sort chronologically so the injected row sits at the end of each simulation
  df_final <- df_final %>%
    dplyr::mutate(temp_sort_date = lubridate::dmy_hms(Clock.Today)) %>%
    dplyr::arrange(SimulationName, temp_sort_date) %>%
    dplyr::select(-temp_sort_date)
  
  # ------------------------------------------------------------------
  # 4. CONSOLE NOTIFICATION
  # ------------------------------------------------------------------
  message(sprintf("Successfully appended %d '%s' rows to new column: '%s'", 
                  nrow(df_new_rows), col_value, col_name))
  
  return(df_final)
}