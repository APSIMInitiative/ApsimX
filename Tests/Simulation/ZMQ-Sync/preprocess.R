library(jsonlite)
library(soilDB)
library(sp)
library(sf)
library(spData)
library(ggplot2)
library(apsimx)

json_file <- "fields.json"

# Read the settings json
json_data <- fromJSON(json_file)

print(json_data)

# load base 

# Do further processing with the data
# Loop over the array
for (i in 1:length(fields)) {
  # TODO Duplicate 

  # Access each element in the array
  element <- fields[i]

  # get latlon
  lonlat <- c(element$lon, element$lat)

  # Update date range
  date_range <- c(json_data$start_date, json_data$end_date)
  edit_apsimx(
    json_data$basefile,
    node = "Clock",
    parm = c("Start", "End"),
    value = date_range)

  # get soil profile at the location
  sp <- get_ssurgo_soil_profile(lonlat, fix = TRUE)
  edit_apsimx_replace_soil_profile(
    json_data$basefile,
    soil.profile = sp
  )

  # setup weather data
  # download data from NASA-POWER
  met <- get_power_apsim_met(lonlat = lonlat, dates = date_range)
  met_filename <- paste0(
    date_range[1], "_", date_range[2], "_",
    lonlat[1], ",", lonlat[2], ".met"
  )
  write_apsim_met(met, wrt.dir = ".", filename = met_filename)

  # set weather data for apsim file
  edit_apsimx(
    json_data$basefile,
    node = "Weather",
    value = met_filename
  )

  # In the future can fill in gaps
  # https://femiguez.github.io/weather_soil_databases/Weather_Soil_Inputs_APSIM.html


}