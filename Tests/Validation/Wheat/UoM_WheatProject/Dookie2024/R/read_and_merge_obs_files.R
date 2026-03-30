#' Read and merge multiple APSIM observed data Excel files
#'
#' Iterates through a vector of Excel files from a specified folder, validates 
#' the presence of mandatory columns (`SimulationName` and `Clock.Today`), 
#' and binds them together. Columns present in only some files will be filled 
#' with `NA` for the others. Empty Excel rows are automatically removed.
#'
#' @param folder_path Character. The base directory path where the files are located.
#' @param file_names Character vector. A vector of Excel file names to read.
#' @param sheet_name Character. The name of the sheet to read.
#' @return A single merged dataframe (tibble).
#' @export
read_and_merge_obs_files <- function(folder_path, file_names, sheet_name) {
  
  # 1. Create a helper function to process a single file
  process_single_file <- function(filename) {
    filepath <- file.path(folder_path, filename)
    
    if (!file.exists(filepath)) {
      stop("File not found: ", filepath)
    }
    
    # Read the specific sheet
    df <- readxl::read_excel(path = filepath, sheet = sheet_name)
    
    # 2. Enforce mandatory columns
    mandatory_cols <- c("SimulationName", "Clock.Today")
    missing_cols <- setdiff(mandatory_cols, colnames(df))
    
    if (length(missing_cols) > 0) {
      stop(
        sprintf(
          "Validation failed! File '%s' is missing mandatory columns: %s", 
          filename, 
          paste(missing_cols, collapse = ", ")
        )
      )
    }
    
    # 3. Clean up ghost rows and format dates
    df <- df %>%
      # Drop rows where both SimulationName and Clock.Today are entirely blank
      dplyr::filter(!is.na(SimulationName) | !is.na(Clock.Today)) %>%
      dplyr::mutate(Clock.Today = as.Date(Clock.Today))
    
    return(df)
  }
  
  # 4. Loop through the vector of file names and apply the helper function
  list_of_dfs <- lapply(file_names, process_single_file)
  
  # 5. Bind the entire list together in one go
  df_merged <- dplyr::bind_rows(list_of_dfs)
  
  return(df_merged)
}