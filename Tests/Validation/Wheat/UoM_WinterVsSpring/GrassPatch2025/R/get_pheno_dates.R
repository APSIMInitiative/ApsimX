get_pheno_dates <- function(df, dateDOY) {
  
  # 1. Transform input string "dd-mm-yyyy" into a Date object
  ref_date <- lubridate::dmy(dateDOY)
  
  # 2. Perform the selection
  df_filtered <- df %>%
    dplyr::select(
      any_of(c("SimulationName", "Clock.Today", "Wheat.SowingData.Cultivar", "ReleaseYear")),
      matches("Wheat\\.Phenology.*DOY")
    )
  
  # 3. Create new date columns based on the DOY columns
  # We use across() to find the DOY columns and mutate them into Dates
  df_final <- df_filtered %>%
    mutate(
      across(
        matches("Wheat\\.Phenology.*DOY"), 
        # For each value (x), add it to the reference date
        \(x) ref_date + lubridate::days(as.integer(x) - 1),
        # This names the new columns ".Date" appended to the original name
        .names = "{.col}.Date"
      ) 
    ) %>%
    dplyr::select(SimulationName, matches("Date")) %>% 
    
    # 4. Pivot to long format
    tidyr::pivot_longer(
    cols = ends_with(".Date"),
    names_to = "PhenoEvent",
    values_to = "PhenoDate"
  ) %>%
    # 5. Clean up the event names 
    mutate(PhenoEvent = stringr::str_remove_all(PhenoEvent, 
                                                "Wheat|Phenology|Estimated|DOY|Date|\\.")) %>%
    na.omit() %>%
    # 5. Create the numeric Stage column
    mutate(Wheat.Phenology.Stage = case_when(
      PhenoEvent == "Emergence" ~ 3,
      PhenoEvent == "PCDS6"     ~ 6,
      PhenoEvent == "PCDS8"     ~ 8,
      PhenoEvent == "PCDS10"    ~ 10,
      TRUE                      ~ NA_real_  # Default for other events
    ))
  
  return(df_final)
}