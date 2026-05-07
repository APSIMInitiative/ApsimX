#' Derive Interpolated Phenology Dates from Haun Stage
#'
#' @description
#' Scans a nested observation dataframe for a "haun" dataset. Calculates the 
#' Final Leaf Number (FLN), and interpolates dates for Maximum Leaf Appearance, 
#' Terminal Spikelet (FLN - 3), and Double Ridge (max(2, FLN - 6)).
#'
#' @details
#' **Nested Structure:** Expects a nested tibble with `df_name` and `data` columns.
#' **Dynamic Grouping:** Groups by `SimulationName` or `Cultivar`.
#' **Interpolation:** Uses base R `approx()` to interpolate dates safely.
#'
#' @param compiled_obs A nested dataframe containing `df_name` and `data` columns.
#' @param max_leaf_limit Numeric. Fractional threshold for max leaf limit (default = 0.95).
#'
#' @return A data.frame with the grouping variable, FLN, and derived APSIM dates.
#'
#' @importFrom dplyr filter arrange group_by summarise mutate select n
#' @importFrom rlang .data
#' @export
derive_haun_pheno_dates <- function(compiled_obs, max_leaf_limit) {
  
  #compiled_obs <- df1
  #max_leaf_limit <- 0.95
  
  require(dplyr)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. EXTRACT TARGET DATAFRAME FROM NESTED TIBBLE
  # ------------------------------------------------------------------
  if (!all(c("df_name", "data") %in% names(compiled_obs))) {
    stop("CRITICAL: Input must be a nested dataframe containing 'df_name' and 'data' columns.")
  }
  
  haun_idx <- grep("haun", compiled_obs$df_name, ignore.case = TRUE)
  
  if (length(haun_idx) == 0) {
    stop("CRITICAL: No row with 'haun' in 'df_name' was found.")
  }
  
  df_haun <- compiled_obs$data[[haun_idx[1]]]
  
  # ------------------------------------------------------------------
  # 2. VALIDATE COLUMNS AND IDENTIFIER
  # ------------------------------------------------------------------
  req_cols <- c("Date", "Wheat.Phenology.HaunStage")
  missing_cols <- setdiff(req_cols, names(df_haun))
  
  if (length(missing_cols) > 0) {
    stop(sprintf("CRITICAL: The 'haun' dataframe is missing required columns: %s", 
                 paste(missing_cols, collapse = ", ")))
  }
  
  if ("SimulationName" %in% names(df_haun)) {
    grp_col <- "SimulationName"
  } else if ("Cultivar" %in% names(df_haun)) {
    grp_col <- "Cultivar"
  } else {
    stop("CRITICAL: Neither 'SimulationName' nor 'Cultivar' were found to group by.")
  }
  
  # ------------------------------------------------------------------
  # 3. CALCULATE METRICS AND INTERPOLATE
  # ------------------------------------------------------------------
  df_derived <- df_haun %>%
    dplyr::filter(!is.na(Date), !is.na(Wheat.Phenology.HaunStage)) %>%
    dplyr::arrange(.data[[grp_col]], Date) %>%
    dplyr::group_by(.data[[grp_col]]) %>%
    dplyr::summarise(
      
      # Step A: Base Leaf Math
      LeafNumberMaximum = max(Wheat.Phenology.HaunStage, na.rm = TRUE),
      FLN               = as.integer(round(LeafNumberMaximum)),
      LeafNumberLimit   = LeafNumberMaximum * max_leaf_limit,
      
      # Step B: Target Haun Stages for Phenology Events
      Haun_TS = FLN - 3,
      Haun_DR = max(2, FLN - 6),
      
      # Step C: Execute Safe Numeric Interpolations
      Date_Num_Limit = if (n() >= 2 && LeafNumberMaximum > 0) approx(x = Wheat.Phenology.HaunStage, y = as.numeric(Date), xout = LeafNumberLimit, rule = 2, ties = "mean")$y else NA_real_,
      Date_Num_TS    = if (n() >= 2 && LeafNumberMaximum > 0) approx(x = Wheat.Phenology.HaunStage, y = as.numeric(Date), xout = Haun_TS, rule = 2, ties = "mean")$y else NA_real_,
      Date_Num_DR    = if (n() >= 2 && LeafNumberMaximum > 0) approx(x = Wheat.Phenology.HaunStage, y = as.numeric(Date), xout = Haun_DR, rule = 2, ties = "mean")$y else NA_real_,
      
      .groups = "drop"
    ) %>%
    # Step D: Convert back to Native R Dates and map to APSIM Names
    dplyr::mutate(
      Date                                                        = as.Date(Date_Num_Limit, origin = "1970-01-01"),
      `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress` = as.Date(Date_Num_TS, origin = "1970-01-01"),
      `[Wheat].Phenology.LeavesInitiating.DateToProgress`         = as.Date(Date_Num_DR, origin = "1970-01-01")
    ) %>%
    # Clean up intermediate calculation columns
    dplyr::select(
      -Date_Num_Limit, -Date_Num_TS, -Date_Num_DR, 
      -Haun_TS, -Haun_DR
    )
  
  # ------------------------------------------------------------------
  # 4. CONSOLE NOTIFICATION
  # ------------------------------------------------------------------
  message(sprintf("Successfully derived Haun phenology dates (Limit, TS, DR) for %d '%s' groups.", 
                  nrow(df_derived), grp_col))
  
  return(df_derived)
}