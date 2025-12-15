# R/functions.R

#' Filters the observed data list for data frames containing "PCDS" in their name
#' and extracts them into a simple list of data frames.
#'
#' @param list_observed_dfs A tibble with columns 'df_name' (character) and
#'                          'data' (list-column of data frames).
#' @return A simple named list of data frames (df_list_PCDS)

filter_and_extract_pcds <- function(list_observed_dfs) {
  # Load necessary packages
  require(dplyr)
  require(purrr)
  
  # 1. Filter the tibble for names containing "PCDS" (case sensitive)
  df_pcds_filtered <- list_observed_dfs %>%
    dplyr::filter(grepl("PCDS", df_name, ignore.case = TRUE))
  
  # 2. Extract the list of data frames from the 'data' column
  # We use purrr::set_names() to maintain the df_name as the list name
  df_list_PCDS <- df_pcds_filtered %>%
    dplyr::select(df_name, data) %>%
    tibble::deframe() # Converts to a named list (df_name -> data)
  
  # Note: If you want an UNNAMED list of dfs, you would use:
  # df_list_PCDS <- df_pcds_filtered %>% purrr::pull(data)
  
  return(df_list_PCDS)
}