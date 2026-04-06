#' Save a dataframe to an Excel file with a specific sheet name
#'
#' Creates the target directory if it doesn't exist and writes the dataframe 
#' to an Excel (.xlsx) file, naming the worksheet based on the provided parameter.
#'
#' @param folder_path Character. The directory where the file will be saved.
#' @param file_name Character. The name of the Excel file (e.g., "Cleaned_Obs.xlsx").
#' @param sheet_name Character. The name to assign to the worksheet (e.g., "Observed").
#' @param df Dataframe. The dataset to save.
#' @return Character. The full path to the saved Excel file.
#' @export
save_df_to_excel <- function(folder_path, file_name, sheet_name, df) {
  
  # Ensure the target directory exists
  dir.create(folder_path, showWarnings = FALSE, recursive = TRUE)
  
  # Construct the full file path
  full_path <- file.path(folder_path, file_name)
  
  # writexl uses the names of a list to set the Excel sheet names
  output_list <- list(df)
  names(output_list) <- sheet_name
  
  # Write the named list to Excel
  writexl::write_xlsx(x = output_list, path = full_path)
  
  # Return the file path so {targets} can track it as an output file
  return(full_path)
}