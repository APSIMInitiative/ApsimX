#' Sanitize and Fix Raw Observation Dates
#'
#' @description
#' Scans a compiled list of raw observation dataframes for date entry errors.
#' Performs a three-stage correction:
#' 1. Syntax Fixes: Cleans human-entry typos (e.g., "dd/mm//yyyy").
#' 2. Year Fixes: Corrects dates falling before the reference date.
#' 3. NA Imputation: Replaces missing dates with the variable's average date.
#'
#' @param compiled_obs A nested tibble containing `df_name` and a `data` list-column.
#' @param date_DOY_ref Character. The reference start date (e.g., "01-01-2025").
#'
#' @return The cleaned nested tibble with corrected dates.
#'
#' @export
do_entry_fixes <- function(compiled_obs, date_DOY_ref) {
  
  require(dplyr)
  require(purrr)
  require(lubridate)
  
  # 1. Parse the reference date safely
  ref_date <- suppressWarnings(lubridate::dmy(date_DOY_ref))
  if (is.na(ref_date)) {
    ref_date <- suppressWarnings(lubridate::ymd(date_DOY_ref))
  }
  if (is.na(ref_date)) {
    stop(sprintf("CRITICAL: Could not parse date_DOY_ref ('%s'). Ensure format is DD-MM-YYYY.", date_DOY_ref))
  }
  
  target_year <- lubridate::year(ref_date)
  warning_logs <- c()
  
  # 2. Iterate through the nested dataframes
  res <- compiled_obs %>%
    dplyr::mutate(
      data = purrr::map2(data, df_name, function(df, name) {
        
        # Skip if no Date column exists
        if (!"Date" %in% names(df)) return(df)
        
        # Find the best identifier column for the warning message
        id_col <- rep("Unknown_ID", nrow(df))
        if ("SimulationName" %in% names(df)) {
          id_col <- df$SimulationName
        } else if ("Cultivar" %in% names(df)) {
          id_col <- df$Cultivar
        }
        
        # ==========================================================
        # STAGE 1: STRING SYNTAX FIXES (e.g., dd/mm//yyyy)
        # ==========================================================
        if (is.character(df$Date)) {
          bad_str_idx <- grep("/{2,}|-{2,}|\\\\+", df$Date)
          
          if (length(bad_str_idx) > 0) {
            for (i in bad_str_idx) {
              raw_str <- df$Date[i]
              fixed_str <- gsub("/{2,}|-{2,}|\\\\+", "/", raw_str)
              df$Date[i]  <- fixed_str
              
              warning_logs <<- c(
                warning_logs,
                sprintf(" -> [%s] ID: '%s' | SYNTAX FIX | RAW: '%s' | FIXED: '%s'", 
                        name, id_col[i], raw_str, fixed_str)
              )
            }
          }
        }
        
        # Safely convert to Date object
        if (!inherits(df$Date, "Date")) {
          parsed_dates <- suppressWarnings(lubridate::parse_date_time(df$Date, orders = c("dmy", "ymd", "mdy")))
          df$Date <- as.Date(parsed_dates)
        }
        
        # ==========================================================
        # STAGE 2: CHRONOLOGICAL YEAR FIXES 
        # ==========================================================
        bad_idx <- which(!is.na(df$Date) & df$Date < ref_date)
        
        if (length(bad_idx) > 0) {
          for (i in bad_idx) {
            raw_date <- df$Date[i]
            
            fixed_date <- raw_date
            lubridate::year(fixed_date) <- target_year
            df$Date[i] <- fixed_date
            
            warning_logs <<- c(
              warning_logs,
              sprintf(" -> [%s] ID: '%s' | YEAR FIX   | RAW: %s | FIXED: %s", 
                      name, id_col[i], as.character(raw_date), as.character(fixed_date))
            )
          }
        }
        
        # ==========================================================
        # STAGE 3: MISSING DATE IMPUTATION (The Agronomy Rescue)
        # ==========================================================
        na_idx <- which(is.na(df$Date))
        
        if (length(na_idx) > 0) {
          # Find all valid dates in this specific dataframe to calculate the mean
          valid_dates <- df$Date[!is.na(df$Date)]
          
          if (length(valid_dates) > 0) {
            # Convert to numeric to get the true mean, then convert back to Date
            avg_date <- as.Date(round(mean(as.numeric(valid_dates))), origin = "1970-01-01")
            
            for (i in na_idx) {
              df$Date[i] <- avg_date
              warning_logs <<- c(
                warning_logs,
                sprintf(" -> [%s] ID: '%s' | MISSING FIX | RAW: NA | FIXED: %s (Average Injected)", 
                        name, id_col[i], as.character(avg_date))
              )
            }
          } else {
            # Edge case: If the entire sheet has zero dates, we can't calculate an average.
            warning_logs <<- c(
              warning_logs,
              sprintf(" -> [%s] ID: '%s' | FAILED FIX | RAW: NA | Cannot inject average (All dates missing)", 
                      name, id_col[i])
            )
          }
        }
        
        return(df)
      })
    )
  
  # 3. Print MASSIVE warning if ANY fixes were applied
  if (length(warning_logs) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " ⚠️  CRITICAL WARNING: RAW DATE TYPOS AUTO-CORRECTED ⚠️ ",
      "======================================================================",
      " The following bad date entries were intercepted and automatically fixed:",
      warning_logs,
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    warning("Raw date typos were auto-corrected. See console for exact mapping.", call. = FALSE)
  }
  
  return(res)
}