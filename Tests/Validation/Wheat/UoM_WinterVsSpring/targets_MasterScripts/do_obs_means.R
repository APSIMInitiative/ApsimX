#' Calculate Mean of Observations by Simulation and Date
#'
#' @description
#' A pure mathematical averaging engine. Groups solely by SimulationName 
#' and Clock.Today, dropping all other non-numeric metadata (like Cultivar) 
#' to ensure a perfectly clean numerical output.
#'
#' @param df Dataframe containing raw observations with Rep/Plot data.
#' @return A dataframe of averaged numeric values.
#' @export
do_obs_means <- function(df) {
  
  df_mean <- df %>%
    # Drop the physical trial layout identifiers
    dplyr::select(-dplyr::any_of(c("Rep", "Plot"))) %>%
    
    # The pure grouping keys
    dplyr::group_by(SimulationName, Clock.Today) %>%
    
    # The Math Engine
    dplyr::summarise(
      dplyr::across(tidyselect::where(is.numeric), \(x) mean(x, na.rm = TRUE)), 
      .groups = "drop" 
    ) %>%
    
    # NA standardization (converts NaN from 0/0 divisions back to NA)
    dplyr::mutate(
      dplyr::across(tidyselect::where(is.numeric), \(x) ifelse(is.nan(x), NA, x))
    )
  
  return(df_mean)
}