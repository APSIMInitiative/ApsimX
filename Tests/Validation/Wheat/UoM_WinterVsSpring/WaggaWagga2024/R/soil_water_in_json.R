# df<-tar_read(df_soil_water)
#library(jsonlite)

soil_water_in_json <- function (df) {
  
  require(jsonlite)
  
  # 1. Clean NaN values (as discussed)
  mean_val <- mean(df$InitialWater_mean, na.rm = TRUE)
  df$InitialWater_mean[is.nan(df$InitialWater_mean)] <- mean_val
  
  # 2. Round the values to 2 decimal places
  # Note: In standard JSON numbers, "0.40" is usually represented as "0.4"
  df$InitialWater_mean <- round(df$InitialWater_mean, 2)
  
  
  # 3. Create the list, but wrap columns with as.list()
  # This forces the vertical "one-per-row" formatting
  json_structure <- list(
    Thickness = as.list(df$Thickness),
    InitialValues = as.list(df$InitialWater_mean)
  )
  
  # 4. Convert to JSON
  # auto_unbox = TRUE is CRITICAL here to prevent "[100]" instead of "100"
  json_output <- toJSON(json_structure, pretty = TRUE, auto_unbox = TRUE, digits = NA)
  return(json_output)
}