#' Find Date Reaching a Target Stage Percentage (Universal Master)
#'
#' @description
#' Scans a list of observation dataframes to find the exact date when a specified 
#' percentage of the maximum phenological progress (e.g., 'DateToProgress') 
#' is first reached for each SimulationName.
#'
#' @details
#' The function isolates the target variable dynamically by searching for 
#' "DateToProgress" in the column names. It handles ties by selecting the 
#' first chronological date the threshold was met or exceeded.
#'
#' @param df_list_PCDS A named list of data frames. Each data frame must contain 
#'   `SimulationName`, `Date`, and exactly one column with 
#'   "DateToProgress" in its name.
#' @param StageTargetPerc Numeric. The target percentage (0-100) of the maximum 
#'   progress value to reach.
#'
#' @return A single bound data frame containing `SimulationName`, 
#'   `StageName`, `TargetPerc`, the calculated maximum and target values, 
#'   and the `DateReached`.
#'
#' @importFrom dplyr arrange group_by mutate filter slice_min ungroup transmute bind_rows
#' @importFrom rlang .data
#' @export
findDateStageTarget <- function(df_list_PCDS, StageTargetPerc) {
  
  stopifnot(is.list(df_list_PCDS))
  stopifnot(length(StageTargetPerc) == 1)
  
  require(dplyr)
  require(rlang)
  
  results <- vector("list", length(df_list_PCDS))
  nm_list <- names(df_list_PCDS)
  
  for (i in seq_along(df_list_PCDS)) {
    
    nm <- nm_list[[i]]
    df <- df_list_PCDS[[i]]
    
    # ------------------------------------------------------------------
    # 1. DEFENSIVE CHECKS
    # ------------------------------------------------------------------
    if (!all(c("SimulationName", "Date") %in% names(df))) {
      stop(sprintf("CRITICAL: Dataframe '%s' is missing 'SimulationName' or 'Date'.", nm))
    }
    
    # identify the progress variable
    value_col <- names(df)[grepl("DateToProgress", names(df))]
    
    if (length(value_col) != 1) {
      stop(sprintf("Expected exactly one 'DateToProgress' column in '%s', but found %d.", nm, length(value_col)))
    }
    
    # extract stage name (clean but robust)
    StageName <- value_col[[1]]
    
    # ------------------------------------------------------------------
    # 2. CALCULATE AND EXTRACT DATES
    # ------------------------------------------------------------------
    # find first date reaching target
    res <- df %>%
      dplyr::arrange(SimulationName, Date) %>%
      dplyr::group_by(SimulationName) %>%
      dplyr::mutate(
        max_value    = max(.data[[value_col]], na.rm = TRUE),
        target_value = max_value * StageTargetPerc / 100
      ) %>%
      dplyr::filter(.data[[value_col]] >= target_value) %>%
      dplyr::slice_min(Date, n = 1, with_ties = FALSE) %>%
      dplyr::ungroup() %>%
      dplyr::transmute(
        SimulationName  = SimulationName,
        StageName       = StageName,
        TargetPerc      = StageTargetPerc,
        Maxvalue        = max_value,
        TargetValue     = target_value,
        DateReached     = Date
      )
    
    results[[i]] <- res
  }
  
  # Bind the list of dataframes into a single clean output
  return(dplyr::bind_rows(results))
}