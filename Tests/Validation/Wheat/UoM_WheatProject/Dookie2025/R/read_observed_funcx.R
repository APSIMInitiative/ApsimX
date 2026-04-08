#' Read and Process Observed Excel Data for APSIM
#'
#' @description
#' Reads raw experimental observation data from an Excel sheet, dynamically extracts 
#' and formats dates, standardizes cultivar column names, applies a unit correction 
#' factor, and calculates the mean value across experimental blocks.
#'
#' @details
#' **Date Parsing:** Excel frequently corrupts date formats by converting them to 
#' numeric serial values or mixed string formats. This function includes an internal 
#' helper (`parse_any_date`) that safely tests for Excel serial numbers (origin 1899-12-30) 
#' and multiple standard string formats to guarantee robust date extraction.
#' 
#' **Standardization:** The function safely handles legacy column naming conventions 
#' by checking for either `!Cultivar` or `!Line` and standardizing it to `Cultivar`. 
#' Finally, it aggregates the data, computing the mean of the specified variable 
#' across all blocks for each unique `Date` and `Cultivar` combination.
#'
#' @param file_path Character. The full path to the Excel file.
#' @param SheetName Character. The exact name of the sheet to read. Must be a single, non-empty string.
#' @param VarName Character. The name of the column in the Excel file containing the raw data to extract.
#' @param NewVarName Character. The target name for the extracted variable (typically an APSIM-formatted name).
#' @param UnitCorrect Numeric. A multiplier applied to the aggregated mean to correct units (e.g., converting g/m2 to kg/ha).
#'
#' @return A tidy tibble containing `Date`, `Cultivar`, `Block`, and the aggregated 
#'   numeric variable defined by `NewVarName`. Rows containing `NA` values are omitted.
#'
#' @importFrom readxl read_excel
#' @importFrom dplyr rename any_of mutate select group_by summarise across all_of where
#' @importFrom rlang .data `:=` `!!`
#' @importFrom stats na.omit
#' @export
read_observed_func <- function(file_path, SheetName, VarName, NewVarName, UnitCorrect) {
  
  # ---- SANITIZE & RESOLVE SHEET ARGUMENT ----
  if (is.null(SheetName) || is.na(SheetName) || SheetName == "") {
    stop("Invalid or empty sheet name in metadata.")
  }
  
  # 1. Split the string by "|" and trim any accidental spaces
  # E.g., "Sheet1 | Sheet2" becomes c("Sheet1", "Sheet2")
  possible_sheets <- trimws(unlist(strsplit(as.character(SheetName), "\\|")))
  
  # 2. Ask the Excel file what sheets it actually contains
  available_sheets <- readxl::excel_sheets(file_path)
  
  # 3. Find the intersection (which of our requested sheets exist in the file?)
  valid_sheets <- possible_sheets[possible_sheets %in% available_sheets]
  
  # 4. Fail gracefully if none match
  if (length(valid_sheets) == 0) {
    stop(sprintf(
      "None of the requested sheets ('%s') were found in the file. Available sheets: %s",
      paste(possible_sheets, collapse = "' or '"),
      paste(available_sheets, collapse = ", ")
    ))
  }
  
  # 5. Lock in the first valid sheet found
  target_sheet <- valid_sheets[1]
  
  # ---- READ THE SHEET ----
  df <- readxl::read_excel(
    path = file_path,
    sheet = target_sheet,
    col_types = "text"
  )
  
  # ---- DATE COLUMN DETECTION ----
  date_col <- names(df)[grepl("date", names(df), ignore.case = TRUE)][1]
  if (is.na(date_col)) {
    stop(paste("No date column found in sheet:", SheetName))
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
  
  df <- df %>%
    # 1. Safely rename whichever column exists to a standard name
    dplyr::rename(dplyr::any_of(c(Cultivar = "!Cultivar", Cultivar = "!Line"))) %>%
    
    # 2. Continue with your normal mutate, referencing the newly cleaned name
    dplyr::mutate(
      Date      = parse_any_date(.data[[date_col]]),
      Block     = as.factor(`!Block`),
      Cultivar  = as.factor(Cultivar), # This is now safe, regardless of original name
      !!NewVarName := as.numeric(.data[[VarName]])
    ) %>%
    dplyr::select(Date, Cultivar, Block, dplyr::all_of(NewVarName)) %>%
    dplyr::group_by(Date, Cultivar) %>%
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