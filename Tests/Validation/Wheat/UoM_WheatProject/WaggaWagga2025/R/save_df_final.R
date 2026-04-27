save_df_final <- function(df_final, obs_path, file_name) {
  
  wb <- openxlsx::createWorkbook()
  openxlsx::addWorksheet(wb, "Observed")
  openxlsx::writeData(wb, sheet = "Observed", x = df_final)
  
  # Ensure the path exists before saving
  if (!dir.exists(obs_path)) {
    dir.create(obs_path, recursive = TRUE)
  }
  
  save_path <- file.path(obs_path, file_name)
  
  openxlsx::saveWorkbook(wb, save_path, overwrite = TRUE)
  
  # 1. Print the message to the console for your own sanity
  message(sprintf("Saved observed data to: %s", save_path))
  
  # 2. Return ONLY the raw file path for {targets} to track
  return(save_path)
}