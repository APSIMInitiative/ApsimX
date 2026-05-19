#' Interpolates PCDS variables across Date and estimates the date for 50% (0.50)
#' of each variable for each Cultivar.
#'
#' @param df_list_PCDS A named list of data frames, where each data frame
#'                     contains at least 'SimulationName', 'Cultivar', 'Date', 
#'                     and result variables with names like '...PCDS...'.
#' @return A single data frame (df_50_interp) listing the date of 50% achievement
#'         for each PCDS variable, SimulationName, and Cultivar.
#'         
#' @importFrom dplyr arrange group_by mutate ungroup cur_group n filter select contains
#' @importFrom tidyr complete
#' @importFrom lubridate year
#' @importFrom purrr walk2
#' @export
interpolate_obs_phenoStages <- function(df_list_PCDS) {
  
  # Load required packages
  require(dplyr)
  require(purrr)
  require(tidyr)
  require(lubridate)
  
  df_interp_list <- list()
  all_names <- names(df_list_PCDS)
  
  # Set the target year for global correction
  target_year <- 2025
  
  for (nm in all_names) {
    
    pcds_df <- df_list_PCDS[[nm]]
    
    # Optional but recommended: defensive check to ensure SimulationName exists
    if (!"SimulationName" %in% names(pcds_df)) {
      stop(sprintf("CRITICAL: 'SimulationName' column is missing from list element '%s'", nm))
    }
    
    # ------------------------------------------------------------------
    # NEW: DATE TYPO CORRECTION & WARNING GENERATION
    # ------------------------------------------------------------------
    # Identify any rows where the year is explicitly wrong (ignoring NAs)
    bad_dates <- pcds_df %>%
      dplyr::filter(!is.na(Date) & lubridate::year(Date) != target_year)
    
    if (nrow(bad_dates) > 0) {
      
      # Loop through the bad dates and throw a targeted warning for each
      purrr::walk2(bad_dates$SimulationName, bad_dates$Date, ~{
        
        raw_date <- .y
        fixed_date <- .y
        lubridate::year(fixed_date) <- target_year
        
        warning(sprintf(
          "DATE CORRECTION | SimulationName '%s' had a correction in date format/value RAW: %s and FIXED: %s",
          .x, as.character(raw_date), as.character(fixed_date)
        ), call. = FALSE)
      })
      
      # Force the entire Date column to the target year, preserving month/day
      lubridate::year(pcds_df$Date) <- target_year
    }
    # ------------------------------------------------------------------
    
    cat("====", nm, "====\n")
    print(head(pcds_df))
    
    # Identify the variable column
    result_col <- names(pcds_df)[grepl("DateToProgress", names(pcds_df))]
    
    if (length(result_col) != 1) {
      stop(sprintf("Expected exactly one column matching 'DateToProgress' in '%s', but found %d.", nm, length(result_col)))
    }
    
    result_col <- result_col[[1]]
    
    # isolate each df for daily interpolation
    df_interp <- pcds_df %>%
      # 1. Add SimulationName to the sorting and grouping hierarchy
      arrange(SimulationName, Date) %>%
      group_by(SimulationName) %>%
      tidyr::complete(
        Date = seq(min(Date, na.rm = TRUE),
                   max(Date, na.rm = TRUE),
                   by = "day")
      ) %>%
      mutate(
        !!result_col := {
          
          # Extract the valid (non-NA) dates and values for this specific group
          valid_idx <- !is.na(.data[[result_col]])
          x_vals <- Date[valid_idx]
          y_vals <- .data[[result_col]][valid_idx]
          
          # 2. SAFE INTERPOLATION CHECK
          if (length(x_vals) >= 2) {
            
            # If we have 2 or more points, it is safe to interpolate
            approx(x = x_vals, y = y_vals, xout = Date, rule = 2)$y
            
          } else {
            
            # 3. CONTEXTUAL WARNING
            sim_name <- cur_group()$SimulationName
            cult_name <- cur_group()$Cultivar
            
            warning(
              sprintf(
                "Skipping interpolation for '%s' | Simulation: '%s' | Cultivar: '%s'. Reason: Less than 2 valid data points available.",
                nm, sim_name, cult_name
              ),
              call. = FALSE
            )
            
            # Return a vector of NAs to fill the column for this group without crashing
            rep(NA_real_, n())
          }
        }
      ) %>%
      ungroup()
    
    df_interp <- df_interp %>%
      dplyr::select(SimulationName, Date, dplyr::contains("[Wheat].Phenology"))
    
    # ---- store as list element ----
    df_interp_list[[nm]] <- df_interp
    
  }
  
  return(df_interp_list)
}