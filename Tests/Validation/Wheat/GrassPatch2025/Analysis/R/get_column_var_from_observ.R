#' Extract Specific Variables with Standard APSIM Metadata
#'
#' @description
#' Extracts a specific variable from the observed data frame while always 
#' preserving the "standard" APSIM metadata columns: SimulationName, 
#' Clock.Today, Cultivar, and ReleaseYear.
#'
#' @param df A data frame (e.g., file_obs_mean).
#' @param col_name String. The additional column to preserve.
#'
#' @return A data frame containing the four standard metadata columns 
#' plus the requested variable.
#' 
#' @export
get_column_var_from_observ <- function(df, col_name) {
  
  # 1. Define the standard metadata columns
  standard_cols <- c("SimulationName", "Clock.Today", "Wheat.SowingData.Cultivar", "ReleaseYear")
  
  # 2. Combine with the requested column and check for existence
  target_cols <- unique(c(standard_cols, col_name))
  missing_cols <- setdiff(target_cols, colnames(df))
  
  if (length(missing_cols) > 0) {
    stop(paste("The following columns were not found in the data frame:", 
               paste(missing_cols, collapse = ", ")))
  }
  
  # 3. Perform selection
  df_selected <- df %>%
    dplyr::select(dplyr::all_of(target_cols))%>%
    na.omit()
  
  return(df_selected)
}