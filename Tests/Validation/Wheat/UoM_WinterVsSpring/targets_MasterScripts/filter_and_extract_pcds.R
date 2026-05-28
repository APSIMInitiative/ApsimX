#' Filter and Extract Specific Target Dataframes (Universal Master)
#'
#' @description
#' Scans a nested tibble of compiled observations and extracts only the datasets 
#' explicitly requested in the `pcd_stages` vector. It flattens the nested structure 
#' into a standard named list for downstream phenology interpolation.
#'
#' @param list_observed_dfs A nested tibble containing `df_name` and `data` columns.
#' @param pcd_stages Character vector. The exact names of the dataframes to extract 
#'   (e.g., c("stemYield_6_raw", "earYield_10_raw")).
#'
#' @return A named list of data frames containing only the requested datasets.
#'
#' @importFrom dplyr filter select
#' @importFrom tibble deframe
#' @export
filter_and_extract_pcds <- function(list_observed_dfs, pcd_stages) {
  
  require(dplyr)
  require(tibble)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!is.data.frame(list_observed_dfs) || !all(c("df_name", "data") %in% names(list_observed_dfs))) {
    stop("CRITICAL: Input must be a nested dataframe containing 'df_name' and 'data' columns.")
  }
  
  if (missing(pcd_stages) || length(pcd_stages) == 0) {
    stop("CRITICAL: The 'pcd_stages' target vector is empty or missing.")
  }
  
  # ------------------------------------------------------------------
  # 2. MATCHING & MISSING CHECKS
  # ------------------------------------------------------------------
  # Compare what we asked for vs what actually exists in the pipeline
  found_stages   <- intersect(pcd_stages, list_observed_dfs$df_name)
  missing_stages <- setdiff(pcd_stages, list_observed_dfs$df_name)
  
  # If absolutely nothing matched, kill the pipeline
  if (length(found_stages) == 0) {
    stop(sprintf(
      "CRITICAL: None of the requested datasets were found.\n  -> Looked for: [%s]", 
      paste(pcd_stages, collapse = ", ")
    ))
  }
  
  # If we are missing just some of them, warn the user but keep going
  if (length(missing_stages) > 0) {
    warning(sprintf(
      "Notice: The following requested datasets were not found and will be skipped:\n  -> [%s]", 
      paste(missing_stages, collapse = ", ")
    ), call. = FALSE)
  }
  
  # ------------------------------------------------------------------
  # 3. FILTER FOR EXACT MATCHES
  # ------------------------------------------------------------------
  # Filter the tibble for names that exist in our confirmed 'found_stages' list
  df_pcds_filtered <- list_observed_dfs %>%
    dplyr::filter(df_name %in% found_stages)
  
  # ------------------------------------------------------------------
  # 4. CONVERT TO NAMED LIST
  # ------------------------------------------------------------------
  # Extract the list of data frames from the 'data' column.
  # deframe() perfectly converts a two-column tibble (name, value) into a named list
  df_list_PCDS <- df_pcds_filtered %>%
    dplyr::select(df_name, data) %>%
    tibble::deframe() 
  
  # ------------------------------------------------------------------
  # 5. CONSOLE NOTIFICATION
  # ------------------------------------------------------------------
  message(sprintf("\u2705 Successfully extracted %d target dataframes.", length(df_list_PCDS)))
  
  return(df_list_PCDS)
}