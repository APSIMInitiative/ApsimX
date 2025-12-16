apply_corrections <- function(df_tbl, df_stages_Observ) {
  
  # 1. Ensure required packages are available
  # It's better practice to ensure packages are loaded outside a function, 
  # but within a script, 'require' works. Let's keep it for context.
  if (!requireNamespace("lubridate", quietly = TRUE)) stop("Package lubridate required.")
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package dplyr required.")
  if (!requireNamespace("tidyr", quietly = TRUE)) stop("Package tidyr required.")
  if (!requireNamespace("purrr", quietly = TRUE)) stop("Package purrr required.")
  
  # 2. Create the Date Lookup Table (Wide Format)
  date_lookup <- df_stages_Observ %>%
    dplyr::filter(Wheat.Phenology.Stage %in% c(6, 8)) %>%
    dplyr::select(Cultivar, Date, Wheat.Phenology.Stage) %>%
    dplyr::mutate(PhenoDate = paste0("PhenoDate_", Wheat.Phenology.Stage)) %>%
    dplyr::select(-Wheat.Phenology.Stage) %>%
    # Use the spread function as you defined (or pivot_wider for modern tidyverse)
    tidyr::spread(PhenoDate, Date)
  
  # Check for the expected columns in the lookup table (a sanity check)
  if (!all(c("PhenoDate_6", "PhenoDate_8") %in% names(date_lookup))) {
    stop("Lookup table is missing 'PhenoDate_6' or 'PhenoDate_8'. Check tidyr::spread output.")
  }
  
  # 3. Apply corrections using purrr::map2 within mutate
  # Assuming df_tbl is a data frame or tibble with columns 'df_name' and 'data' (list-column of dataframes)
  df_tbl <- df_tbl %>%
    dplyr::mutate(
      data = purrr::map2(
        .x = data,
        .y = df_name,
        .f = function(df, nm) {
          
          # 1. Fix NDVI offset year (kept for completeness)
          if (nm == "ndvi_raw") {
            df <- df %>%
              dplyr::mutate(
                Date = if_else(
                  lubridate::year(Date) == 2025,
                  Date %m+% lubridate::years(-1),
                  Date
                )
              )
          }
          
          # 2. Biomass missing dates (Group 6)
          if (nm %in% c("stemYield_6_raw", "spikeYield_6_raw", "senescLeafYield_6_raw",
                        "totalAboveGround_6_raw", "par_6_raw", "greenLeaf_6_raw")) {
            
            # --- START FIX: Group 6 ---
            df <- df %>%
              # Join the lookup table based on Cultivar
              dplyr::left_join(date_lookup, by = "Cultivar") %>%
              # Use the date from the correct column (PhenoDate_6) to update df$Date
              # Note the use of the correct column name: PhenoDate_6
              dplyr::mutate(Date = .data$PhenoDate_6) %>% 
              # Remove the temporary lookup columns (PhenoDate_6 and PhenoDate_8)
              dplyr::select(-.data$PhenoDate_6, -.data$PhenoDate_8)
            # --- END FIX: Group 6 ---
          }
          
          # 3. Biomass missing dates (Group 8)
          if (nm %in% c("stemYield_8_raw", "spikeYield_8_raw", "senescLeafYield_8_raw",
                        "totalAboveGround_8_raw", "par_8_raw", "greenLeaf_8_raw")) {
            
            # --- START FIX: Group 8 ---
            df <- df %>%
              # Join the lookup table based on Cultivar
              dplyr::left_join(date_lookup, by = "Cultivar") %>%
              # Use the date from the correct column (PhenoDate_8) to update df$Date
              # Note the use of the correct column name: PhenoDate_8
              dplyr::mutate(Date = .data$PhenoDate_8) %>%
              # Remove the temporary lookup columns (PhenoDate_6 and PhenoDate_8)
              dplyr::select(-.data$PhenoDate_6, -.data$PhenoDate_8)
            # --- END FIX: Group 8 ---
          }
          
          return(df)
        }
      )
    )
  
  return(df_tbl)
}