#' Impute or Exclude Missing Phenology Dates
#'
#' @description
#' Scans the final wide APSIM phenology input matrix for missing dates (NAs).
#' - Tier 1: If an entire column is missing, it drops the column so APSIM can natively simulate it.
#' - Tier 2: If a column is partially missing, it imputes the gaps using group-aware averages.
#'
#' @param df Wide dataframe of APSIM phenology inputs (SimulationName + DateToProgress columns).
#' @param group_keys Character vector of strings to group by (e.g., c("EVA", "WWHI")).
#' @return The dataframe formatted safely for APSIM.
#' @export
do_averages_for_missing_pheno <- function(df, group_keys) {
  
  if (!requireNamespace("lubridate", quietly = TRUE)) stop("Package 'lubridate' required.")
  
  stage_cols <- setdiff(names(df), "SimulationName")
  imputation_logs <- c()
  dropped_logs <- c()
  
  for (col in stage_cols) {
    # Find rows with NA or blank strings
    na_idx <- which(is.na(df[[col]]) | df[[col]] == "")
    
    # ==========================================================
    # TIER 1: ENTIRE COLUMN MISSING (The Surgeon)
    # ==========================================================
    if (length(na_idx) == nrow(df)) {
      
      # Remove the column entirely from the dataframe
      df[[col]] <- NULL
      dropped_logs <- c(dropped_logs, sprintf(" -> EXCLUDED: '%s'", col))
      
      # ==========================================================
      # TIER 2: PARTIALLY MISSING (The Group Imputer)
      # ==========================================================
    } else if (length(na_idx) > 0) {
      
      for (i in na_idx) {
        current_sim <- df$SimulationName[i]
        
        # 1. Identify which group this simulation belongs to
        matched_group <- NULL
        for (key in group_keys) {
          if (grepl(key, current_sim, ignore.case = TRUE)) {
            matched_group <- key
            break
          }
        }
        
        # 2. Subset all rows for that specific group
        if (!is.null(matched_group)) {
          group_rows <- grep(matched_group, df$SimulationName, ignore.case = TRUE)
        } else {
          group_rows <- 1:nrow(df) # Fallback to global average
        }
        
        # 3. Extract the valid dates for this column within the group
        raw_dates <- df[[col]][group_rows]
        valid_dates_str <- raw_dates[!is.na(raw_dates) & raw_dates != ""]
        
        if (length(valid_dates_str) > 0) {
          # Parse, average, and re-format
          valid_dates <- suppressWarnings(lubridate::parse_date_time(valid_dates_str, orders = c("dmy", "ymd", "Ymd")))
          valid_dates <- as.Date(valid_dates[!is.na(valid_dates)])
          
          if (length(valid_dates) > 0) {
            avg_date <- as.Date(round(mean(as.numeric(valid_dates))), origin = "1970-01-01")
            formatted_avg <- format(avg_date, "%d-%m-%Y")
            
            df[[col]][i] <- formatted_avg
            
            group_label <- ifelse(is.null(matched_group), "GLOBAL FALLBACK", matched_group)
            imputation_logs <- c(
              imputation_logs, 
              sprintf(" -> IMPUTED: [%s] filled '%s' with %s (Group: %s)", current_sim, col, formatted_avg, group_label)
            )
          }
        }
      }
    }
  }
  
  # ==========================================================
  # REPORTING
  # ==========================================================
  if (length(dropped_logs) > 0 || length(imputation_logs) > 0) {
    message("\n=== PHENOLOGY MATRIX ADJUSTMENTS ===")
    
    if (length(dropped_logs) > 0) {
      message("\n[1] PARAMETERS FULLY EXCLUDED (Lacking Data):")
      message(paste(dropped_logs, collapse = "\n"))
      message("    * APSIM will safely use its internal thermal-time engine for these stages.")
    }
    
    if (length(imputation_logs) > 0) {
      message("\n[2] MISSING DATES IMPUTED (Group Averaged):")
      message(paste(imputation_logs, collapse = "\n"))
    }
    message("====================================\n")
  }
  
  return(df)
}