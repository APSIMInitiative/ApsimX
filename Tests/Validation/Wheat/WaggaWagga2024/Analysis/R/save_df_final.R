save_df_final <- function(df_final, obs_path, file_name) {
  
  # 5. Save the Excel file (Side Effect)
  # IMPORTANT: This saves the file as a side effect.
  # If you want the pipeline to only track the data, you can move this
  # into a separate target (see section 2 below).
  wb <- openxlsx::createWorkbook()
  openxlsx::addWorksheet(wb, "Observed")
  openxlsx::writeData(wb, sheet = "Observed", x = df_final)
  
  # Ensure the path exists before saving
  # if (!dir.exists(obs_path)) {
  #   dir.create(obs_path, recursive = TRUE)
  # }
  
  save_path <- file.path(obs_path, file_name)
  
  openxlsx::saveWorkbook(wb, save_path, overwrite = TRUE)
  
  message(paste0("Saved observed data to:", save_path))
  
  x <- paste("Saved observed data to:", save_path)
  
  return(x)
}

