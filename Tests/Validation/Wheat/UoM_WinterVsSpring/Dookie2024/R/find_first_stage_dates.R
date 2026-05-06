#' Interpolate Phenology Stages and Inject Sowing Dates
#'
#' Filters for Wheat Phenology Stages, extracts the first date each stage is reached
#' per simulation, and injects a sowing event (Stage = 1) from the sowing dates table.
#'
#' @param df_sowDates_by_sim Dataframe. Contains Sowing dates by SimulationName.
#' @param df_haun_stages Dataframe. The merged and pivoted observational data.
#' @param sow_date_col Character. The name of the date column in df_sowDates_by_sim. 
#' @return A dataframe (tibble) containing the combined phenology timeline.
#' @export
find_first_stage_dates <- function(df_sowDates_by_sim, 
                                               file_pheno_haun_obs, 
                            sow_date_col = "SowingDate") {
  
  # 1 & 2. Filter stages, enforce Date format, and find the first day each stage is hit
  df_stages <- file_pheno_haun_obs %>%
    dplyr::filter(VarName == "Wheat.Phenology.Stage") %>%
    dplyr::mutate(Clock.Today = as.Date(Clock.Today)) %>%
    # Group by Simulation and the exact Stage (VarValue)
    dplyr::group_by(SimulationName, VarValue) %>%
    # Extract the earliest date (first row) for that specific stage
    dplyr::slice_min(order_by = Clock.Today, n = 1, with_ties = FALSE) %>%
    dplyr::ungroup()
  
  # 3. Create the Sowing Date rows (VarValue = 1) for each SimulationName
  df_sowing <- df_sowDates_by_sim %>%
    # Rename the specific sowing date column to 'Clock.Today' so it binds seamlessly
    dplyr::select(
      SimulationName,
      Clock.Today = dplyr::all_of(sow_date_col)
    ) %>%
    dplyr::mutate(
      Clock.Today = as.Date(Clock.Today),
      VarName = "Wheat.Phenology.Stage",
      VarValue = 1 # APSIM Stage 1 represents Sowing
    )
  
  # Bind them together and arrange chronologically so the timeline makes sense
  df_final <- dplyr::bind_rows(df_sowing, df_stages) %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  return(df_final)
}