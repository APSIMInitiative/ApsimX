#' Read and Process Observed Excel Data for APSIM (Explicit Single-Key Version)
#'
#' @export
read_observed_one_key <- function(file_path, SheetName, VarName, NewVarName, UnitCorrect, unique_key) {
  
  require(dplyr)
  require(readxl)
  require(stringr)
  require(tidyr)
  
  file_id <- basename(file_path)
  possible_sheets <- trimws(unlist(strsplit(as.character(SheetName), "\\|")))
  
  # ==================================================================
  # 🚨 THE UNIVERSAL ALARM FUNCTION 🚨
  # ==================================================================
  print_missing_alarm <- function(issue_type, detail, target_sheet = possible_sheets[1]) {
    warning_box <- c(
      "",
      "======================================================================",
      sprintf(" \u26A0\uFE0F  DATA NOT FOUND: MISSING %s \u26A0\uFE0F", toupper(issue_type)),
      "======================================================================",
      sprintf(" File  : %s", file_id),
      sprintf(" Sheet : '%s'", target_sheet),
      sprintf(" Target: '%s' -> '%s'", VarName, NewVarName),
      sprintf(" Error : %s", detail),
      " Action: Skipping this extraction. The pipeline will continue.",
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
  }
  
  # 🛡️ THE INVISIBLE ASSASSIN FIX: Default to 1 if metadata corr_fact was left blank
  if (is.na(UnitCorrect) || is.null(UnitCorrect) || trimws(as.character(UnitCorrect)) == "") {
    UnitCorrect <- 1
  }
  
  # ------------------------------------------------------------------
  # 1. SHEET RESOLUTION
  # ------------------------------------------------------------------
  if (is.null(SheetName) || is.na(SheetName) || SheetName == "") {
    print_missing_alarm("SHEET NAME", "Invalid or empty sheet name requested in metadata.")
    return(NULL)
  }
  
  available_sheets <- readxl::excel_sheets(file_path)
  trimmed_available <- trimws(available_sheets)
  valid_sheets <- available_sheets[trimmed_available %in% possible_sheets]
  
  if (length(valid_sheets) == 0) {
    print_missing_alarm("SHEET", sprintf("None of the requested sheets ('%s') exist in this file.", SheetName))
    return(NULL)
  }
  
  active_sheet <- valid_sheets[1]
  
  # ------------------------------------------------------------------
  # 2. READ RAW DATA
  # ------------------------------------------------------------------
  df <- suppressMessages(readxl::read_excel(path = file_path, sheet = active_sheet, col_types = "text"))
  names(df) <- trimws(names(df)) 
  
  # ------------------------------------------------------------------
  # 3. DYNAMIC COLUMN DETECTION (Simplified for Keys)
  # ------------------------------------------------------------------
  date_col <- names(df)[grepl("date", names(df), ignore.case = TRUE)][1]
  
  # Find the unique key exactly (case-insensitive)
  key_match <- grep(paste0("^", unique_key, "$"), names(df), ignore.case = TRUE, value = TRUE)
  key_col <- if (length(key_match) > 0) key_match[1] else NA
  
  if (is.na(date_col) || is.na(key_col)) {
    missing_what <- ifelse(is.na(date_col), "Date", unique_key)
    msg <- sprintf("Missing '%s' column in the sheet.", missing_what)
    print_missing_alarm(missing_what, msg, active_sheet)
    return(NULL)
  }
  
  # Target Variable Detection
  actual_var_name <- VarName
  if (!actual_var_name %in% names(df)) {
    fuzzy_match <- grep(paste0("^", VarName, "$"), names(df), ignore.case = TRUE, value = TRUE)
    if (length(fuzzy_match) == 1) {
      actual_var_name <- fuzzy_match[1]
    } else {
      print_missing_alarm("VARIABLE", sprintf("Column '%s' was not found in the sheet.", VarName), active_sheet)
      return(NULL)
    }
  }
  
  # ------------------------------------------------------------------
  # 4. SWISS CHEESE DATE PARSER
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
      for (fmt in c("%d/%m/%y", "%d/%m/%Y", "%Y-%m-%d", "%Y/%m/%d")) {
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
  # 5. STANDARDIZATION & CLEANING
  # ------------------------------------------------------------------
  df_mapped <- df %>%
    dplyr::mutate(
      !!unique_key := trimws(as.character(.data[[key_col]])),
      Date = parse_any_date(.data[[date_col]]),
      !!NewVarName := suppressWarnings(as.numeric(stringr::str_replace_all(.data[[actual_var_name]], ",", ".")))
    )
  
  if (all(is.na(df_mapped[[NewVarName]]))) {
    warning(sprintf("CRITICAL in '%s': All values in '%s' evaluated to NA. Check raw data.", file_id, actual_var_name), call. = FALSE)
  }
  
  # ------------------------------------------------------------------
  # 6. SAFETY AGGREGATION (Plot Level)
  # ------------------------------------------------------------------
  # We group by the unique key and Date. If there are accidental duplicate 
  # measurements for the exact same Plot on the exact same Date, this safely averages them.
  df_clean <- df_mapped %>%
    dplyr::filter(!is.na(.data[[unique_key]]) & .data[[unique_key]] != "") %>%
    dplyr::group_by(.data[[unique_key]], Date) %>%
    dplyr::summarise(
      dplyr::across(
        dplyr::all_of(NewVarName),
        ~ replace(mean(.x, na.rm = TRUE) * UnitCorrect, is.nan(mean(.x, na.rm = TRUE)), NA)
      ),
      .groups = "drop"
    ) %>%
    tidyr::drop_na(dplyr::all_of(NewVarName))
  
  return(df_clean)
}