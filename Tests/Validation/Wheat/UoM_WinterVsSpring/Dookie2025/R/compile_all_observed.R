#' Compile Observed Data from Multiple Excel Files
#'
#' @description
#' Reads and compiles observational data across an array of Excel files.
#' It iterates through a configuration dataframe (`df_obs_info`) to map sheet
#' and column names, extracts the corresponding data from *all* provided files, 
#' and row-binds the results into a single nested tibble.
#'
#' @param folder Character. The directory path where the Excel files are located.
#' @param excel_files Character vector. One or more Excel file names to be processed 
#'   (e.g., `c("file1.xlsx", "file2.xlsx")`).
#' @param df_obs_info Data frame. Configuration table detailing the variables to extract.
#'   Must contain: \code{df_name}, \code{sheet_name}, \code{column_name}, 
#'   \code{apsim_var_name}, and \code{corr_fact}.
#'
#' @return A tibble with two columns: \code{df_name} (character) and \code{data} 
#'   (list-column containing the combined dataframes for each variable across all files).
#'
#' @importFrom dplyr select mutate
#' @importFrom purrr pmap map_dfr
#' @export
#' 
#' 
compile_all_observed <- function(folder, excel_files, df_obs_info) {
  
  # folder<-info_1
  # excel_files<-info_2
  # df_obs_info<- info_3
  #source("C:/github/ApsimX/Tests/Validation/Wheat/UoM_WheatProject/Dookie2025/R/read_observed_func.R")
  
  # 1. Defensive Checks: Columns
  req_cols <- c("df_name", "sheet_name", "column_name", "apsim_var_name", "corr_fact")
  missing_cols <- setdiff(req_cols, names(df_obs_info))
  
  if (length(missing_cols) > 0) {
    stop("`df_obs_info` is missing required columns: ", paste(missing_cols, collapse = ", "))
  }
  
  # 2. Defensive Checks: File Existence
  full_paths <- file.path(folder, excel_files)
  missing_files <- full_paths[!file.exists(full_paths)]
  
  if (length(missing_files) > 0) {
    stop("The following files were not found on disk:\n", paste(missing_files, collapse = "\n"))
  }
  
  # 3. Core Logic: Iterate over configurations AND files
  final_tibble <- df_obs_info |> 
    dplyr::mutate(
      
      # pmap maps over the rows of df_obs_info
      data = purrr::pmap(
        list(sheet_name, column_name, apsim_var_name, corr_fact),
        function(sh, col, new_col, corr) {
          
          # map_dfr maps over ALL files and immediately row-binds the results
          purrr::map_dfr(full_paths, function(path) {
            
            # Note: Relies on `read_observed_func` being defined in your environment
            read_observed_func(
              file_path   = path,
              SheetName   = as.character(sh),
              VarName     = as.character(col),
              NewVarName  = as.character(new_col),
              UnitCorrect = as.numeric(corr)
            )
            
          })
        }
      )
    ) |> 
    
    # 4. Clean up the output to match the original function's structure
    dplyr::select(df_name, data)
  
  return(final_tibble)
}