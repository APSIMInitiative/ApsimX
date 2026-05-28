#' Save Final Observed Data to Excel
#'
#' @description
#' Writes the finalized, APSIM-ready observation dataframe to an Excel workbook.
#' This function safely handles directory creation and returns the exact file 
#' path, making it perfectly compatible with `{targets}` file tracking.
#'
#' @details
#' **Pipeline Tracking:** In a `{targets}` pipeline, targets that generate external 
#' files should return the exact character path to the saved file. This allows 
#' `targets` to hash the file on disk and detect if it was modified outside the 
#' pipeline. Ensure the target calling this function is configured with `format = "file"`.
#'
#' @param df_final A data.frame containing the final compiled observations.
#' @param obs_path Character. The directory path where the file should be saved.
#' @param file_name Character. The name of the Excel file (e.g., "Observed_Data.xlsx").
#'
#' @return Character. The full path to the saved Excel file.
#'
#' @importFrom openxlsx createWorkbook addWorksheet writeData saveWorkbook
#' @export
save_df_final <- function(df_final, obs_path, file_name) {
  
  # 1. Ensure the output directory exists
  if (!dir.exists(obs_path)) {
    dir.create(obs_path, recursive = TRUE)
  }
  
  # 2. Construct the full save path
  save_path <- file.path(obs_path, file_name)
  
  # 3. Create and populate the workbook
  wb <- openxlsx::createWorkbook()
  openxlsx::addWorksheet(wb, "Observed")
  openxlsx::writeData(wb, sheet = "Observed", x = df_final)
  
  # 4. Save the file (overwriting if it already exists)
  openxlsx::saveWorkbook(wb, save_path, overwrite = TRUE)
  
  # 5. Notify the console
  message(sprintf("Successfully saved observed data to: %s", save_path))
  
  # 6. Return the exact file path for targets tracking
  return(save_path)
}