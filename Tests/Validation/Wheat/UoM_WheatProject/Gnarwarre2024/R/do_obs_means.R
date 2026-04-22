

do_obs_means <- function(df) {
  
  df_mean <- df %>%
    dplyr::select(-Rep, -Plot) %>%
    group_by(SimulationName, Clock.Today) %>%
    summarise(
      across(where(is.numeric),\(x) mean(x, na.rm = TRUE)), 
      .groups = "drop" # Good practice to ungroup at the end
    ) %>%
    # Replace all NaN values with NA in numeric columns
    mutate(across(where(is.numeric), \(x) ifelse(is.nan(x), NA, x)))
  
  return(df_mean)
} 