#' Sanitize and Fix Raw Observation Dates (Dookie Context)
#'
#' @description
#' Scans a compiled list of raw observation dataframes for date entry errors.
#' Specifically checks for dates that fall before the project's reference date 
#' (e.g., mistyping the previous year) and automatically corrects their year 
#' to match the reference year.
#'
#' @param df_tbl A nested tibble containing `df_name` and a `data` list-column 
#'   (the output of `compile_all_observed`).
#' @param ref_date Character. The reference start date from config 
#'   (e.g., "01-01-2024").
#'
#' @return The cleaned nested tibble with corrected dates.
#'
#' @importFrom dplyr mutate
#' @importFrom purrr map2
#' @importFrom lubridate dmy ymd year
#' @export
apply_corrections_Dookie24 <- function(df_tbl, ref_date) {
  
  # 1. Parse the reference date safely (handles both DD-MM-YYYY and YYYY-MM-DD)
  parsed_ref_date <- suppressWarnings(lubridate::dmy(ref_date))
  if (is.na(parsed_ref_date)) {
    parsed_ref_date <- suppressWarnings(lubridate::ymd(ref_date))
  }
  if (is.na(parsed_ref_date)) {
    stop(sprintf("CRITICAL: Could not parse ref_date ('%s'). Ensure format is DD-MM-YYYY.", ref_date))
  }
  
  target_year <- lubridate::year(parsed_ref_date)
  
  # External list to capture warnings across all purrr iterations
  warning_logs <- c()
  
  # 2. Iterate through the nested dataframes
  res <- df_tbl %>%
    dplyr::mutate(
      data = purrr::map2(data, df_name, function(df, name) {
        
        # Skip if no Date column exists
        if (!"Date" %in% names(df)) return(df)
        
        # Safely ensure Date is a true Date object
        df$Date <- suppressWarnings(as.Date(df$Date))
        
        # Identify rows where the Date is chronologically before the reference date
        bad_idx <- which(!is.na(df$Date) & df$Date < parsed_ref_date)
        
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
            
            # Append to the global log (using <<- to modify the outer variable)
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
      sprintf(" The following dates fell before the reference date (%s)", as.character(parsed_ref_date)),
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