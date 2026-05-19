#' Derive Interpolated Phenology Dates from Haun Stage
#'
#' @description
#' Extracts main-stem leaf emergence records from a nested observation structure,
#' calculates the Final Leaf Number (FLN), and applies a monotonic linear approximation 
#' to map specific physiological milestones—Maximum Leaf Appearance, Terminal Spikelet (FLN - 3), 
#' and Double Ridge (max(2, FLN - 6))—to true calendar dates.
#'
#' @details
#' **Defensive Data Ordering:** Base R's \code{approx()} requires the interpolation predictor 
#' (\code{x}) to be strictly increasing. This function explicitly intercepts grouped slices 
#' and orders them by the raw Haun Stage values before running the interpolation. This prevents 
#' pipeline crashes or date twisting caused by duplicated or plateaued field measurements.
#'
#' @param compiled_obs A nested data frame containing at least \code{df_name} (character) 
#'   and \code{data} (list of data frames) columns.
#' @param max_leaf_limit Numeric. Fractional threshold coefficient used to pinpoint the date 
#'   of maximum leaf development (default = 0.95).
#'
#' @return A data frame containing the grouping column (\code{SimulationName} or \code{Cultivar}), 
#'   calculated physiological components (\code{LeafNumberMaximum}, \code{FLN}, \code{LeafNumberLimit}), 
#'   and three APSIM-X formatted date tracks.
#' @export
#'
#' @examples
#' \dontrun{
#' df_haun_dates <- derive_haun_pheno_dates(
#'   compiled_obs = list_observed_dfs, 
#'   max_leaf_limit = 0.95
#' )
#' }
derive_pheno_dates_from_haun <- function(compiled_obs, max_leaf_limit = 0.95) {
  
  # ---- 1. EXTRACT NESTED DATA SAFELY ----
  if (!all(c("df_name", "data") %in% names(compiled_obs))) {
    stop("Error [derive_haun_pheno_dates]: Input must be a nested tibble containing 'df_name' and 'data' blocks.")
  }
  
  haun_idx <- grep("haun", compiled_obs$df_name, ignore.case = TRUE)
  if (length(haun_idx) == 0) {
    stop("Error [derive_haun_pheno_dates]: No operational dataset with 'haun' located in 'df_name'.")
  }
  
  df_haun <- compiled_obs$data[[haun_idx[1]]]
  
  # ---- 2. DYNAMIC GROUPING CORRECTION ----
  req_cols <- c("Date", "Wheat.Phenology.HaunStage")
  missing_cols <- setdiff(req_cols, names(df_haun))
  if (length(missing_cols) > 0) {
    stop(sprintf("Error [derive_haun_pheno_dates]: Missing structural columns in source: %s", 
                 paste(missing_cols, collapse = ", ")))
  }
  
  grp_col <- intersect(c("SimulationName", "Cultivar"), names(df_haun))
  if (length(grp_col) == 0) {
    stop("Error [derive_haun_pheno_dates]: Neither 'SimulationName' nor 'Cultivar' keys found to anchor groupings.")
  }
  grp_col <- grp_col[1] # Bind strictly to the dominant header key
  
  # ---- 3. RUN INTERPOLATION ENGINE ----
  df_derived <- df_haun %>%
    dplyr::filter(!is.na(Date), !is.na(Wheat.Phenology.HaunStage)) %>%
    # Parse date strings explicitly into numeric tracking intervals
    dplyr::mutate(
      Date_Num = as.numeric(as.Date(Date)),
      Wheat.Phenology.HaunStage = as.numeric(Wheat.Phenology.HaunStage)
    ) %>%
    # Ensure rows are grouped and ordered monotonically by the predictor variable for approx()
    dplyr::arrange(.data[[grp_col]], Wheat.Phenology.HaunStage) %>%
    dplyr::group_by(.data[[grp_col]]) %>%
    dplyr::summarise(
      LeafNumberMaximum = max(Wheat.Phenology.HaunStage, na.rm = TRUE),
      FLN               = as.integer(round(LeafNumberMaximum)),
      LeafNumberLimit   = LeafNumberMaximum * max_leaf_limit,
      
      Haun_TS = FLN - 3,
      Haun_DR = max(2, FLN - 6),
      
      # Execute interpolations safely with monotonic vector safeguards
      Date_Num_Limit = if (dplyr::n() >= 2 && LeafNumberMaximum > 0) {
        approx(x = Wheat.Phenology.HaunStage, y = Date_Num, xout = LeafNumberLimit, rule = 2, ties = "mean")$y
      } else { NA_real_ },
      
      Date_Num_TS = if (dplyr::n() >= 2 && LeafNumberMaximum > 0) {
        approx(x = Wheat.Phenology.HaunStage, y = Date_Num, xout = Haun_TS, rule = 2, ties = "mean")$y
      } else { NA_real_ },
      
      Date_Num_DR = if (dplyr::n() >= 2 && LeafNumberMaximum > 0) {
        approx(x = Wheat.Phenology.HaunStage, y = Date_Num, xout = Haun_DR, rule = 2, ties = "mean")$y
      } else { NA_real_ },
      
      .groups = "drop"
    ) %>%
    # ---- 4. RECAST BACK TO CALENDAR DATES & ALIGN NAMES ----
  dplyr::mutate(
    Date = as.Date(Date_Num_Limit, origin = "1970-01-01"),
    `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress` = as.Date(Date_Num_TS, origin = "1970-01-01"),
    `[Wheat].Phenology.LeavesInitiating.DateToProgress`          = as.Date(Date_Num_DR, origin = "1970-01-01")
  ) %>%
    dplyr::select(
      dplyr::all_of(grp_col),
      LeafNumberMaximum, FLN, LeafNumberLimit,
      Date,
      `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress`,
      `[Wheat].Phenology.LeavesInitiating.DateToProgress`
    )
  
  # ---- 5. PIPELINE NOTIFICATION ----
  message(sprintf("Success [derive_haun_pheno_dates]: Mapped dates (Limit, TS, DR) for %d '%s' groups.", 
                  nrow(df_derived), grp_col))
  
  return(df_derived)
}