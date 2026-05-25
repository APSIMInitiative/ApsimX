#' Calculate and Append Harvest Index (Universal Engine)
#'
#' @description
#' Safely calculates the Harvest Index (HI) from Grain Weight and Total Above-Ground 
#' Biomass (AGB), appending it as a new column. It strictly aligns the calculation 
#' row-by-row, ensuring HI only populates when both biological components are present.
#'
#' @details
#' **Zero-Division Protection:** The function evaluates the denominator (AGB) before 
#' calculation. If AGB is zero, NA, or negative, the function safely bypasses the math 
#' and inserts an NA, preventing pipeline crashes or Inf values.
#'
#' @param df Data frame. The continuous observation timeline.
#' @param grain_col Character. The name of the grain weight column (default: "Wheat.Grain.Wt").
#' @param agb_col Character. The name of the total biomass column (default: "Wheat.AboveGround.Wt").
#' @param hi_col_name Character. The desired name for the output column (default: "HarvestIndex").
#'
#' @return A data frame with the new Harvest Index column appended.
#' @export
calc_harvest_index <- function(df, grain_col = "Wheat.Grain.Wt", agb_col = "Wheat.AboveGround.Wt", hi_col_name = "HarvestIndex") {
  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df) || !is.data.frame(df) || nrow(df) == 0) {
    stop("Error [calc_harvest_index]: Main observation dataframe is missing or empty.")
  }
  
  if (!all(c(grain_col, agb_col) %in% names(df))) {
    stop(sprintf(
      "Error [calc_harvest_index]: Required biological columns '%s' or '%s' not found in dataframe.", 
      grain_col, agb_col
    ))
  }
  
  # ---- 2. SECURE ROW-BY-ROW CALCULATION ----
  df_out <- df %>%
    dplyr::mutate(
      !!hi_col_name := dplyr::if_else(
        # CONDITION: AGB must exist and be strictly greater than 0. Grain must exist.
        !is.na(.data[[agb_col]]) & .data[[agb_col]] > 0 & !is.na(.data[[grain_col]]),
        
        # TRUE: Calculate HI
        .data[[grain_col]] / .data[[agb_col]],
        
        # FALSE: Safely pad with NA
        NA_real_
      )
    )
  
  # ---- 3. BIOLOGICAL PLAUSIBILITY WARNING ----
  # HI should never be > 1.0 (Grain cannot weigh more than the whole plant)
  if (any(df_out[[hi_col_name]] > 1, na.rm = TRUE)) {
    warning(sprintf(
      "QC WARNING [calc_harvest_index]: Found Harvest Index > 1.0. Check '%s' and '%s' for data entry errors.", 
      grain_col, agb_col
    ), call. = FALSE)
  }
  
  # ---- 4. COMPLETION NOTIFICATION ----
  valid_hi_count <- sum(!is.na(df_out[[hi_col_name]]))
  message(sprintf("Success [calc_harvest_index]: Calculated %d Harvest Index values under column '%s'.", 
                  valid_hi_count, hi_col_name))
  
  return(df_out)
}