#' Read and Process Gravimetric Soil Water Content from Excel
#'
#' @description
#' A universal data-ingestion function designed for the APSIM-X data pipeline.
#' It reads gravimetric soil moisture data across specified depth intervals,
#' normalizes naming conventions, computes mean water content fractions, 
#' and calculates physical soil profile thickness layer intervals in millimeters (mm).
#'
#' @param folder_path String. The absolute or relative path to the folder containing the raw data file.
#' @param file_name String. The exact name of the target Excel file (e.g., "2024_WaggaWagga_PHDA24WARI2.xlsx").
#' @param SheetName String. The exact name of the Excel worksheet containing the soil data.
#'
#' @return A data frame grouped by \code{Depth} containing calculated parameters:
#' \itemize{
#'   \item \code{Depth}: Original interval string (e.g., "0..10").
#'   \item \code{InitialWater_mean}: Mean volumetric/gravimetric water content fraction (0.0 - 1.0).
#'   \item \code{SoilDepthStart}: The upper limit of the layer boundary in mm.
#'   \item \code{SoilDepthEnd}: The lower limit of the layer boundary in mm.
#'   \item \code{Thickness}: The physical depth thickness of the layer segment in mm.
#' }
#' @export
#'
#' @examples
#' \dontrun{
#' df_soil <- read_soil_water(
#'   folder_path = "WaggaWagga2024", 
#'   file_name = "Wagga_RawData.xlsx", 
#'   SheetName = "GravimetricMoistureNearSowing"
#' )
#' }
read_soil_water_data <- function(folder_path, file_name, SheetName) {
  
  # ---- 1. SANITIZE ARGUMENTS ----
  if (is.null(SheetName) || length(SheetName) != 1 || is.na(SheetName) || SheetName == "") {
    stop("Error [read_soil_water]: Invalid or missing 'SheetName' provided.")
  }
  
  file_path <- file.path(folder_path, file_name)
  if (!file.exists(file_path)) {
    stop(paste("Error [read_soil_water]: Target file does not exist at path:", file_path))
  }
  
  # ---- 2. READ RAW DATA ----
  df_raw <- readxl::read_excel(
    path = file_path,
    sheet = SheetName,
    col_types = "text"
  )
  
  # ---- 3. DYNAMIC COLUMN MATCHING ----
  # Safeguard against minor typographic layout differences across experimental sheets
  depth_col <- grep("^Depth$", names(df_raw), ignore.case = TRUE, value = TRUE)
  water_col <- grep("Gravimetric.*Water.*Content|InitialWater", names(df_raw), ignore.case = TRUE, value = TRUE)
  
  if (length(depth_col) == 0 || length(water_col) == 0) {
    stop(paste("Error [read_soil_water]: Required columns ('Depth' and 'Gravimetric_Water_Content') not located in sheet:", SheetName))
  }
  
  # ---- 4. DIBBLE DATA MUTATION PIPELINE ----
  df_processed <- df_raw %>%
    dplyr::select(Depth = dplyr::all_of(depth_col[1]), 
                  RawWater = dplyr::all_of(water_col[1])) %>%
    dplyr::filter(!is.na(Depth), !is.na(RawWater)) %>%
    dplyr::mutate(
      Depth = as.factor(Depth),
      InitialWater = 0.01 * as.numeric(RawWater)
    ) %>%
    dplyr::group_by(Depth) %>%
    dplyr::summarise(InitialWater_mean = mean(InitialWater, na.rm = TRUE), .groups = "drop") %>%
    
    # Separate depth string interval bounds (e.g. "0..10" -> "0", "10")
    tidyr::separate(
      col = Depth, 
      into = c("SoilDepthStart", "SoilDepthEnd"), 
      sep = "\\s*\\.\\.\\s*", 
      convert = TRUE,
      remove = FALSE
    ) %>%
    
    # Sort rows safely by structural numerical depth sequence 
    dplyr::arrange(SoilDepthStart) %>%
    
    # Convert cm values to standard mm required natively by the APSIM soil component
    dplyr::mutate(
      SoilDepthStart = SoilDepthStart * 10,
      SoilDepthEnd   = SoilDepthEnd * 10,
      Thickness      = SoilDepthEnd - SoilDepthStart
    )
  
  return(df_processed)
}