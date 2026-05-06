# R/functions.R

#' Interpolates PCDS variables across Date and estimates the date for 50% (0.50)
#' of each variable for each Cultivar.
#'
#' @param df_list_PCDS A named list of data frames, where each data frame
#'                     contains at least 'Cultivar', 'Date', and result variables
#'                     with names like '...PCDS...'.
#' @return A single data frame (df_50_interp) listing the date of 50% achievement
#'         for each PCDS variable and Cultivar.
#'         
#'  df_list_PCDS <- tar_read(df_list_PCDS)
#'  PercTarg <- tar_read(PercTarg)       
interpolate_obs_phenoStages <- function(df_list_PCDS) {
  
  # Load required packages
  require(dplyr)
  require(purrr)
  require(tidyr)
  
  # Use purrr::map_dfr to iterate over the list and row-bind the results
  
  df_interp_list <- list()
  
  all_names <- names(df_list_PCDS)
  
  for (nm in all_names) {
    
    pcds_df <- df_list_PCDS[[nm]]
    
    cat("====", nm, "====\n")
    print(head(pcds_df))
    
    # Identify the variable column
    result_col <- names(pcds_df)[grepl("DateToProgress", names(pcds_df))]
    
    if (length(result_col) != 1) {
      stop("Expected exactly one column matching ", value_pattern)
    }
    
  result_col <- result_col[[1]]
    
    # isolate each df for daily interpolation
  df_interp <-  pcds_df %>%
      arrange(Cultivar, Date) %>%
      group_by(Cultivar) %>%
      tidyr::complete(
        Date = seq(min(Date, na.rm = TRUE),
                   max(Date, na.rm = TRUE),
                   by = "day")
      ) %>%
      mutate(
        !!result_col := approx(
          x = Date[!is.na(.data[[result_col]])],
          y = .data[[result_col]][!is.na(.data[[result_col]])],
          xout = Date,
          rule = 2
        )$y
      ) %>%
      ungroup()
  
  # ---- store as list element ----
  df_interp_list[[nm]] <- df_interp
    
  }
  
  return(df_interp_list)
  
}