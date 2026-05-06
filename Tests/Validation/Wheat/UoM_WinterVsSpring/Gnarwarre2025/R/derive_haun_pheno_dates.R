#' Derive Interpolated Phenology Dates from Haun Stage
#'
#' @description
#' Calculates the Final Leaf Number (FLN), and interpolates dates for Maximum 
#' Leaf Appearance, Terminal Spikelet (FLN - 3), and Double Ridge (max(2, FLN - 6))
#' based on Haun stage observations.
#'
#' @details
#' **Flat Structure:** Expects a standard dataframe, bypassing nested tibble extraction.
#' **Date Parsing:** Uses a robust parser to handle `Clock.Today` strings before interpolating.
#' **Interpolation:** Uses base R `approx()` to interpolate dates safely.
#'
#' @param df A dataframe containing `SimulationName`, `Clock.Today`, and `Wheat.Phenology.HaunStage`.
#' @param max_leaf_limit Numeric. Fractional threshold for max leaf limit (default = 0.95).
#'
#' @return A data.frame with the `SimulationName`, FLN, and derived APSIM dates.
#'
#' @importFrom dplyr filter arrange group_by summarise mutate select n
#' @importFrom lubridate parse_date_time
#' @export
derive_haun_pheno_dates <- function(df, max_leaf_limit = 0.95) {
  
  require(dplyr)
  require(lubridate)
  
  # Fallback in case config passes NULL
  if (is.null(max_leaf_limit) || is.na(max_leaf_limit)) {
    max_leaf_limit <- 0.95
  }
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  req_cols <- c("SimulationName", "Clock.Today", "Wheat.Phenology.HaunStage")
  missing_cols <- setdiff(req_cols, names(df))
  
  if (length(missing_cols) > 0) {
    stop(sprintf("CRITICAL: Input dataframe is missing required columns: %s", 
                 paste(missing_cols, collapse = ", ")))
  }
  
  # ------------------------------------------------------------------
  # 2. DATE PARSING & FILTERING (THE FIREWALL)
  # ------------------------------------------------------------------
  df_clean <- df %>%
    dplyr::filter(!is.na(Wheat.Phenology.HaunStage)) %>%
    # Explicitly filter literal "NA" strings to prevent store_class_format crash
    dplyr::filter(!is.na(Clock.Today), 
                  as.character(Clock.Today) != "",
                  as.character(Clock.Today) != "NA") %>%
    dplyr::mutate(
      SimulationName = as.character(SimulationName),
      .temp_date = suppressWarnings(
        lubridate::parse_date_time(
          as.character(Clock.Today), 
          orders = c("dmy HMS", "ymd HMS", "dmy", "ymd", "Ymd")
        )
      ),
      .temp_date = as.Date(.temp_date)
    ) %>%
    dplyr::filter(!is.na(.temp_date))
  
  # ------------------------------------------------------------------
  # 3. CALCULATE METRICS AND INTERPOLATE
  # ------------------------------------------------------------------
  df_derived <- df_clean %>%
    dplyr::arrange(SimulationName, .temp_date) %>%
    dplyr::group_by(SimulationName) %>%
    dplyr::summarise(
      LeafNumberMaximum = max(Wheat.Phenology.HaunStage, na.rm = TRUE),
      FLN               = as.integer(round(LeafNumberMaximum)),
      LeafNumberLimit   = LeafNumberMaximum * max_leaf_limit,
      
      Haun_TS = FLN - 3,
      Haun_DR = max(2, FLN - 6),
      
      # Force numeric typing on the approx() return to prevent list-column coercion
      Date_Num_Limit = if (n() >= 2 && LeafNumberMaximum > 0) as.numeric(approx(x = Wheat.Phenology.HaunStage, y = as.numeric(.temp_date), xout = LeafNumberLimit, rule = 2, ties = "mean")$y) else NA_real_,
      Date_Num_TS    = if (n() >= 2 && LeafNumberMaximum > 0) as.numeric(approx(x = Wheat.Phenology.HaunStage, y = as.numeric(.temp_date), xout = Haun_TS, rule = 2, ties = "mean")$y) else NA_real_,
      Date_Num_DR    = if (n() >= 2 && LeafNumberMaximum > 0) as.numeric(approx(x = Wheat.Phenology.HaunStage, y = as.numeric(.temp_date), xout = Haun_DR, rule = 2, ties = "mean")$y) else NA_real_,
      
      .groups = "drop"
    ) %>%
    
    # ------------------------------------------------------------------
  # 4. RE-CAST DATES
  # ------------------------------------------------------------------
  dplyr::mutate(
    Date                                                        = as.Date(Date_Num_Limit, origin = "1970-01-01"),
    `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress` = as.Date(Date_Num_TS, origin = "1970-01-01"),
    `[Wheat].Phenology.LeavesInitiating.DateToProgress`         = as.Date(Date_Num_DR, origin = "1970-01-01")
  ) %>%
    dplyr::select(
      SimulationName, FLN, LeafNumberMaximum,
      Date,
      `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress`,
      `[Wheat].Phenology.LeavesInitiating.DateToProgress`
    )
  
  message(sprintf("Successfully derived Haun phenology dates (Limit, TS, DR) for %d simulations.", 
                  nrow(df_derived)))
  
  return(df_derived)
}