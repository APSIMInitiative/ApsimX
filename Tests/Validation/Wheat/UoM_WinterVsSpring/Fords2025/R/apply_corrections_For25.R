#' Apply Specific Corrections for Fords 2025 (For25)
#'
#' @param df_tbl The compiled list of observed dataframes (output of Phase C)
#' @param folder_path The directory where dates_to_correct.csv should be stored
#' @param ref_date A reference date (like sowing date) to enforce the correct year
#' @export
apply_corrections_For25 <- function(df_tbl, folder_path, ref_date) {
  
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  if (!requireNamespace("purrr", quietly = TRUE)) stop("Package 'purrr' required.")
  
  cat("\n======================================================================\n")
  cat(" \U0001F6E0\U000FE0F  PHASE C.2: APPLYING FOR25-SPECIFIC CORRECTIONS \n")
  cat("======================================================================\n")
  
  csv_path <- file.path(folder_path, "dates_to_correct.csv")
  
  # ------------------------------------------------------------------
  # 0. THE SWISS CHEESE DATE PARSER (Global to this function)
  # ------------------------------------------------------------------
  parse_any_date <- function(x) {
    final_dates <- as.Date(rep(NA_character_, length(x)))
    
    nums <- suppressWarnings(as.numeric(x))
    num_idx <- which(!is.na(nums))
    if (length(num_idx) > 0) {
      final_dates[num_idx] <- as.Date(nums[num_idx], origin = "1899-12-30")
    }
    
    rem_idx <- which(is.na(final_dates) & !is.na(x) & trimws(as.character(x)) != "")
    if (length(rem_idx) > 0) {
      x_rem <- as.character(x[rem_idx])
      for (fmt in c("%d/%m/%Y", "%d-%m-%Y", "%d/%m/%y", "%d-%m-%y", "%Y-%m-%d", "%Y/%m/%d", "%m/%d/%Y")) {
        temp_dates <- suppressWarnings(as.Date(x_rem, format = fmt))
        success_idx <- which(!is.na(temp_dates))
        if (length(success_idx) > 0) {
          final_dates[rem_idx[success_idx]] <- temp_dates[success_idx]
          x_rem <- x_rem[-success_idx]       
          rem_idx <- rem_idx[-success_idx]   
        }
        if (length(rem_idx) == 0) break
      }
    }
    return(final_dates)
  }
  
  # ------------------------------------------------------------------
  # 0.5 SAFE TARGET YEAR EXTRACTION
  # ------------------------------------------------------------------
  # Check if the user just passed a pure 4-digit year (e.g., 2025 or "2025")
  if (suppressWarnings(!is.na(as.numeric(ref_date))) && nchar(trimws(as.character(ref_date))) == 4) {
    target_year <- as.numeric(ref_date)
  } else {
    # Otherwise, parse the complex date string
    safe_ref <- parse_any_date(as.character(ref_date))
    if (any(is.na(safe_ref))) {
      stop(sprintf("CRITICAL ERROR: 'ref_date' (%s) could not be parsed into a valid date.", ref_date))
    }
    target_year <- as.numeric(format(safe_ref[1], "%Y"))
  }
  
  cat(sprintf("   [\U0001F4C5 REFERENCE] Target Year securely locked as: %d\n", target_year))
  
  # ------------------------------------------------------------------
  # 1. THE PRE-AUDIT: Who is actually missing dates?
  # ------------------------------------------------------------------
  missing_summary <- df_tbl %>%
    dplyr::mutate(missing_count = purrr::map_int(data, ~sum(is.na(.x$Date)))) %>%
    dplyr::filter(missing_count > 0)
  
  missing_list <- if(nrow(missing_summary) > 0) paste(missing_summary$df_name, collapse = "\n -> ") else ""
  
  # ------------------------------------------------------------------
  # 1.5 THE AUDIT LOG
  # ------------------------------------------------------------------
  if (nrow(missing_summary) > 0) {
    purrr::walk2(df_tbl$df_name, df_tbl$data, function(name_val, raw_df) {
      if (is.null(raw_df) || nrow(raw_df) == 0) return()
      missing_count <- sum(is.na(raw_df$Date))
      
      if (missing_count > 0) {
        target_vars <- setdiff(names(raw_df), c("SimulationName", "Date", "Plot", "Exp_key_name"))
        warning_box <- c(
          "",
          "----------------------------------------------------------------------",
          sprintf(" \u26A0\uFE0F  MISSING DATE ALARM: '%s' \u26A0\uFE0F", name_val),
          "----------------------------------------------------------------------",
          sprintf(" -> Variable(s)    : [%s]", paste(target_vars, collapse = ", ")),
          sprintf(" -> Missing Dates  : %d rows found without a valid Date!", missing_count),
          "----------------------------------------------------------------------"
        )
        cat(paste(warning_box, collapse = "\n"), "\n")
      }
    })
  }
  
  # ------------------------------------------------------------------
  # 2. TEMPLATE GENERATION & HALT
  # ------------------------------------------------------------------
  if (nrow(missing_summary) > 0 && !file.exists(csv_path)) {
    template <- data.frame(df_name = missing_summary$df_name, new_date = "")
    write.csv(template, csv_path, row.names = FALSE)
    
    stop_msg <- c(
      "",
      "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
      " \u26A0\uFE0F FATAL ALARM: PIPELINE HALTED FOR MISSING DATES \u26A0\uFE0F",
      "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
      " The pipeline cannot proceed because the datasets listed above lack valid dates.",
      "",
      sprintf(" ACTION: A template has been auto-populated at: \n %s", csv_path),
      " Open it, provide a 'new_date' (YYYY-MM-DD or DD/MM/YYYY) for each row, and save.",
      " Then run targets::tar_make() again.",
      "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!",
      ""
    )
    stop(paste(stop_msg, collapse = "\n"), call. = FALSE)
  }
  
  # ------------------------------------------------------------------
  # 3. READ & VALIDATE USER CORRECTIONS
  # ------------------------------------------------------------------
  if (nrow(missing_summary) > 0) {
    corrections <- read.csv(csv_path, stringsAsFactors = FALSE)
    
    if (!all(c("df_name", "new_date") %in% names(corrections))) {
      stop("\n🚨 CRITICAL ERROR: 'dates_to_correct.csv' must contain exactly two columns: 'df_name' and 'new_date'.", call. = FALSE)
    }
    
    corrections <- corrections %>% dplyr::filter(trimws(df_name) != "" & trimws(new_date) != "")
    
    if (nrow(corrections) == 0) {
      stop(sprintf("\n🚨 CRITICAL ERROR: 'dates_to_correct.csv' has no dates filled in!\n -> Please provide the dates for the following dataframes and run again:\n -> %s\n\n File: %s", missing_list, csv_path), call. = FALSE)
    }
    
    corrections$parsed_date <- parse_any_date(corrections$new_date)
    
    if (any(is.na(corrections$parsed_date))) {
      bad_dates <- corrections$new_date[is.na(corrections$parsed_date)]
      stop(sprintf("\n🚨 CRITICAL ERROR: Could not understand the date format in CSV.\n -> Found unreadable entries: [%s]\n -> Please use DD/MM/YYYY or YYYY-MM-DD.", 
                   paste(bad_dates, collapse=", ")), call. = FALSE)
    }
  } else {
    corrections <- data.frame(df_name = character(), parsed_date = as.Date(character()))
  }
  
  # ------------------------------------------------------------------
  # 4. SURGICAL INJECTION
  # ------------------------------------------------------------------
  df_corrected <- df_tbl %>%
    dplyr::mutate(
      data = purrr::pmap(list(df_name, data), function(name_val, raw_df) {
        if (name_val %in% corrections$df_name) {
          fix_date <- corrections$parsed_date[corrections$df_name == name_val][1]
          na_count <- sum(is.na(raw_df$Date))
          
          raw_df <- raw_df %>% dplyr::mutate(Date = dplyr::if_else(is.na(Date), fix_date, Date))
          cat(sprintf("   [\u2714\uFE0F FIXED NAs] '%s' | Original: NA (%d rows) -> Corrected: %s\n", 
                      name_val, na_count, fix_date))
        }
        return(raw_df)
      })
    )
  
  # ------------------------------------------------------------------
  # 4.5 BULLETPROOF YEAR SWAP RESCUE
  # ------------------------------------------------------------------
  df_corrected <- df_corrected %>%
    dplyr::mutate(
      data = purrr::pmap(list(df_name, data), function(name_val, raw_df) {
        
        if (is.null(raw_df) || nrow(raw_df) == 0) return(raw_df)
        
        current_years <- as.numeric(format(raw_df$Date, "%Y"))
        mismatch_idx <- which(!is.na(current_years) & current_years != target_year)
        
        if (length(mismatch_idx) > 0) {
          affected_sims <- unique(raw_df$SimulationName[mismatch_idx])
          original_dates_disp <- paste(unique(as.character(raw_df$Date[mismatch_idx])), collapse = ", ")
          
          # Forcefully rebuild the date string using the correct target_year
          new_dates_str <- paste(target_year, format(raw_df$Date[mismatch_idx], "%m-%d"), sep="-")
          parsed_new_dates <- as.Date(new_dates_str)
          
          # Fallback: If swapping the year created an invalid date (e.g., Feb 29 on a non-leap year)
          # safely roll it back to Feb 28 to prevent NA crashes.
          if (any(is.na(parsed_new_dates))) {
            parsed_new_dates[is.na(parsed_new_dates)] <- as.Date(paste(target_year, "02-28", sep="-"))
          }
          
          raw_df$Date[mismatch_idx] <- parsed_new_dates
          
          corrected_dates_disp <- paste(unique(as.character(raw_df$Date[mismatch_idx])), collapse = ", ")
          
          cat(sprintf("   [\U0001F504 YEAR SWAP] '%s' | Original: [%s] -> Corrected: [%s]\n      -> Affected Sims: %s\n", 
                      name_val, original_dates_disp, corrected_dates_disp, 
                      paste(head(affected_sims, 5), collapse = ", ")))
        }
        return(raw_df)
      })
    )
  
  # ------------------------------------------------------------------
  # 5. THE POST-AUDIT FIREWALL
  # ------------------------------------------------------------------
  still_missing <- df_corrected %>%
    dplyr::mutate(missing_count = purrr::map_int(data, ~sum(is.na(.x$Date)))) %>%
    dplyr::filter(missing_count > 0)
  
  if (nrow(still_missing) > 0) {
    still_missing_list <- paste(still_missing$df_name, collapse = "\n -> ")
    stop(sprintf("\n🚨 CRITICAL ERROR: There are STILL missing dates after applying your CSV corrections!\n -> Please add these missing dataframes to your CSV:\n -> %s", still_missing_list), call. = FALSE)
  }
  
  cat("======================================================================\n\n")
  return(df_corrected)
}