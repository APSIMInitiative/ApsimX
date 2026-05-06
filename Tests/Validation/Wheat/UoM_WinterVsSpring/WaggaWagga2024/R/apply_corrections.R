#' Apply Manual Data Corrections to Raw Observations
#'
#' @description
#' Performs targeted manual corrections on the raw observation data. Specifically, 
#' it fixes a known year-offset issue in the NDVI data and patches missing dates 
#' for biomass sampling at stages 6 (Stem Elongation) and 8 (Flowering) by 
#' cross-referencing the synthetic phenology dates.
#'
#' @details
#' **Structural Integrity:** This function relies strictly on `SimulationName` 
#' to join the missing dates. This ensures that the patched dates perfectly match 
#' the timeline of the specific simulation, maintaining the pipeline's Single Source of Truth.
#'
#' @param df_tbl A nested tibble containing the raw `df_name` and `data` (list-column).
#' @param df_stages_Observ Data frame containing synthetic phenology dates with 
#'   `SimulationName`, `Date`, and `Wheat.Phenology.Stage`.
#'
#' @return The nested tibble with the internal dataframes corrected.
#'
#' @importFrom dplyr filter select mutate left_join if_else
#' @importFrom tidyr pivot_wider
#' @importFrom purrr map2
#' @importFrom lubridate year `%m+%` years
#' @importFrom rlang .data
#' @export
apply_corrections <- function(df_tbl, df_stages_Observ) {
  
  require(dplyr)
  require(tidyr)
  require(purrr)
  require(lubridate)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS & PREPARATION
  # ------------------------------------------------------------------
  if (!"SimulationName" %in% names(df_stages_Observ)) {
    stop("CRITICAL: 'df_stages_Observ' must contain a 'SimulationName' column.")
  }
  
  # Ensure the target stage column exists (checking both possible naming conventions just in case)
  stage_col <- names(df_stages_Observ)[grepl("Stage", names(df_stages_Observ))]
  if (length(stage_col) != 1) {
    stop("CRITICAL: Could not uniquely identify the Stage numeric column in 'df_stages_Observ'.")
  }
  stage_col <- stage_col[1]
  
  # ------------------------------------------------------------------
  # 2. CREATE DATE LOOKUP TABLE (WIDE FORMAT)
  # ------------------------------------------------------------------
  date_lookup <- df_stages_Observ %>%
    dplyr::filter(.data[[stage_col]] %in% c(6, 8)) %>%
    # CRITICAL UPDATE: Use SimulationName instead of Cultivar
    dplyr::select(SimulationName, Date, !!stage_col) %>%
    dplyr::mutate(PhenoDate = paste0("PhenoDate_", .data[[stage_col]])) %>%
    dplyr::select(-!!stage_col) %>%
    # Modernized to pivot_wider instead of the deprecated spread()
    tidyr::pivot_wider(names_from = PhenoDate, values_from = Date)
  
  if (!all(c("PhenoDate_6", "PhenoDate_8") %in% names(date_lookup))) {
    stop("CRITICAL: Lookup table failed to generate 'PhenoDate_6' or 'PhenoDate_8'. Check inputs.")
  }
  
  # ------------------------------------------------------------------
  # 3. APPLY CORRECTIONS WITHIN NESTED TIBBLE
  # ------------------------------------------------------------------
  df_tbl_corrected <- df_tbl %>%
    dplyr::mutate(
      data = purrr::map2(
        .x = data,
        .y = df_name,
        .f = function(df, nm) {
          
          # -----------------------------------------
          # Fix 1: NDVI offset year
          # -----------------------------------------
          if (nm == "ndvi_raw") {
            df <- df %>%
              dplyr::mutate(
                Date = dplyr::if_else(
                  lubridate::year(Date) == 2025,
                  Date %m+% lubridate::years(-1),
                  Date
                )
              )
          }
          
          # -----------------------------------------
          # Fix 2: Biomass missing dates (Group 6)
          # -----------------------------------------
          if (nm %in% c("stemYield_6_raw", "spikeYield_6_raw", "senescLeafYield_6_raw",
                        "totalAboveGround_6_raw", "par_6_raw", "greenLeaf_6_raw")) {
            
            # Ensure the raw df actually has SimulationName before trying to join
            if (!"SimulationName" %in% names(df)) stop(sprintf("Dataframe %s is missing 'SimulationName'", nm))
            
            df <- df %>%
              dplyr::left_join(date_lookup, by = "SimulationName") %>%
              dplyr::mutate(Date = .data$PhenoDate_6) %>% 
              dplyr::select(-.data$PhenoDate_6, -.data$PhenoDate_8)
          }
          
          # -----------------------------------------
          # Fix 3: Biomass missing dates (Group 8)
          # -----------------------------------------
          if (nm %in% c("stemYield_8_raw", "spikeYield_8_raw", "senescLeafYield_8_raw",
                        "totalAboveGround_8_raw", "par_8_raw", "greenLeaf_8_raw")) {
            
            # Ensure the raw df actually has SimulationName before trying to join
            if (!"SimulationName" %in% names(df)) stop(sprintf("Dataframe %s is missing 'SimulationName'", nm))
            
            df <- df %>%
              dplyr::left_join(date_lookup, by = "SimulationName") %>%
              dplyr::mutate(Date = .data$PhenoDate_8) %>%
              dplyr::select(-.data$PhenoDate_6, -.data$PhenoDate_8)
          }
          
          return(df)
        }
      )
    )
  
  message("Successfully applied manual corrections (NDVI years and Biomass missing dates).")
  return(df_tbl_corrected)
}