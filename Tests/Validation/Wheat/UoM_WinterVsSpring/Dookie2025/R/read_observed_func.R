#' Read and Process Observed Excel Data for APSIM
#'
#' @export
read_observed_func <- function(file_path, SheetName, VarName, NewVarName, UnitCorrect, df_simNames) {
  
  file_id <- basename(file_path)
  
  # ---- 1. SANITIZE & DYNAMICALLY DETECT CSV MAPPING ----
  names(df_simNames) <- trimws(names(df_simNames))
  
  # Check if SowTime is available in the mapping CSV
  has_csv_sow <- "SowTime" %in% names(df_simNames)
  
  req_csv_cols <- c("Cultivar", "SimulationName")
  if (!all(req_csv_cols %in% names(df_simNames))) {
    stop(sprintf("CRITICAL: The mapping CSV is missing required columns. Found headers: [%s]", 
                 paste(names(df_simNames), collapse = "], [")))
  }
  
  # Pre-clean the mapping table dynamically
  df_simNames_clean <- df_simNames %>%
    dplyr::mutate(Cultivar = trimws(as.character(Cultivar)))
  
  if (has_csv_sow) {
    df_simNames_clean <- df_simNames_clean %>%
      dplyr::mutate(SowTime = trimws(as.character(SowTime)))
  }
  
  # ---- 2. SHEET RESOLUTION (FORGIVING MATCHER) ----
  if (is.null(SheetName) || is.na(SheetName) || SheetName == "") {
    stop(sprintf("CRITICAL in file '%s': Invalid or empty sheet name requested.", file_id))
  }
  
  possible_sheets <- trimws(unlist(strsplit(as.character(SheetName), "\\|")))
  available_sheets <- readxl::excel_sheets(file_path)
  
  # 🛡️ THE FORGIVING MATCHER 🛡️
  # Trim the Excel sheet names in memory just to do a fair comparison
  trimmed_available <- trimws(available_sheets)
  
  # Find the actual, raw Excel strings that match our cleaned requested names
  valid_sheets <- available_sheets[trimmed_available %in% possible_sheets]
  
  if (length(valid_sheets) == 0) {
    stop(sprintf(
      "CRITICAL in file '%s': Requested sheet(s) [%s] not found. \nAvailable sheets: [%s]", 
      file_id, 
      paste(possible_sheets, collapse = "] | ["),
      paste(available_sheets, collapse = "], [")
    ))
  }
  
  # Optional: Print a polite note to the console when it fixes a typo
  if (valid_sheets[1] != possible_sheets[1]) {
    message(sprintf("   -> Notice: Corrected sloppy sheet name in Excel. Requested [%s] but read [%s]", 
                    possible_sheets[1], valid_sheets[1]))
  }
  
  # ---- 3. READ RAW DATA (SILENCED) ----
  df <- suppressMessages(readxl::read_excel(path = file_path, sheet = valid_sheets[1], col_types = "text"))
  names(df) <- trimws(names(df)) 
  
  # ---- 4. DYNAMIC COLUMN DETECTION ----
  date_col <- names(df)[grepl("date", names(df), ignore.case = TRUE)][1]
  cult_col <- names(df)[grepl("cultivar|line", names(df), ignore.case = TRUE)][1]
  sow_col  <- names(df)[grepl("sow.*time", names(df), ignore.case = TRUE)][1]
  
  if (any(is.na(c(date_col, cult_col)))) {
    stop(sprintf("CRITICAL in '%s': Missing core columns (Date or Cultivar).", file_id))
  }
  
  # 🛡️ THE UPGRADED DATE PARSER: Prevents the `bind_rows` logical crash 🛡️
  parse_any_date <- function(x) {
    # If the column is completely empty, return a true Date-class NA vector
    # This stops the 'logical vs numeric/Date' crash downstream
    if (all(is.na(x))) {
      return(as.Date(rep(NA_character_, length(x))))
    }
    
    suppressWarnings({
      nums <- suppressWarnings(as.numeric(x))
      if (!all(is.na(nums))) { out <- as.Date(nums, origin = "1899-12-30"); if (!all(is.na(out))) return(out) }
      for (fmt in c("%d/%m/%y", "%d/%m/%Y", "%Y-%m-%d")) { out <- suppressWarnings(as.Date(x, format = fmt)); if (!all(is.na(out))) return(out) }
      suppressWarnings(as.Date(as.character(x)))
    })
  }
  
  # ---- 5. STANDARDIZATION & MAPPING PIPELINE ----
  df_mapped <- df %>%
    dplyr::mutate(
      Cultivar  = trimws(as.character(.data[[cult_col]])),
      Date      = parse_any_date(.data[[date_col]]),
      !!NewVarName := as.numeric(.data[[VarName]])
    )
  
  # Only attempt to extract SowTime if the column actually exists in the Excel file
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
  }
  
  # Establish dynamic join keys
  join_keys <- "Cultivar"
  if (has_csv_sow && !is.na(sow_col)) {
    join_keys <- c("Cultivar", "SowTime")
  }
  
  df_mapped <- df_mapped %>%
    dplyr::left_join(df_simNames_clean, by = join_keys)
  
  # ---- 6. ORPHAN FIREWALL ----
  orphans <- df_mapped %>% dplyr::filter(is.na(SimulationName))
  if (nrow(orphans) > 0) {
    # Dynamically build the error string based on available columns
    if ("SowTime" %in% names(orphans)) {
      bad_keys <- unique(paste(orphans$Cultivar, orphans$SowTime, sep = "|"))
    } else {
      bad_keys <- unique(orphans$Cultivar)
    }
    
    if (length(bad_keys) > 4) bad_keys <- c(bad_keys[1:4], sprintf("...and %d more", length(bad_keys) - 4))
    warning(sprintf("Orphans dropped in '%s': %d rows. Unmatched keys: %s", 
                    file_id, nrow(orphans), paste(bad_keys, collapse = ", ")), call. = FALSE)
  }
  
  # ---- 7. COLLAPSE TO TREATMENT MEANS ----
  df_clean <- df_mapped %>%
    dplyr::filter(!is.na(SimulationName)) %>% 
    dplyr::group_by(SimulationName, Date) %>%
    dplyr::summarise(
      dplyr::across(
        dplyr::all_of(NewVarName),
        ~ replace(mean(.x, na.rm = TRUE) * UnitCorrect,
                  is.nan(mean(.x, na.rm = TRUE)), NA)
      ),
      .groups = "drop"
    ) %>%
    tidyr::drop_na(dplyr::all_of(NewVarName))
  
  return(df_clean)
}