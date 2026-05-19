#' Append New Observation Dataframe to Nested Tibble
#'
#' @description
#' Safely appends a newly generated dataframe (such as synthetic phenology stages) 
#' as a new nested element within the compiled observations tibble. 
#'
#' @details
#' **Structural Integrity:** Before appending, the function explicitly checks that 
#' the incoming dataframe (`df_new`) contains `SimulationName`. This enforces 
#' the pipeline's strict reliance on `SimulationName` as the primary identifier 
#' rather than just `Cultivar`. It also prevents silent overwrites if the 
#' `new_name` already exists.
#'
#' @param list_observed_clean A nested tibble containing `df_name` and `data` columns.
#' @param df_new The new data frame to append. Must contain a `SimulationName` column.
#' @param new_name Character. The exact name to assign to this new dataset in the `df_name` column.
#'
#' @return An updated nested tibble containing the newly appended row.
#'
#' @importFrom dplyr bind_rows
#' @importFrom tibble tibble
#' @export
add_to_observed_clean <- function(list_observed_clean, df_new, new_name) {
  
  require(dplyr)
  require(tibble)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  # Base structural safety checks (targets-friendly)
  stopifnot(
    is.data.frame(list_observed_clean),
    all(c("df_name", "data") %in% names(list_observed_clean)),
    is.data.frame(df_new),
    is.character(new_name),
    length(new_name) == 1
  )
  
  # Enforce SimulationName as the primary structural element
  if (!"SimulationName" %in% names(df_new)) {
    stop(sprintf("CRITICAL: The incoming dataframe '%s' must contain a 'SimulationName' column.", new_name))
  }
  
  # Prevent silent overwrites of existing dataframes
  if (new_name %in% list_observed_clean$df_name) {
    stop(sprintf("CRITICAL: A dataset named '%s' already exists in the observed list.", new_name))
  }
  
  # ------------------------------------------------------------------
  # 2. APPEND TO NESTED TIBBLE
  # ------------------------------------------------------------------
  updated_list <- dplyr::bind_rows(
    list_observed_clean,
    tibble::tibble(
      df_name = new_name,
      data    = list(df_new)
    )
  )
  
  # ------------------------------------------------------------------
  # 3. CONSOLE NOTIFICATION
  # ------------------------------------------------------------------
  message(sprintf("Successfully appended '%s' to the nested observed data list.", new_name))
  
  return(updated_list)
}