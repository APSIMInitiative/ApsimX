# R/functions.R

apply_corrections <- function(df_tbl) {
  
  # 1. Ensure dplyr is loaded for mutate (and your other tidyverse functions)
  require(lubridate)
  require(dplyr)
  
  # Use dplyr::mutate to safely replace the 'data' column
  df_tbl <- df_tbl %>%
    mutate(
      data = purrr::map2(
        .x = data,
        .y = df_name,
        .f = function(df, nm) {
          # Fix NDVI offset year
          if (nm == "ndvi_raw") {
            df <- df %>%
              mutate(
                Date = if_else(
                  year(Date) == 2025,
                  Date %m+% years(-1),
                  Date
                )
              )
          }
          
          # Biomass missing dates (Group 6)
          if (nm %in% c("stemYield_6_raw","spikeYield_6_raw","senescLeafYield_6_raw",
                        "totalAboveGround_6_raw","par_6_raw","greenLeaf_6_raw")) {
            df$Date <- lubridate::dmy("28/08/2024") # Added lubridate:: for clarity
          }
          
          # Biomass missing dates (Group 8)
          if (nm %in% c("stemYield_8_raw","spikeYield_8_raw","senescLeafYield_8_raw",
                        "totalAboveGround_8_raw","par_8_raw","greenLeaf_8_raw")) {
            df$Date <- lubridate::dmy("23/09/2024") # Added lubridate:: for clarity
          }
          
          return(df)
        }
      )
    )
  
  return(df_tbl)
}