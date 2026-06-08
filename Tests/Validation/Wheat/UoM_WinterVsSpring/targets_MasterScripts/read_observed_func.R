#' Read and Process Observed Excel Data for APSIM (Universal Master)
#'
#' @export
read_observed_func <- function(file_path, SheetName, VarName, NewVarName, UnitCorrect) {
  
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
      sprintf(" ⚠️  DATA NOT FOUND: MISSING %s ⚠️", toupper(issue_type)),
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
  # 1. SHEET RESOLUTION (THE FORGIVING MATCHER)
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
  # 3. DYNAMIC COLUMN DETECTION 
  # ------------------------------------------------------------------
  date_col <- names(df)[grepl("date", names(df), ignore.case = TRUE)][1]
  cult_col <- names(df)[grepl("cultivar|line", names(df), ignore.case = TRUE)][1]
  sow_col  <- names(df)[grepl("sow.*time", names(df), ignore.case = TRUE)][1]
  
  # ------------------------------------------------------------------
  # 3.1 THE CULTIVAR RESCUE OPERATION (Design Sheet Linkage)
  # ------------------------------------------------------------------
  needs_rescue <- is.na(cult_col) || any(is.na(df[[cult_col]]) | trimws(as.character(df[[cult_col]])) == "")
  
  if (needs_rescue) {
    plot_col <- names(df)[grepl("plot", names(df), ignore.case = TRUE)][1]
    design_sheet <- available_sheets[tolower(trimws(available_sheets)) == "design"][1]
    
    if (!is.na(design_sheet) && !is.na(plot_col)) {
      df_design <- suppressMessages(readxl::read_excel(path = file_path, sheet = design_sheet, col_types = "text"))
      names(df_design) <- trimws(names(df_design))
      
      des_cult_col <- names(df_design)[grepl("cultivar|line", names(df_design), ignore.case = TRUE)][1]
      des_plot_col <- names(df_design)[grepl("plot", names(df_design), ignore.case = TRUE)][1]
      
      if (!is.na(des_cult_col) && !is.na(des_plot_col)) {
        map_dict <- df_design %>%
          dplyr::select(Plot_ID = dplyr::all_of(des_plot_col), Rescue_Cult = dplyr::all_of(des_cult_col)) %>%
          dplyr::mutate(Plot_ID = trimws(as.character(Plot_ID)), Rescue_Cult = trimws(as.character(Rescue_Cult))) %>%
          dplyr::filter(!is.na(Plot_ID) & Plot_ID != "" & !is.na(Rescue_Cult) & Rescue_Cult != "") %>%
          dplyr::distinct(Plot_ID, .keep_all = TRUE)
        
        df <- df %>%
          dplyr::mutate(Temp_Plot_Join = trimws(as.character(.data[[plot_col]]))) %>%
          dplyr::left_join(map_dict, by = c("Temp_Plot_Join" = "Plot_ID"))
        
        if (is.na(cult_col)) {
          df$Cultivar <- df$Rescue_Cult
          cult_col <- "Cultivar"
        } else {
          df <- df %>%
            dplyr::mutate(!!cult_col := dplyr::coalesce(
              dplyr::na_if(trimws(as.character(.data[[cult_col]])), ""),
              Rescue_Cult
            ))
        }
        df <- df %>% dplyr::select(-Temp_Plot_Join, -Rescue_Cult)
      }
    }
  }
  
  # ------------------------------------------------------------------
  # 3.2 THE FINAL VERIFICATION FIREWALL
  # ------------------------------------------------------------------
  if (is.na(date_col) || is.na(cult_col) || all(is.na(df[[cult_col]]) | trimws(as.character(df[[cult_col]])) == "")) {
    missing_what <- ifelse(is.na(date_col), "Date", "Cultivar")
    msg <- ifelse(missing_what == "Cultivar", 
                  "No reference to Cultivar name was found in Design sheet or other Sheets.", 
                  "Missing Date column.")
    
    print_missing_alarm(missing_what, msg, active_sheet)
    return(NULL)
  }
  
  # ------------------------------------------------------------------
  # 3.5 TARGET VARIABLE RESOLVER (MULTI-COLUMN & FUZZY MATCHER)
  # ------------------------------------------------------------------
  possible_vars <- trimws(unlist(strsplit(as.character(VarName), "\\|")))
  actual_var_name <- NA_character_
  
  for (p_var in possible_vars) {
    # 1. Try Exact Match
    if (p_var %in% names(df)) {
      actual_var_name <- p_var
      break
    }
    
    # 2. Try Case-Insensitive Exact Match Fallback
    fuzzy_match <- grep(paste0("^", p_var, "$"), names(df), ignore.case = TRUE, value = TRUE)
    if (length(fuzzy_match) > 0) {
      actual_var_name <- fuzzy_match[1]
      break
    }
  }
  
  if (is.na(actual_var_name)) {
    print_missing_alarm("VARIABLE", sprintf("None of the requested columns ('%s') were found in the sheet.", VarName), active_sheet)
    return(NULL)
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
  # 5. STANDARDIZATION PIPELINE
  # ------------------------------------------------------------------
  df_mapped <- df %>%
    dplyr::mutate(
      Cultivar  = trimws(as.character(.data[[cult_col]])),
      Date      = parse_any_date(.data[[date_col]]),
      !!NewVarName := suppressWarnings(as.numeric(stringr::str_replace_all(.data[[actual_var_name]], ",", ".")))
    )
  
  if (all(is.na(df_mapped[[NewVarName]]))) {
    warning(sprintf("CRITICAL in '%s': All values in '%s' evaluated to NA. Check raw data.", file_id, actual_var_name), call. = FALSE)
  }
  
  grouping_vars <- c("Cultivar", "Date")
  
  if (!is.na(sow_col)) {
    df_mapped <- df_mapped %>%
      dplyr::mutate(
        SowTime = trimws(as.character(stringr::str_replace(
          string = .data[[sow_col]],
          pattern = stringr::regex("\\b(early|mid|late)[\\s_]+([a-z]+)", ignore_case = TRUE),
          replacement = function(m) {
            prefix <- tolower(stringr::str_extract(m, "(?i)early|mid|late"))
            month  <- stringr::str_to_title(stringr::str_extract(m, "(?i)[a-z]+$"))
            paste0(prefix, "-", month)
          }
        )))
      )
    grouping_vars <- c("Cultivar", "SowTime", "Date")
  }
  
  # ------------------------------------------------------------------
  # 6. COLLAPSE TO TREATMENT MEANS
  # ------------------------------------------------------------------
  df_clean <- df_mapped %>%
    dplyr::filter(!is.na(Cultivar) & Cultivar != "") %>% 
    dplyr::group_by(dplyr::across(dplyr::all_of(grouping_vars))) %>%
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