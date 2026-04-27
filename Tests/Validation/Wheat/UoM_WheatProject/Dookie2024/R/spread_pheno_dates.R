#' Spread Phenology Data to Wide Format
#'
#' @description
#' Validates the presence of required columns, formats dates to the strict APSIM 
#' dd-mmm-yyyy standard, and pivots the parameters to a wide format.
#'
#' @param df Dataframe. The long-format interpolated phenology data.
#'
#' @return A wide-format dataframe ready for APSIM parameterization.
#'
#' @importFrom dplyr select mutate
#' @importFrom tidyr pivot_wider
#' @export
spread_pheno_dates <- function(df) {
  
  require(dplyr)
  require(tidyr)
  
  # ------------------------------------------------------------------
  # 1. DEFENSIVE CHECKS
  # ------------------------------------------------------------------
  if (!is.data.frame(df)) stop("CRITICAL: Input 'df' must be a dataframe.")
  
  req_cols <- c("SimulationName", "ParamName", "Clock.Today")
  missing_cols <- setdiff(req_cols, names(df))
  
  if (length(missing_cols) > 0) {
    stop(sprintf("CRITICAL: Missing required columns: %s", paste(missing_cols, collapse = ", ")))
  }
  
  # ------------------------------------------------------------------
  # 2. DATE FORMATTING & PIVOTING
  # ------------------------------------------------------------------
  df_wide <- df %>%
    # Isolate the exact three columns needed
    dplyr::select(SimulationName, ParamName, Clock.Today) %>%
    
    # Ensure Clock.Today is safely treated as a Date before formatting 
    dplyr::mutate(
      Clock.Today = as.Date(Clock.Today),
      Clock.Today = format(Clock.Today, "%d-%b-%Y")
    ) %>%
    
    # Pivot wider so ParamNames become the column headers
    tidyr::pivot_wider(
      names_from = ParamName,
      values_from = Clock.Today
    )
  
  message(sprintf("Successfully spread phenology data across %d simulations.", nrow(df_wide)))
  
  return(df_wide)
}