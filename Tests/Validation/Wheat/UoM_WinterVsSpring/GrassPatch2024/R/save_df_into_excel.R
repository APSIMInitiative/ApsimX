save_df_into_excel <- function(df, folder, filename, sheetname) {
  # 1. Construct the full output path
  out_path <- file.path(folder, filename)
  
  # 2. Ensure the directory exists (optional but safe)
  if (!dir.exists(folder)) dir.create(folder, recursive = TRUE)
  
  # Use setNames to dynamically assign the sheet name from the variable
  data_to_save <- setNames(list(df), sheetname)
  
  # 3. Write the file
  writexl::write_xlsx(data_to_save, path = out_path)
  
  # 4. Return the path so targets can track it
  return(out_path)
}

