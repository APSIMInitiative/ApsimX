#' Read and Process Gravimetric Soil Water Content from Excel
#'
#' @description
#' A universal data-ingestion function designed for the APSIM-X data pipeline.
#' It safely identifies both single-column (e.g., "0..10") and dual-column 
#' (e.g., "Depth From", "Depth To") raw depth layouts, computes mean water 
#' content fractions, and calculates physical soil profile thickness layer 
#' intervals in millimeters (mm).
#'
#' @param folder_path String. The absolute or relative path to the folder containing the raw data file.
#' @param file_name String. The exact name of the target Excel file (e.g., "2024_WaggaWagga_PHDA24WARI2.xlsx").
#' @param SheetName String. The exact name of the Excel worksheet containing the soil data.
#'
#' @return A data frame grouped by \code{Depth} containing calculated parameters.
#' @export
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
  
  # ---- 3. DYNAMIC COLUMN DETECTION ----
  # Water Column (Matches "Gravimetric_Water_Content" or "InitialWater")
  water_col <- grep("Gravimetric.*Water.*Content|InitialWater", names(df_raw), ignore.case = TRUE, value = TRUE)[1]
  
  # Depth Layout Variations
  depth_single <- grep("^Depth$", names(df_raw), ignore.case = TRUE, value = TRUE)[1]
  depth_from   <- grep("Depth.*From", names(df_raw), ignore.case = TRUE, value = TRUE)[1]
  depth_to     <- grep("Depth.*To", names(df_raw), ignore.case = TRUE, value = TRUE)[1]
  
  if (is.na(water_col)) {
    stop(paste("Error [read_soil_water]: Required Water Content column not located in sheet:", SheetName))
  }
  
  # ---- 4. ADAPTIVE MUTATION PIPELINE ----
  
  # SCENARIO A: Dual-Column Layout (e.g., "Depth From (cm)" and "Depth To (cm)")
  if (!is.na(depth_from) && !is.na(depth_to)) {
    df_processed <- df_raw %>%
      dplyr::select(
        SoilDepthStart = dplyr::all_of(depth_from),
        SoilDepthEnd   = dplyr::all_of(depth_to),
        RawWater       = dplyr::all_of(water_col)
      ) %>%
      dplyr::filter(!is.na(SoilDepthStart), !is.na(RawWater)) %>%
      dplyr::mutate(
        SoilDepthStart = as.numeric(SoilDepthStart),
        SoilDepthEnd   = as.numeric(SoilDepthEnd),
        Depth          = as.factor(paste0(SoilDepthStart, "..", SoilDepthEnd)), # Recreate standard ID
        InitialWater   = 0.01 * as.numeric(RawWater)
      )
    
    # SCENARIO B: Single-Column Layout (e.g., "Depth" = "0..10")
  } else if (!is.na(depth_single)) {
    df_processed <- df_raw %>%
      dplyr::select(
        Depth    = dplyr::all_of(depth_single), 
        RawWater = dplyr::all_of(water_col)
      ) %>%
      dplyr::filter(!is.na(Depth), !is.na(RawWater)) %>%
      dplyr::mutate(
        Depth        = as.factor(Depth),
        InitialWater = 0.01 * as.numeric(RawWater)
      ) %>%
      tidyr::separate(
        col     = Depth, 
        into    = c("SoilDepthStart", "SoilDepthEnd"), 
        sep     = "\\s*\\.\\.\\s*", 
        convert = TRUE,
        remove  = FALSE
      )
  } else {
    stop(paste("Error [read_soil_water]: Could not find 'Depth' or 'Depth From/To' columns in sheet:", SheetName))
  }
  
  # ---- 5. COMMON APSIM-X FORMATTING ----
  df_final <- df_processed %>%
    # Group by the established boundaries to calculate mean
    dplyr::group_by(Depth, SoilDepthStart, SoilDepthEnd) %>%
    dplyr::summarise(InitialWater_mean = mean(InitialWater, na.rm = TRUE), .groups = "drop") %>%
    
    # Sort safely by structural numerical sequence 
    dplyr::arrange(SoilDepthStart) %>%
    
    # Convert cm values to standard mm natively required by APSIM
    dplyr::mutate(
      SoilDepthStart = SoilDepthStart * 10,
      SoilDepthEnd   = SoilDepthEnd * 10,
      Thickness      = SoilDepthEnd - SoilDepthStart
    )
  
  return(df_final)
}