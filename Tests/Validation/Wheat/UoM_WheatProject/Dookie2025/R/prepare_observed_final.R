#' Prepare Final Observed Data for APSIM
#'
#' @description
#' Compiles a nested list of cleaned observation dataframes into a single, master 
#' dataframe ready for APSIM ingestion. It binds the rows, drops unnecessary 
#' metadata, and standardizes the date format to match APSIM requirements.
#'
#' @details
#' **Structural Validation:** Before binding, the function verifies that every nested 
#' dataframe contains the exact required structure: `SimulationName`, `Date`, and 
#' at least one dynamically named variable of interest.
#' 
#' **Date Formatting:** APSIM strictly requires dates in the `Clock.Today` format 
#' (e.g., "15/05/2024 00:00:00"). This function safely converts the standard R `Date` 
#' objects into this exact string structure while dropping the original `Date` column.
#'
#' @param list_observed_clean A tibble containing a `data` list-column, where each 
#'   inner dataframe represents cleaned observations for a specific variable.
#'
#' @return A single, flattened tibble containing `SimulationName`, `Clock.Today`, 
#'   and all dynamically combined variables of interest.
#'
#' @importFrom dplyr pull bind_rows mutate select everything
#' @importFrom purrr walk
#' @export
prepare_observed_final <- function(list_observed_clean) {
  
  require(dplyr)
  require(purrr)
  
  # ------------------------------------------------------------------
  # 1. STRICT SAFETY CHECKS
  # ------------------------------------------------------------------
  # Inspect every internal dataframe before attempting to bind them
  purrr::walk(list_observed_clean$data, function(df) {
    if (!all(c("SimulationName", "Date") %in% names(df))) {
      stop("Validation Failed: One or more dataframes are missing 'SimulationName' or 'Date'.")
    }
    if (ncol(df) < 3) {
      stop("Validation Failed: One or more dataframes are missing a variable of interest. Found only: ", paste(names(df), collapse = ", "))
    }
  })
  
  # ------------------------------------------------------------------
  # 2. EXTRACT AND BIND
  # ------------------------------------------------------------------
  df_list <- list_observed_clean %>%
    dplyr::pull(data)
  
  # bind_rows beautifully handles dataframes with different "variable of interest" columns,
  # filling rows with NA where a variable wasn't measured on a given day/simulation.
  df_final <- dplyr::bind_rows(df_list)
  
  # ------------------------------------------------------------------
  # 3. FORMAT FOR APSIM
  # ------------------------------------------------------------------
  df_final <- df_final %>%
    dplyr::mutate(
      # Safely format directly to avoid POSIXct timezone shifts
      Clock.Today = format(as.Date(Date), "%d/%m/%Y 00:00:00")
    ) %>%
    # Reorder columns: SimulationName first, Clock.Today second, everything else next.
    # The minus sign drops the old Date column safely.
    dplyr::select(SimulationName, Clock.Today, dplyr::everything(), -Date)
  
  return(df_final)
}