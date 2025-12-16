# R/functions.R

prepare_final_observed <- function(list_observed_clean,df_sim_names) {
  # --- Setup ---
  # Ensure all required packages are loaded
  # Add 'openxlsx' and 'lubridate' to tar_option_set(packages = ...) in _targets.R
  require(dplyr)
 # require(purrr)
  # require(openxlsx)
  
  # 1. Bind all internal data frames into one large data frame
  # list_observed_clean is a tibble with columns like df_name and data (list-column)
  df_list <- list_observed_clean %>%
    pull(data) # Extract the list of data frames from the 'data' column
  
  df_final <- dplyr::bind_rows(df_list) %>%
    # 2. Merge with simulation names
    dplyr::inner_join(df_sim_names, by = "Cultivar") %>%
    # 3. Create the 'Clock.Today' column in the required APSIM format
    dplyr::mutate(
      Clock.Today = format(
        as.POSIXct(Date),
        "%d/%m/%Y 00:00:00"
      )
    ) %>%
    # 4. Reorder and select final columns
    dplyr::select(SimulationName, Clock.Today, dplyr::everything(), -Cultivar, -Date)
  
  # Return the data frame (the actual target value)
  return(df_final)
}