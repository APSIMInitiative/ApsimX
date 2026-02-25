#' Merge Phenology Stage Dates into Observed Means
#'
#' @description
#' This function takes the continuous numeric observations and stacks them with 
#' the discrete phenology event dates. It uses bind_rows to ensure SimulationName 
#' and Clock.Today align, while filling non-matching columns with NA.
#'
#' @param df_obs The main data frame of observed means (file_obs_mean).
#' @param df_pheno The data frame containing interpolated phenology dates (df_obs_pheno_dates).
#'
#' @return A data frame containing both continuous data and discrete phenology stage markers.
#' 
#' @export
add_stages_to_obs <- function(df_obs, df_pheno) {
  
  # 1. Align the Phenology data to the Observation structure
  # We ensure Clock.Today is a Date object so it matches the obs data
  df_pheno_to_stack <- df_pheno %>%
    dplyr::mutate(Clock.Today = lubridate::as_date(PhenoDate)) %>%
    dplyr::select(SimulationName, Clock.Today, Wheat.Phenology.Stage)
  
  # 2. Bind them together
  # bind_rows handles different column sets by filling with NA
  df_combined <- dplyr::bind_rows(df_obs, df_pheno_to_stack) %>%
    # 3. Sort by Simulation and Date so the stages appear chronologically
    dplyr::arrange(SimulationName, Clock.Today)
  
  return(df_combined)
}