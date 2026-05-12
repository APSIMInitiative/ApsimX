#' Read and Process Observed Excel Data for APSIM
#'
#' @export
read_observed_func <- function(file_path, SheetName, VarName, NewVarName, UnitCorrect, df_simNames) {
  
  file_id <- basename(file_path)
  
  # ---- 1. SANITIZE CSV MAPPING TABLE ----
  # Strip sneaky spaces from the CSV headers just in case it imported as "SowTime "
  names(df_simNames) <- trimws(names(df_simNames))
  
  # Firewall: If the CSV doesn't have these exact names, crash and tell us what it DOES have.
  req_csv_cols <- c("Cultivar", "SowTime", "SimulationName")
  if (!all(req_csv_cols %in% names(df_simNames))) {
    stop(sprintf("CRITICAL: The mapping CSV is missing required columns. Found headers: [%s]", 
                 paste(names(df_simNames), collapse = "], [")))
  }
  
  # Pre-clean the mapping table so we don't have to do it inside the join
  df_simNames_clean <- df_simNames %>%
    dplyr::mutate(
      Cultivar = trimws(as.character(Cultivar)),
      SowTime  = trimws(as.character(SowTime))
    )
  
  # ---- 2. SHEET RESOLUTION ----
  if (is.null(SheetName) || is.na(SheetName) || SheetName == "") {
    stop(sprintf("CRITICAL in file '%s': Invalid or empty sheet name requested.", file_id))
  }
  
  possible_sheets <- trimws(unlist(strsplit(as.character(SheetName), "\\|")))
  available_sheets <- readxl::excel_sheets(file_path)
  valid_sheets <- possible_sheets[possible_sheets %in% available_sheets]
  
  if (length(valid_sheets) == 0) {
    stop(sprintf("CRITICAL in file '%s': Requested sheets not found. Available: %s", 
                 file_id, paste(available_sheets, collapse = ", ")))
  }
  
  # ---- 3. READ RAW DATA (SILENCED) ----
  df <- suppressMessages(readxl::read_excel(path = file_path, sheet = valid_sheets[1], col_types = "text"))
  names(df) <- trimws(names(df)) # Whitespace sanitizer for Excel headers
  
  # ---- 4. DYNAMIC COLUMN DETECTION ----
  date_col <- names(df)[grepl("date", names(df), ignore.case = TRUE)][1]
  sow_col  <- names(df)[grepl("sow.*time", names(df), ignore.case = TRUE)][1]
  cult_col <- names(df)[grepl("cultivar|line", names(df), ignore.case = TRUE)][1]
  
  if (any(is.na(c(date_col, sow_col, cult_col)))) {
    stop(sprintf("CRITICAL in '%s': Missing core columns (Date, Sow Time, or Cultivar).", file_id))
  }
  
  parse_any_date <- function(x) {
    suppressWarnings({
      nums <- suppressWarnings(as.numeric(x))
      if (!all(is.na(nums))) { out <- as.Date(nums, origin = "1899-12-30"); if (!all(is.na(out))) return(out) }
      for (fmt in c("%d/%m/%y", "%d/%m/%Y", "%Y-%m-%d")) { out <- suppressWarnings(as.Date(x, format = fmt)); if (!all(is.na(out))) return(out) }
      suppressWarnings(as.Date(x))
    })
  }
  
  # ---- 5. STANDARDIZATION & MAPPING PIPELINE ----
  df_mapped <- df %>%
    dplyr::mutate(
      
      # The pristine SowTime extraction happens in one step
      SowTime = trimws(as.character(stringr::str_replace(
        string = .data[[sow_col]],
        pattern = stringr::regex("\\b(early|mid|late)[\\s_]+([a-z]+)", ignore_case = TRUE),
        replacement = function(m) {
          prefix <- tolower(stringr::str_extract(m, "(?i)early|mid|late"))
          month  <- stringr::str_to_title(stringr::str_extract(m, "(?i)[a-z]+$"))
          paste0(prefix, "-", month)
        }
      ))),
      
      Cultivar  = trimws(as.character(.data[[cult_col]])),
      Date      = parse_any_date(.data[[date_col]]),
      
      !!NewVarName := as.numeric(.data[[VarName]])
    ) %>%
    
    # Clean, safe join using our pre-cleaned mapping table
    dplyr::left_join(df_simNames_clean, by = c("Cultivar", "SowTime"))
  
  # ---- 6. ORPHAN FIREWALL ----
  orphans <- df_mapped %>% dplyr::filter(is.na(SimulationName))
  if (nrow(orphans) > 0) {
    bad_keys <- unique(paste(orphans$Cultivar, orphans$SowTime, sep = "|"))
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