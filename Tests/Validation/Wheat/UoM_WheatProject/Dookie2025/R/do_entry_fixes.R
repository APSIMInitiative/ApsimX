#' Sanitize and Fix Raw Observation Dates
#'
#' @description
#' Scans a compiled list of raw observation dataframes for date entry errors.
#' Specifically checks for dates that fall before the project's reference date 
#' (e.g., mistyping 2024 instead of 2025) and automatically corrects their year 
#' to match the reference year.
#'
#' @details
#' **Dynamic Identifier Targeting:** At this early stage in the pipeline, 
#' `SimulationName` might not yet be attached (depending on upstream join order). 
#' This function dynamically searches for `SimulationName`, and gracefully falls 
#' back to `Cultivar` as the warning identifier if needed.
#'
#' @param compiled_obs A nested tibble containing `df_name` and a `data` list-column 
#'   (the output of `compile_all_observed`).
#' @param date_DOY_ref Character. The reference start date from config 
#'   (e.g., "01-01-2025" or "2025-01-01").
#'
#' @return The cleaned nested tibble with corrected dates.
#'
#' @importFrom dplyr mutate
#' @importFrom purrr map2
#' @importFrom lubridate dmy ymd year
#' @export
do_entry_fixes <- function(compiled_obs, date_DOY_ref) {
  
  require(dplyr)
  require(purrr)
  require(lubridate)
  
  # 1. Parse the reference date safely (handles both DD-MM-YYYY and YYYY-MM-DD)
  ref_date <- suppressWarnings(lubridate::dmy(date_DOY_ref))
  if (is.na(ref_date)) {
    ref_date <- suppressWarnings(lubridate::ymd(date_DOY_ref))
  }
  if (is.na(ref_date)) {
    stop(sprintf("CRITICAL: Could not parse date_DOY_ref ('%s'). Ensure format is DD-MM-YYYY.", date_DOY_ref))
  }
  
  target_year <- lubridate::year(ref_date)
  
  # External list to capture warnings across all purrr iterations
  warning_logs <- c()
  
  # 2. Iterate through the nested dataframes
  res <- compiled_obs %>%
    dplyr::mutate(
      data = purrr::map2(data, df_name, function(df, name) {
        
        # Skip if no Date column exists
        if (!"Date" %in% names(df)) return(df)
        
        # Safely ensure Date is a true Date object
        df$Date <- suppressWarnings(as.Date(df$Date))
        
        # Identify rows where the Date is chronologically before the reference date
        bad_idx <- which(!is.na(df$Date) & df$Date < ref_date)
        
        if (length(bad_idx) > 0) {
          
          # Find the best identifier column for the warning message
          id_col <- rep("Unknown_ID", nrow(df))
          if ("SimulationName" %in% names(df)) {
            id_col <- df$SimulationName
          } else if ("Cultivar" %in% names(df)) {
            id_col <- df$Cultivar
          }
          
          # Apply the fix row-by-row to build the exact warning strings
          for (i in bad_idx) {
            raw_date <- df$Date[i]
            
            # Create the fixed date by injecting the target year
            fixed_date <- raw_date
            lubridate::year(fixed_date) <- target_year
            
            # Apply fix back to dataframe
            df$Date[i] <- fixed_date
            
            # Append to the global log
            warning_logs <<- c(
              warning_logs,
              sprintf(" -> [%s] ID: '%s' | RAW: %s | FIXED: %s", 
                      name, id_col[i], as.character(raw_date), as.character(fixed_date))
            )
          }
        }
        
        return(df)
      })
    )
  
  # 3. Print MASSIVE warning if fixes were applied
  if (length(warning_logs) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " ⚠️  CRITICAL WARNING: RAW DATE TYPOS AUTO-CORRECTED ⚠️ ",
      "======================================================================",
      sprintf(" The following dates fell before the reference date (%s)", as.character(ref_date)),
      sprintf(" and had their year automatically forced to %d:", target_year),
      warning_logs,
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    
    # Trigger native warning so targets flags it in tar_meta()
    warning("Raw date typos were auto-corrected. See console for exact mapping.", call. = FALSE)
  }
  
  return(res)
}