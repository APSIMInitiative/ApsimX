#' Prepare Final Observations for APSIM
#'
#' @description
#' Unnests the master observation list, binds all dataframes together, 
#' aggressively scrubs "ghost" empty rows, and formats the timestamps into 
#' the strict `Clock.Today` format required by APSIM.
#'
#' @param list_observed_clean A nested tibble containing the final corrected dataframes.
#'
#' @return A single, wide dataframe ready for APSIM injection without empty rows.
#'
#' @importFrom dplyr bind_rows filter mutate select any_of everything
#' @export
prepare_final_observed <- function(list_observed_clean) {
  
  require(dplyr)
  
  # 1. Bind all internal data frames into one large master dataframe
  df_final <- dplyr::bind_rows(list_observed_clean$data) %>%
    
    # ----------------------------------------------------------------
  # 2. THE FIREWALL: Aggressively scrub "ghost" rows
  # ----------------------------------------------------------------
  # Drop any row where SimulationName or Date is NA or completely blank
  dplyr::filter(
    !is.na(SimulationName), 
    SimulationName != "",
    !is.na(Date)
  ) %>%
    
    # 3. Create the 'Clock.Today' column in the required APSIM format
    dplyr::mutate(
      Clock.Today = format(as.POSIXct(Date), "%d/%m/%Y 00:00:00")
    ) %>%
    
    # 4. Reorder and safely drop old columns
    dplyr::select(
      SimulationName, 
      Clock.Today, 
      dplyr::everything(), 
      -dplyr::any_of(c("Cultivar", "Date"))
    )
  
  message("Successfully compiled, scrubbed empty rows, and formatted the final observation dataframe.")
  return(df_final)
}