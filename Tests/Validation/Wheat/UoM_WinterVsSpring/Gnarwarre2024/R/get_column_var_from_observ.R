#' Extract Specific Variables with Standard APSIM Metadata
#'
#' @description
#' Extracts a specific variable from the observed data frame while always 
#' preserving core APSIM metadata columns (SimulationName, Clock.Today).
#' It safely extracts extra metadata (like Cultivar and ReleaseYear) ONLY 
#' if they exist in the dataframe, preventing crashes across different projects.
#'
#' @param df A data frame (e.g., file_obs_mean).
#' @param col_name String. The additional column to preserve.
#'
#' @return A data frame containing the core metadata columns, any existing 
#' optional metadata, plus the requested variable.
#' 
#' @importFrom dplyr select any_of all_of
#' @importFrom tidyr drop_na
#' @export
get_column_var_from_observ <- function(df, col_name) {
  
  require(dplyr)
  require(tidyr)
  
  # 1. Define strictly REQUIRED columns (Pipeline will crash if missing)
  required_cols <- c("SimulationName", "Clock.Today", col_name)
  
  missing_required <- setdiff(required_cols, colnames(df))
  if (length(missing_required) > 0) {
    stop(sprintf("CRITICAL: The following required columns are missing from the data: %s", 
                 paste(missing_required, collapse = ", ")))
  }
  
  # 2. Define OPTIONAL metadata columns (Nice to have, won't crash if missing)
  optional_cols <- c("Cultivar", "Wheat.SowingData.Cultivar", "ReleaseYear")
  
  # 3. Perform selection securely
  df_selected <- df %>%
    
    # Grab required columns strictly, and optional columns loosely
    dplyr::select(
      dplyr::all_of(required_cols), 
      dplyr::any_of(optional_cols)
    ) %>%
    
    # THE FIREWALL: Only drop the row if the SPECIFIC target variable is NA. 
    # (Prevents silent data loss if an optional column happens to be NA).
    tidyr::drop_na(dplyr::all_of(col_name))
  
  return(df_selected)
}