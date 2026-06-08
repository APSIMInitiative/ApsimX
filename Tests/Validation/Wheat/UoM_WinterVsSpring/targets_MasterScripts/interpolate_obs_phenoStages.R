#' Interpolates PCDS variables across Date on a daily basis.
#'
#' @param df_list_PCDS A named list of data frames, where each data frame
#'                     contains at least 'SimulationName', 'Cultivar', 'Date', 
#'                     and result variables with names like '...PCDS...'.
#' @return A named list of data frames (df_interp_list) with daily interpolated 
#'         dates for each PCDS variable, grouped by SimulationName and Cultivar.
#'         
#' @importFrom dplyr arrange group_by mutate ungroup filter n
#' @importFrom tidyr complete
#' @importFrom rlang .data `:=`
#' @export
interpolate_obs_phenoStages <- function(df_list_PCDS) {
  
  # Load required packages
  require(dplyr)
  require(purrr)
  require(tidyr)
  require(rlang)
  
  df_interp_list <- list()
  all_names <- names(df_list_PCDS)
  
  for (nm in all_names) {
    
    pcds_df <- df_list_PCDS[[nm]]
    
    # ------------------------------------------------------------------
    # 1. DEFENSIVE CHECKS
    # ------------------------------------------------------------------
    if (!all(c("SimulationName", "Date") %in% names(pcds_df))) {
      stop(sprintf("CRITICAL: Dataframe '%s' is missing 'SimulationName', or 'Date'.", nm))
    }
    
    cat("====", nm, "====\n")
    print(head(pcds_df))
    
    # Identify the variable column dynamically
    result_col <- names(pcds_df)[grepl("DateToProgress", names(pcds_df))]
    
    if (length(result_col) != 1) {
      stop(sprintf("CRITICAL: Expected exactly one column matching 'DateToProgress' in '%s', but found %d.", nm, length(result_col)))
    }
    
    result_col <- result_col[[1]]
    
    # ------------------------------------------------------------------
    # 2. GROUP AND INTERPOLATE
    # ------------------------------------------------------------------
    # isolate each df for daily interpolation
    df_interp <- pcds_df %>%
      # Add SimulationName to the hierarchy
      dplyr::arrange(SimulationName, Date) %>%
      dplyr::group_by(SimulationName) %>%
      tidyr::complete(
        Date = seq(min(Date, na.rm = TRUE),
                   max(Date, na.rm = TRUE),
                   by = "day")
      ) %>%
      dplyr::mutate(
        !!result_col := {
          
          # Isolate valid data points for this specific simulation/cultivar group
          valid_idx <- !is.na(.data[[result_col]])
          x_vals <- Date[valid_idx]
          y_vals <- .data[[result_col]][valid_idx]
          
          # approx() strictly requires at least 2 points to interpolate
          if (length(x_vals) >= 2) {
            approx(
              x = x_vals,
              y = y_vals,
              xout = Date,
              rule = 2 # rule = 2 extends the extreme values to avoid NAs at the edges
            )$y
          } else {
            # Gracefully fill with NA if interpolation is impossible for this group
            rep(NA_real_, dplyr::n())
          }
        }
      ) %>%
      dplyr::ungroup()
    
    # ---- store as list element ----
    df_interp_list[[nm]] <- df_interp
    
  }
  
  return(df_interp_list)
}