#' Save Final Observed Data to Excel (Universal Master)
#'
#' @description
#' Writes the finalized, APSIM-ready observation dataframe to an Excel workbook.
#' Automatically formats the sheet for readability (freezes headers, auto-fits 
#' columns) and safely handles directory creation.
#'
#' @details
#' **Pipeline Tracking:** Returns the exact character path to the saved file. 
#' Ensure the target calling this function is configured with `format = "file"`.
#'
#' @param df_final A data.frame containing the final compiled observations.
#' @param obs_path Character. The directory path where the file should be saved.
#' @param file_name Character. The name of the Excel file (e.g., "Observed_Data.xlsx").
#' @param sheetName Character. The name of the worksheet. Defaults to "Observed".
#'
#' @return Character. The full path to the saved Excel file.
#'
#' @importFrom openxlsx createWorkbook addWorksheet writeData saveWorkbook createStyle setColWidths freezePane
#' @export
save_obs_to_excel <- function(df_final, obs_path, file_name, sheetName = "Observed") {
  
  require(openxlsx)
  
  # ------------------------------------------------------------------
  # 1. THE FIREWALL
  # ------------------------------------------------------------------
  if (!is.data.frame(df_final) || nrow(df_final) == 0) {
    stop("CRITICAL: Attempted to save an empty or invalid dataframe to Excel.")
  }
  
  # Ensure the output directory exists
  if (!dir.exists(obs_path)) {
    dir.create(obs_path, recursive = TRUE)
  }
  
  save_path <- file.path(obs_path, file_name)
  
  # ------------------------------------------------------------------
  # 2. CREATE WORKBOOK & SHEET
  # ------------------------------------------------------------------
  wb <- openxlsx::createWorkbook()
  openxlsx::addWorksheet(wb, sheetName)
  
  # Create a bold header style
  headerStyle <- openxlsx::createStyle(textDecoration = "bold", border = "Bottom")
  
  # ------------------------------------------------------------------
  # 3. WRITE DATA (APSIM-SAFE)
  # ------------------------------------------------------------------
  # keepNA = FALSE ensures NAs become blank cells, not the text "NA"
  openxlsx::writeData(
    wb, 
    sheet = sheetName, 
    x = df_final, 
    headerStyle = headerStyle,
    keepNA = FALSE
  )
  
  # ------------------------------------------------------------------
  # 4. QUALITY OF LIFE FORMATTING
  # ------------------------------------------------------------------
  # Auto-fit all columns so SimulationNames are immediately readable
  openxlsx::setColWidths(wb, sheet = sheetName, cols = 1:ncol(df_final), widths = "auto")
  
  # Freeze the top row so headers stay visible when scrolling down
  openxlsx::freezePane(wb, sheet = sheetName, firstRow = TRUE)
  
  # ------------------------------------------------------------------
  # 5. SAVE & NOTIFY
  # ------------------------------------------------------------------
  openxlsx::saveWorkbook(wb, save_path, overwrite = TRUE)
  
  message(sprintf("\u2705 Successfully saved formatted observed data to: %s", save_path))
  
  # Return the exact file path for targets tracking
  return(save_path)
}