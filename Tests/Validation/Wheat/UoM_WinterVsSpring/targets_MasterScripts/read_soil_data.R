#' Read, Format, and Average Soil Data
#'
#' @description
#' Reads raw soil data, combines depth columns, safely forces string artifacts 
#' into NAs, averages variables, and strictly orders the profile by increasing depth.
#'
#' @param folder Character. Path to the folder containing the file.
#' @param file Character. Name of the Excel file.
#' @param sheet Character. Name of the sheet to read.
#' @param vars_to_extract Character vector of column names to process.
#' @param col_depth_from Character. Exact name of the 'Depth From' column.
#' @param col_depth_to Character. Exact name of the 'Depth To' column.
#'
#' @return A clean dataframe sorted top-to-bottom by depth.
#' @export
read_soil_data <- function(folder, file, sheet, vars_to_extract, 
                           col_depth_from = "Depth From", 
                           col_depth_to = "Depth To") {
  
  if (!requireNamespace("readxl", quietly = TRUE)) stop("Package 'readxl' required.")
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' required.")
  
  file_path <- file.path(folder, file)
  if (!file.exists(file_path)) stop(sprintf("CRITICAL: File not found at '%s'", file_path))
  
  message(sprintf("📖 Reading soil data from: %s (Sheet: %s)", file, sheet))
  
  # 1. Read the raw data
  df_raw <- readxl::read_excel(file_path, sheet = sheet)
  
  # 2. Variable Consistency Check
  missing_cols <- setdiff(c(col_depth_from, col_depth_to, vars_to_extract), names(df_raw))
  if (length(missing_cols) > 0) {
    stop(sprintf("CRITICAL: The following requested columns are missing from the sheet: %s", 
                 paste(missing_cols, collapse = ", ")))
  }
  
  # 3. Process, Scrub, Average, and SORT
  df_processed <- df_raw %>%
    dplyr::mutate(
      Depth = paste0(.data[[col_depth_from]], "-", .data[[col_depth_to]]),
      
      # BULLETPROOF SORT KEY: Strip all letters/symbols and force to pure number
      SortKey = suppressWarnings(as.numeric(gsub("[^0-9.]", "", .data[[col_depth_from]])))
    ) %>%
    dplyr::mutate(
      dplyr::across(dplyr::all_of(vars_to_extract), ~ suppressWarnings(as.numeric(.)))
    ) %>%
    dplyr::group_by(Depth, SortKey) %>%
    dplyr::summarise(
      dplyr::across(
        dplyr::all_of(vars_to_extract), 
        ~ mean(., na.rm = TRUE)
      ),
      .groups = "drop"
    ) %>%
    # STRICT INCREASING ORDER: Sorts topsoil (e.g., 0) down to subsoil (e.g., 160)
    dplyr::arrange(SortKey) %>%
    dplyr::select(-SortKey)
  
  # 4. Final NA warning
  total_nas <- sum(is.na(df_processed[vars_to_extract]))
  if (total_nas > 0) {
    message(sprintf("⚠️ Note: %d NAs generated/detected in the requested variables.", total_nas))
  }
  
  return(df_processed)
}