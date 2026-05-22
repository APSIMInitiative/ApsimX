#' Export Data Frame to Formatted Excel Workbook (Universal Engine)
#'
#' @description
#' A robust, universal utility to write any data frame to an Excel workbook. 
#' It strictly handles missing values and automatically applies professional 
#' formatting (bold headers, frozen top row, auto-fitted columns) for immediate readability.
#'
#' @details
#' **APSIM-X Safety:** Setting \code{keepNA = FALSE} is critical. It ensures that 
#' R's \code{NA} values are written as true blank cells in Excel, preventing APSIM-X 
#' from throwing type-conversion crashes when it encounters the literal text string "NA" 
#' inside a numeric column.
#' 
#' **Pipeline Tracking:** Returns the exact character path to the saved file. 
#' Ensure the target calling this function is configured with \code{format = "file"}.
#'
#' @param df Data frame. The compiled data to be saved.
#' @param folder_path Character. The directory path where the file should be saved.
#' @param file_name Character. The name of the Excel file (e.g., \code{"Observed_Data.xlsx"}).
#' @param sheet_name Character. The name of the worksheet. Defaults to \code{"Sheet1"}.
#'
#' @return Character. The full path to the saved Excel file.
#' @export
save_df_into_excel <- function(df, folder_path, file_name, sheet_name = "Observed") {
  

  
  # ---- 1. DEFENSIVE INTEGRITY CHECKS ----
  if (missing(df) || !is.data.frame(df) || nrow(df) == 0) {
    stop("Error [save_df_to_excel]: Attempted to save an empty or invalid dataframe.")
  }
  if (missing(folder_path) || missing(file_name)) {
    stop("Error [save_df_to_excel]: Must specify both 'folder_path' and 'file_name'.")
  }
  
  # ---- 2. DIRECTORY & PATH MANAGEMENT ----
  if (!dir.exists(folder_path)) {
    dir.create(folder_path, recursive = TRUE)
  }
  save_path <- file.path(folder_path, file_name)
  
  # ---- 3. WORKBOOK INITIALIZATION ----
  wb <- openxlsx::createWorkbook()
  openxlsx::addWorksheet(wb, sheet_name)
  
  # Create a professional bold header style with a bottom border
  header_style <- openxlsx::createStyle(textDecoration = "bold", border = "Bottom")
  
  # ---- 4. DATA INJECTION (APSIM-SAFE) ----
  openxlsx::writeData(
    wb, 
    sheet = sheet_name, 
    x = df, 
    headerStyle = header_style,
    keepNA = FALSE # CRITICAL: Forces NAs to be blank cells, not text strings
  )
  
  # ---- 5. QUALITY OF LIFE FORMATTING ----
  # Auto-fit all columns so SimulationNames and data are immediately readable
  openxlsx::setColWidths(wb, sheet = sheet_name, cols = 1:ncol(df), widths = "auto")
  
  # Freeze the top row so headers stay visible when scrolling down large datasets
  openxlsx::freezePane(wb, sheet = sheet_name, firstRow = TRUE)
  
  # ---- 6. SAVE & RETURN FOR TARGETS TRACKING ----
  openxlsx::saveWorkbook(wb, save_path, overwrite = TRUE)
  
  message(sprintf("Success [save_df_to_excel]: Exported %d rows to '%s' (Sheet: '%s').", 
                  nrow(df), save_path, sheet_name))
  
  return(save_path)
}