#' Read and Process Observed Excel Data for APSIM (Universal Master)
#'
#' @export
read_observed_func <- function(file_path, SheetName, VarName, NewVarName, UnitCorrect) {
  
  require(dplyr)
  require(readxl)
  require(stringr)
  require(tidyr)
  
  file_id <- basename(file_path)
  
  # 🛡️ THE INVISIBLE ASSASSIN FIX: Default to 1 if metadata corr_fact was left blank
  if (is.na(UnitCorrect) || is.null(UnitCorrect) || trimws(as.character(UnitCorrect)) == "") {
    UnitCorrect <- 1
  }
  
  # ------------------------------------------------------------------
  # 1. SHEET RESOLUTION (THE FORGIVING MATCHER)
  # ------------------------------------------------------------------
  if (is.null(SheetName) || is.na(SheetName) || SheetName == "") {
    warning(sprintf("Invalid or empty sheet name requested for file '%s'. Skipping.", file_id), call. = FALSE)
    return(NULL)
  }
  
  possible_sheets <- trimws(unlist(strsplit(as.character(SheetName), "\\|")))
  available_sheets <- readxl::excel_sheets(file_path)
  
  trimmed_available <- trimws(available_sheets)
  valid_sheets <- available_sheets[trimmed_available %in% possible_sheets]
  
  if (length(valid_sheets) == 0) {
    warning(sprintf("Skipped missing sheet '%s' in %s", possible_sheets[1], file_id), call. = FALSE)
    return(NULL)
  }
  
  # ------------------------------------------------------------------
  # 2. READ RAW DATA
  # ------------------------------------------------------------------
  df <- suppressMessages(readxl::read_excel(path = file_path, sheet = valid_sheets[1], col_types = "text"))
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
  # Check if Cultivar column is missing, or if ANY of its rows are blank
  needs_rescue <- is.na(cult_col) || any(is.na(df[[cult_col]]) | trimws(as.character(df[[cult_col]])) == "")
  
  if (needs_rescue) {
    plot_col <- names(df)[grepl("plot", names(df), ignore.case = TRUE)][1]
    design_sheet <- available_sheets[tolower(trimws(available_sheets)) == "design"][1]
    
    if (!is.na(design_sheet) && !is.na(plot_col)) {
      # Read Design sheet silently
      df_design <- suppressMessages(readxl::read_excel(path = file_path, sheet = design_sheet, col_types = "text"))
      names(df_design) <- trimws(names(df_design))
      
      des_cult_col <- names(df_design)[grepl("cultivar|line", names(df_design), ignore.case = TRUE)][1]
      des_plot_col <- names(df_design)[grepl("plot", names(df_design), ignore.case = TRUE)][1]
      
      if (!is.na(des_cult_col) && !is.na(des_plot_col)) {
        # Build a clean mapping dictionary from the Design sheet
        map_dict <- df_design %>%
          dplyr::select(Plot_ID = dplyr::all_of(des_plot_col), Rescue_Cult = dplyr::all_of(des_cult_col)) %>%
          dplyr::mutate(Plot_ID = trimws(as.character(Plot_ID)), Rescue_Cult = trimws(as.character(Rescue_Cult))) %>%
          dplyr::filter(!is.na(Plot_ID) & Plot_ID != "" & !is.na(Rescue_Cult) & Rescue_Cult != "") %>%
          dplyr::distinct(Plot_ID, .keep_all = TRUE)
        
        # Join to our main dataframe to patch the holes
        df <- df %>%
          dplyr::mutate(Temp_Plot_Join = trimws(as.character(.data[[plot_col]]))) %>%
          dplyr::left_join(map_dict, by = c("Temp_Plot_Join" = "Plot_ID"))
        
        if (is.na(cult_col)) {
          # If the Cultivar column didn't exist at all, create it from the rescue data
          df$Cultivar <- df$Rescue_Cult
          cult_col <- "Cultivar"
        } else {
          # If it existed but had holes, patch the holes using coalesce
          df <- df %>%
            dplyr::mutate(!!cult_col := dplyr::coalesce(
              dplyr::na_if(trimws(as.character(.data[[cult_col]])), ""),
              Rescue_Cult
            ))
        }
        # Clean up temporary columns
        df <- df %>% dplyr::select(-Temp_Plot_Join, -Rescue_Cult)
      }
    }
  }
  
  # ------------------------------------------------------------------
  # 3.2 THE FINAL VERIFICATION FIREWALL
  # ------------------------------------------------------------------
  # If it failed the rescue, or if Date is completely missing, abort gracefully.
  if (is.na(date_col) || is.na(cult_col) || all(is.na(df[[cult_col]]) | trimws(as.character(df[[cult_col]])) == "")) {
    
    missing_what <- ifelse(is.na(date_col), "Date", "Cultivar")
    msg <- ifelse(missing_what == "Cultivar", 
                  "No reference to Cultivar name was found in Design sheet or other Sheets.", 
                  "Missing Date column.")
    
    warning_box <- c(
      "",
      "======================================================================",
      sprintf(" \u26A0\uFE0F DATA NOT FOUND: MISSING %s \u26A0\uFE0F", toupper(missing_what)),
      "======================================================================",
      sprintf(" File: %s | Sheet: %s", file_id, valid_sheets[1]),
      sprintf(" Error: %s", msg),
      " Action: Skipping this sheet. The pipeline will continue.",
      "======================================================================",
      ""
    )
    message(paste(warning_box, collapse = "\n"))
    warning(sprintf("Skipped '%s' in %s: %s", valid_sheets[1], file_id, msg), call. = FALSE)
    return(NULL)
  }
  
  # ------------------------------------------------------------------
  # 3.5 MISSING TARGET VARIABLE
  # ------------------------------------------------------------------
  actual_var_name <- VarName
  if (!actual_var_name %in% names(df)) {
    fuzzy_match <- grep(paste0("^", VarName, "$"), names(df), ignore.case = TRUE, value = TRUE)
    if (length(fuzzy_match) == 1) {
      actual_var_name <- fuzzy_match[1]
    } else {
      warning(sprintf("Skipped '%s' in %s: Column '%s' not found.", valid_sheets[1], file_id, VarName), call. = FALSE)
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