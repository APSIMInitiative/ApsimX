#' Read and Process Observed Excel Data for APSIM
#'
#' @description
#' Reads raw experimental observation data from an Excel sheet, dynamically extracts 
#' and formats dates, standardizes cultivar column names, applies a unit correction 
#' factor, and calculates the mean value across experimental blocks.
#'
#' @details
#' **Dataset & Column Detection:** The function automatically tags the output with 
#' "WWHI" or "EVA" based on the input filename. It also uses regex pattern matching 
#' to dynamically find columns representing "Date" and "Sow Time" (catching 
#' inconsistencies like `!Sow_Time` or `!Sow time`).
#'
#' **Date Parsing:** Excel frequently corrupts date formats by converting them to 
#' numeric serial values or mixed string formats. This function includes an internal 
#' helper (`parse_any_date`) that safely tests for Excel serial numbers (origin 1899-12-30) 
#' and multiple standard string formats to guarantee robust date extraction.
#' 
#' **Standardization:** The function safely handles legacy column naming conventions 
#' by checking for either `!Cultivar` or `!Line` and standardizing it to `Cultivar`. 
#' Finally, it aggregates the data, computing the mean of the specified variable 
#' across all blocks for each unique `Dataset`, `SowTime`, `Date`, and `Cultivar`.
#'
#' @param file_path Character. The full path to the Excel file.
#' @param SheetName Character. The exact name (or pipe-separated options) of the sheet to read.
#' @param VarName Character. The name of the column in the Excel file containing the raw data to extract.
#' @param NewVarName Character. The target name for the extracted variable (typically an APSIM-formatted name).
#' @param UnitCorrect Numeric. A multiplier applied to the aggregated mean to correct units (e.g., converting g/m2 to kg/ha).
#'
#' @return A tidy tibble containing `Dataset`, `SowTime`, `Date`, `Cultivar`, `Block`, 
#'   and the aggregated numeric variable defined by `NewVarName`. Rows with `NA` are omitted.
#'
#' @importFrom readxl read_excel excel_sheets
#' @importFrom dplyr rename any_of mutate select group_by summarise across all_of where
#' @importFrom rlang .data `:=` `!!`
#' @importFrom stats na.omit
#' @export
read_observed_func <- function(file_path, SheetName, VarName, NewVarName, UnitCorrect) {
  
  # ---- DATASET DETECTION FROM FILE PATH ----
  dataset_name <- "Unknown"
  if (grepl("WWHI", file_path, ignore.case = TRUE)) {
    dataset_name <- "WWHI"
  } else if (grepl("EVA", file_path, ignore.case = TRUE)) {
    dataset_name <- "EVA"
  }
  
  # ---- SANITIZE & RESOLVE SHEET ARGUMENT ----
  if (is.null(SheetName) || is.na(SheetName) || SheetName == "") {
    stop("Invalid or empty sheet name in metadata.")
  }
  
  possible_sheets <- trimws(unlist(strsplit(as.character(SheetName), "\\|")))
  available_sheets <- readxl::excel_sheets(file_path)
  valid_sheets <- possible_sheets[possible_sheets %in% available_sheets]
  
  if (length(valid_sheets) == 0) {
    stop(sprintf(
      "None of the requested sheets ('%s') were found in the file. Available sheets: %s",
      paste(possible_sheets, collapse = "' or '"),
      paste(available_sheets, collapse = ", ")
    ))
  }
  target_sheet <- valid_sheets[1]
  
  # ---- READ THE SHEET ----
  df <- readxl::read_excel(
    path = file_path,
    sheet = target_sheet,
    col_types = "text"
  )
  
  # ---- DYNAMIC COLUMN DETECTION ----
  # Find Date
  date_col <- names(df)[grepl("date", names(df), ignore.case = TRUE)][1]
  if (is.na(date_col)) {
    stop(paste("No date column found in sheet:", target_sheet))
  }
  
  # Find Sow Time (matches "sow time", "sow_time", "SowTime", etc.)
  sow_col <- names(df)[grepl("sow.*time", names(df), ignore.case = TRUE)][1]
  if (is.na(sow_col)) {
    stop(paste("No Sow Time column found in sheet:", target_sheet))
  }
  
  parse_any_date <- function(x) {
    suppressWarnings({
      nums <- suppressWarnings(as.numeric(x))
      if (!all(is.na(nums))) {
        out <- as.Date(nums, origin = "1899-12-30")
        if (!all(is.na(out))) return(out)
      }
      formats <- c("%d/%m/%y", "%d/%m/%Y", "%Y-%m-%d")
      for (fmt in formats) {
        out <- suppressWarnings(as.Date(x, format = fmt))
        if (!all(is.na(out))) return(out)
      }
      suppressWarnings(as.Date(x))
    })
  }
  
  # ---- PROCESS AND SUMMARIZE PIPELINE ----
  df <- df %>%
    # 1. Safely rename Cultivar/Line
    dplyr::rename(dplyr::any_of(c(Cultivar = "!Cultivar", Cultivar = "!Line"))) %>%
    
    # 2. Add our new descriptive columns and clean existing ones
    dplyr::mutate(
      Dataset   = as.factor(dataset_name),
      
      # DYNAMIC SOW TIME CLEANER (Safe from PCRE engine quirks)
      # Finds the match, lowercases the prefix, title-cases the month, and adds a hyphen
      SowTime   = as.factor(stringr::str_replace(
        string = .data[[sow_col]],
        pattern = stringr::regex("\\b(early|mid|late)[\\s_]+([a-z]+)", ignore_case = TRUE),
        replacement = function(m) {
          prefix <- tolower(stringr::str_extract(m, "(?i)early|mid|late"))
          month  <- stringr::str_to_title(stringr::str_extract(m, "(?i)[a-z]+$"))
          paste0(prefix, "-", month)
        }
      )),
      
      Date      = parse_any_date(.data[[date_col]]),
      Block     = as.factor(`!Block`),
      Cultivar  = as.factor(Cultivar),
      !!NewVarName := as.numeric(.data[[VarName]])
    ) %>%
    
    # 3. Keep the metadata columns for the final output
    dplyr::select(Dataset, SowTime, Date, Cultivar, Block, dplyr::all_of(NewVarName)) %>%
    
    # 4. Group by all metadata factors so they aren't lost when calculating the mean
    dplyr::group_by(Dataset, SowTime, Date, Cultivar) %>%
    dplyr::summarise(
      dplyr::across(
        dplyr::where(is.numeric),
        ~ replace(mean(.x, na.rm = TRUE) * UnitCorrect,
                  is.nan(mean(.x, na.rm = TRUE)), NA)
      ),
      .groups = "drop"
    ) %>%
    stats::na.omit()
  
  
  return(df)
}