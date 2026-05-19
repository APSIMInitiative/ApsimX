#' Filter and Extract PCDS Dataframes
#'
#' @description
#' Scans a nested tibble of compiled observations and extracts only the datasets 
#' containing "PCDS" in their name. It flattens the nested structure into a 
#' standard named list, preserving all internal data (including `SimulationName` 
#' and `Cultivar`) for downstream phenology interpolation.
#'
#' @details
#' **Nested Structure:** Expects a nested tibble input containing exactly 
#' `df_name` (character) and `data` (list-column of dataframes).
#' 
#' @param list_observed_dfs A nested tibble containing `df_name` and `data` columns.
#'
#' @return A named list of data frames (`df_list_PCDS`) containing only the 
#'   filtered PCDS datasets.
#'
#' @importFrom dplyr filter select
#' @importFrom tibble deframe
#' @export
filter_and_extract_pcds <- function(list_observed_dfs) {
  
  require(dplyr)
  require(tibble)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!is.data.frame(list_observed_dfs) || !all(c("df_name", "data") %in% names(list_observed_dfs))) {
    stop("CRITICAL: Input must be a nested dataframe containing 'df_name' and 'data' columns.")
  }
  
  # ------------------------------------------------------------------
  # 2. FILTER FOR PCDS
  # ------------------------------------------------------------------
  # Filter the tibble for names containing "PCDS" (case-insensitive)
  df_pcds_filtered <- list_observed_dfs %>%
    dplyr::filter(grepl("PCDS", df_name, ignore.case = TRUE))
  
  # Safety Net: Ensure we actually found something
  if (nrow(df_pcds_filtered) == 0) {
    stop("CRITICAL: No datasets containing 'PCDS' in their name were found in the input list.")
  }
  
  # ------------------------------------------------------------------
  # 3. CONVERT TO NAMED LIST
  # ------------------------------------------------------------------
  # Extract the list of data frames from the 'data' column.
  # deframe() perfectly converts a two-column tibble (name, value) into a named list
  df_list_PCDS <- df_pcds_filtered %>%
    dplyr::select(df_name, data) %>%
    tibble::deframe() 
  
  # ------------------------------------------------------------------
  # 4. CONSOLE NOTIFICATION
  # ------------------------------------------------------------------
  message(sprintf("Successfully filtered and extracted %d PCDS dataframes.", length(df_list_PCDS)))
  
  return(df_list_PCDS)
}