#' Generate Synthetic Observation Data from Wide Master Timeline
#'
#' @description
#' Transforms the consolidated, wide-format APSIM stage inputs into a vertical 
#' observation dataframe. It maps the bracketed APSIM column names back to 
#' standard numeric phenology codes (e.g., Emerging = 3, Double Ridge = 4).
#'
#' @details
#' **Single Source of Truth:** By using the finalized `haunBased` dataframe 
#' as the sole input, this function guarantees that the synthetic observations 
#' generated for `Observed.xlsx` perfectly match the forced inputs parameterized 
#' in the simulation, without needing upstream Cultivar joins.
#'
#' @param df_haunBased Data frame. The wide-format, prioritized APSIM stage inputs.
#' @param var_name Character. The name of the new stage column (e.g., "Wheat.Phenology.Stage").
#'
#' @return A simplified dataframe containing strictly `SimulationName`, `Date`, 
#'   and the mapped numeric stage column.
#'
#' @importFrom dplyr filter mutate case_when select
#' @importFrom tidyr pivot_longer
#' @importFrom rlang .data `:=`
#' @export
doStageObsData <- function(df_haunBased, var_name) {
  
  require(dplyr)
  require(tidyr)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!"SimulationName" %in% names(df_haunBased)) {
    stop("CRITICAL: 'df_haunBased' must contain a 'SimulationName' column.")
  }
  
  # ------------------------------------------------------------------
  # 2. EXTRACT AND PIVOT MASTER TIMELINE
  # ------------------------------------------------------------------
  # Pivot the wide APSIM format back into a long observation format
  df_long <- df_haunBased %>%
    tidyr::pivot_longer(
      cols = -SimulationName,
      names_to = "StageName",
      values_to = "DateStr"
    ) %>%
    # Drop any stages that didn't get a date calculated (or are NA strings)
    dplyr::filter(!is.na(DateStr) & DateStr != "NA" & DateStr != "")
  
  # ------------------------------------------------------------------
  # 3. MAP TO NUMERIC CODES & FORMAT
  # ------------------------------------------------------------------
  df_final <- df_long %>%
    dplyr::mutate(
      # Convert the APSIM string "dd-mm-yyyy" back to a native R Date
      # tryFormats adds a safety net just in case dates were passed as yyyy-mm-dd
      Date = as.Date(DateStr, tryFormats = c("%d-%m-%Y", "%Y-%m-%d")),
      
      # Map APSIM column names to their exact numerical stage counterparts
      !!var_name := dplyr::case_when(
        grepl("Emerging", StageName, ignore.case = TRUE)                 ~ 3,
        grepl("LeavesInitiating", StageName, ignore.case = TRUE)         ~ 4, # Double Ridge
        grepl("SpikeletsDifferentiating", StageName, ignore.case = TRUE) ~ 5, # Terminal Spikelet
        grepl("StemElongating", StageName, ignore.case = TRUE)           ~ 6,
        grepl("Heading", StageName, ignore.case = TRUE)                  ~ 7,
        grepl("Flowering", StageName, ignore.case = TRUE)                ~ 8,
        grepl("GrainFilling", StageName, ignore.case = TRUE)             ~ 10,
        TRUE                                                             ~ NA_real_
      )
    ) %>%
    # Drop NAs if an unexpected stage slipped through the mapping
    dplyr::filter(!is.na(.data[[var_name]])) %>%
    
    # ------------------------------------------------------------------
  # 4. SIMPLIFY OUTPUT
  # ------------------------------------------------------------------
  # Return strictly the three requested columns
  dplyr::select(SimulationName, Date, !!var_name)
  
  # ------------------------------------------------------------------
  # 5. CONSOLE NOTIFICATION
  # ------------------------------------------------------------------
  message(sprintf("Successfully extracted and mapped '%s' synthetic stages from the wide Haun timeline.", var_name))
  
  return(df_final)
}