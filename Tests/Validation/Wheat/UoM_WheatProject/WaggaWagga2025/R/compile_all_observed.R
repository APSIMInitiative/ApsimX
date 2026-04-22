compile_all_observed <- function(folder, excel_file, df_obs_info) {
  
  # Defensive checks
  if (!all(c("df_name","sheet_name","column_name","apsim_var_name","corr_fact") %in% names(df_obs_info))) {
    stop("df_obs_info missing required columns")
  }
  
  file_path <- file.path(folder, excel_file)
  results <- vector("list", nrow(df_obs_info))
  
  for (i in seq_len(nrow(df_obs_info))) {
    row <- df_obs_info[i, ]
    
    results[[i]] <- read_observed_func(
      file_path   = file_path,
      SheetName   = as.character(row$sheet_name),
      VarName     = as.character(row$column_name),
      NewVarName  = as.character(row$apsim_var_name),
      UnitCorrect = as.numeric(row$corr_fact)
    )
    
    names(results)[i] <- row$df_name
  }
  
  # create tibble output
  final <- tibble(
    df_name = df_obs_info$df_name,
    data    = results
  )
  
  return(final)
  
}
