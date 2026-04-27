#' Tidy Up Haun Stage Observations
#'
#' @description
#' Filters a long-format observation dataframe for Haun stage measurements 
#' and pivots it to a wide format. The resulting dataframe is perfectly 
#' formatted for injection into downstream phenology derivation functions.
#'
#' @details
#' Expects an input dataframe with `SimulationName`, a date column (`Clock.Today` or `Date`),
#' `VarName`, and `VarValue`. It filters for `VarName == "Wheat.Phenology.HaunStage"`,
#' casts the values to numeric, and safely spreads the data.
#'
#' @param df Data frame containing long-format observations.
#'
#' @return A wide data frame with `SimulationName`, `Clock.Today`, and `Wheat.Phenology.HaunStage`.
#'
#' @importFrom dplyr filter select mutate rename
#' @importFrom tidyr pivot_wider
#' @importFrom rlang .data
#' @export
tidy_up_haun <- function(df) {
  
  require(dplyr)
  require(tidyr)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!is.data.frame(df)) {
    stop("CRITICAL: Input 'df' must be a dataframe.")
  }
  
  req_cols <- c("SimulationName", "VarName", "VarValue")
  missing_cols <- setdiff(req_cols, names(df))
  if (length(missing_cols) > 0) {
    stop(sprintf("CRITICAL: Missing required columns: %s", paste(missing_cols, collapse = ", ")))
  }
  
  if (!"Clock.Today" %in% names(df) && !"Date" %in% names(df)) {
    stop("CRITICAL: Dataframe must contain either a 'Clock.Today' or 'Date' column.")
  }
  
  # ------------------------------------------------------------------
  # 2. FILTER FOR HAUN STAGE
  # ------------------------------------------------------------------
  df_filtered <- df %>%
    dplyr::filter(VarName == "Wheat.Phenology.HaunStage")
  
  if (nrow(df_filtered) == 0) {
    stop("CRITICAL: No rows found where VarName == 'Wheat.Phenology.HaunStage'.")
  }
  
  # ------------------------------------------------------------------
  # 3. STANDARDIZE DATE & CLEAN
  # ------------------------------------------------------------------
  # Ensure the date column is strictly named 'Clock.Today' for downstream compatibility
  if (!"Clock.Today" %in% names(df_filtered)) {
    df_filtered <- df_filtered %>% dplyr::rename(Clock.Today = Date)
  }
  
  df_clean <- df_filtered %>%
    dplyr::filter(!is.na(VarValue)) %>%
    # Ensure VarValue is numeric before pivoting to prevent math errors later
    dplyr::mutate(VarValue = suppressWarnings(as.numeric(VarValue))) %>%
    # Isolate only essential columns to prevent extra metadata from fracturing the pivot
    dplyr::select(SimulationName, Clock.Today, VarName, VarValue)
  
  # ------------------------------------------------------------------
  # 4. PIVOT TO WIDE FORMAT
  # ------------------------------------------------------------------
  df_wide <- df_clean %>%
    tidyr::pivot_wider(
      names_from = VarName,
      values_from = VarValue,
      # Protection against duplicates: average multiple measurements on the same day
      values_fn = list(VarValue = mean) 
    )
  
  # Final sanity check to ensure the column was successfully created
  if (!"Wheat.Phenology.HaunStage" %in% names(df_wide)) {
    stop("CRITICAL: Pivot failed to create 'Wheat.Phenology.HaunStage' column.")
  }
  
  message(sprintf("Successfully tidied Haun observations: extracted %d data points across %d simulations.", 
                  sum(!is.na(df_wide$Wheat.Phenology.HaunStage)), 
                  length(unique(df_wide$SimulationName))))
  
  return(df_wide)
}