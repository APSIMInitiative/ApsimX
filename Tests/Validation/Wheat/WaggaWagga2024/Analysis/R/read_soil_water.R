read_soil_water <- function(folder_path, file_name, SheetName) {
  
  require(dplyr)
  
  # ---- SANITIZE SHEET ARGUMENT ----
  # Must be length 1 and not NA/empty
  if (is.null(SheetName) || length(SheetName) != 1 || is.na(SheetName) || SheetName == "") {
    stop(paste("Invalid sheet name in metadata:", SheetName))
  }
  
  file_path <- file.path(folder_path, file_name)
  
  # ---- READ THE SHEET ----
  df <- readxl::read_excel(
    path = file_path,
    sheet = SheetName,
    col_types = "text"
  )
  
  
  df <- df %>%
    dplyr::select(Depth, `Gravimetric_Water_Content %`) %>%
    mutate(Depth=as.factor(Depth), 
           InitialWater = 0.01*as.numeric(`Gravimetric_Water_Content %`)) %>%
    group_by(Depth) %>%
    summarise(InitialWater_mean = mean(InitialWater, na.rm = TRUE))
  
 return (df)
   
}