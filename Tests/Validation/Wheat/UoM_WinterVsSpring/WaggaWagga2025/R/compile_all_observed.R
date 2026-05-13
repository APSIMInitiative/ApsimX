#' Compile Observed Data from Multiple Excel Files
#'
#' @export
compile_all_observed <- function(folder, excel_files, df_obs_info, df_simNames) {
  
  req_cols <- c("df_name", "sheet_name", "column_name", "apsim_var_name", "corr_fact")
  missing_cols <- setdiff(req_cols, names(df_obs_info))
  if (length(missing_cols) > 0) stop("Missing columns in metadata.")
  
  full_paths <- file.path(folder, excel_files)
  missing_files <- full_paths[!file.exists(full_paths)]
  if (length(missing_files) > 0) stop("Files not found on disk.")
  
  final_tibble <- df_obs_info |> 
    dplyr::mutate(
      data = purrr::pmap(
        list(sheet_name, column_name, apsim_var_name, corr_fact),
        function(sh, col, new_col, corr) {
          
          purrr::map_dfr(full_paths, function(path) {
            read_observed_func(
              file_path   = path,
              SheetName   = as.character(sh),
              VarName     = as.character(col),
              NewVarName  = as.character(new_col),
              UnitCorrect = as.numeric(corr),
              df_simNames = df_simNames       # Pass the mapping table down
            )
          })
        }
      )
    ) |> 
    dplyr::select(df_name, data)
  
  return(final_tibble)
}