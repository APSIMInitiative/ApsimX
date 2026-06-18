#' Find Interpolated Date of Maximum Leaf Appearance
#'
#' @description
#' Scans a nested observation dataframe for a specific "haun" dataset. It calculates 
#' the maximum observed Haun stage per group, applies a fractional limit (e.g., 0.95), 
#' and linearly interpolates the exact Date that limit was reached.
#'
#' @details
#' **Nested Structure:** This function expects a nested tibble input containing 
#' `df_name` (character) and `data` (list-column of dataframes).
#' **Dynamic Grouping:** Automatically groups by `SimulationName` if present, 
#' falling back to `Cultivar`.
#' **Interpolation:** Uses base R `approx()` to interpolate the exact date based on 
#' the progression of the Haun stage. It handles ties safely (e.g., if two 
#' consecutive days have the exact same Haun reading).
#'
#' @param compiled_obs A nested dataframe containing `df_name` and `data` columns.
#' @param max_leaf_limit Numeric. The fractional threshold of the maximum leaf number to target (default = 0.95).
#'
#' @return A data.frame containing the grouping variable, the calculated `LeafNumberMaximum`, 
#'   `LeafNumberLimit`, and the interpolated `Date`.
#'
#' @importFrom dplyr filter arrange group_by summarise n
#' @importFrom rlang .data
#' @export
find_date_max_leaf <- function(compiled_obs, max_leaf_limit = 0.95) {
  
  require(dplyr)
  require(rlang)
  
  # ------------------------------------------------------------------
  # 1. EXTRACT TARGET DATAFRAME FROM NESTED TIBBLE
  # ------------------------------------------------------------------
  # Defensive check for the expected nested structure
  if (!all(c("df_name", "data") %in% names(compiled_obs))) {
    stop("CRITICAL: Input must be a nested dataframe containing 'df_name' and 'data' columns.")
  }
  
  # Search the df_name column for "haun" (case-insensitive)
  haun_idx <- grep("haun", compiled_obs$df_name, ignore.case = TRUE)
  
  if (length(haun_idx) == 0) {
    stop("CRITICAL: No row with 'haun' in 'df_name' was found.")
  }
  if (length(haun_idx) > 1) {
    warning(sprintf("Multiple rows matched 'haun'. Using the first match: '%s'", 
                    compiled_obs$df_name[haun_idx[1]]))
  }
  
  # Plunge into the list-column to extract the actual observation dataframe
  df_haun <- compiled_obs$data[[haun_idx[1]]]
  
  if (!is.data.frame(df_haun)) {
    stop("CRITICAL: The extracted 'haun' element is not a valid data frame.")
  }
  
  # ------------------------------------------------------------------
  # 2. VALIDATE COLUMNS AND IDENTIFIER
  # ------------------------------------------------------------------
  req_cols <- c("Date", "Wheat.Phenology.HaunStage")
  missing_cols <- setdiff(req_cols, names(df_haun))
  
  if (length(missing_cols) > 0) {
    stop(sprintf("CRITICAL: The 'haun' dataframe is missing required columns: %s", 
                 paste(missing_cols, collapse = ", ")))
  }
  
  # Dynamically determine the grouping column
  if ("SimulationName" %in% names(df_haun)) {
    grp_col <- "SimulationName"
  } else if ("Cultivar" %in% names(df_haun)) {
    grp_col <- "Cultivar"
  } else {
    stop("CRITICAL: Neither 'SimulationName' nor 'Cultivar' were found to group by.")
  }
  
  # ------------------------------------------------------------------
  # 3. CALCULATE AND INTERPOLATE
  # ------------------------------------------------------------------
  df_max_leaf <- df_haun %>%
    dplyr::filter(!is.na(Date), !is.na(Wheat.Phenology.HaunStage)) %>%
    dplyr::arrange(.data[[grp_col]], Date) %>%
    dplyr::group_by(.data[[grp_col]]) %>%
    dplyr::summarise(
      LeafNumberMaximum = max(Wheat.Phenology.HaunStage, na.rm = TRUE),
      LeafNumberLimit   = max(Wheat.Phenology.HaunStage, na.rm = TRUE) * max_leaf_limit,
      
      Date = {
        if (n() >= 2 && max(Wheat.Phenology.HaunStage, na.rm = TRUE) > 0) {
          
          res <- approx(
            x = Wheat.Phenology.HaunStage, 
            y = as.numeric(Date), 
            xout = max(Wheat.Phenology.HaunStage, na.rm = TRUE) * max_leaf_limit,
            rule = 2,           
            ties = "mean"       
          )$y
          
          as.Date(res, origin = "1970-01-01")
          
        } else {
          as.Date(NA)
        }
      },
      .groups = "drop"
    )
  
  # ------------------------------------------------------------------
  # 4. CONSOLE NOTIFICATION
  # ------------------------------------------------------------------
  message(sprintf("Successfully calculated max leaf dates for %d '%s' groups at a %.0f%% limit.", 
                  nrow(df_max_leaf), grp_col, max_leaf_limit * 100))
  
  return(df_max_leaf)
}