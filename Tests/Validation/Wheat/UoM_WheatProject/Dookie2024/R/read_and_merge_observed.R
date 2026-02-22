#' Read, extract specific columns, validate, and merge observed data
#'
#' @param path Character. The base directory path where the files are located.
#' @param file_names Character vector. A vector of file names to read.
#' @param sheet Character. The name of the sheet to extract from the Excel files.
#' @param cols_to_extract Character vector (or list). The specific columns to extract and bind.
#' @return A single merged dataframe (tibble).
#' @export
read_and_merge_observed <- function(path, file_names, sheet, cols_to_extract) {
  
  cols_to_extract <- unlist(cols_to_extract)
  full_paths <- file.path(path, file_names)
  
  df_list <- lapply(seq_along(full_paths), function(i) {
    file <- full_paths[i]
    fname <- file_names[i]
    
    if (!file.exists(file)) {
      stop("File not found: ", file)
    }
    
    df <- readxl::read_excel(path = file, sheet = sheet)
    
    missing_cols <- setdiff(cols_to_extract, colnames(df))
    if (length(missing_cols) > 0) {
      stop(
        sprintf(
          "Missing required columns in file '%s': %s",
          fname,
          paste(missing_cols, collapse = ", ")
        )
      )
    }
    
    # Extract only the requested columns
    df <- dplyr::select(df, dplyr::all_of(cols_to_extract))
    
    # Safely convert Clock.Today to Date if it exists
    if ("Clock.Today" %in% colnames(df)) {
      df <- df %>%
        dplyr::mutate(Clock.Today = as.Date(Clock.Today))
    }
    
    return(df)
  })
  
  merged_df <- dplyr::bind_rows(df_list) %>%
    tidyr::pivot_longer(
      cols = tidyselect::contains("Wheat.Phenology"),
      names_to = "VarName",
      values_to = "VarValue"
    ) %>%
    na.omit()
  
  return(merged_df)
}