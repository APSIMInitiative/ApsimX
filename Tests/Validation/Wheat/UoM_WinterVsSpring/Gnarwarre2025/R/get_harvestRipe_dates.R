#' Extract Harvest-Ripe Dates from Observation Data
#'
#' @description
#' This function identifies the latest measurement date for a specified reference
#' variable (e.g., grain yield) for each simulation. It assumes this final 
#' measurement date corresponds to the "HarvestRipe" phenological stage.
#'
#' @param obs_data A data.frame, tibble, or a character string representing a 
#'   file path to a CSV/RDS file containing the observation data.
#' @param ref_yield_var Character string. The name of the column containing the 
#'   reference yield or biomass measurement (e.g., "Yield_kg_ha").
#' @param date_col Character string. The name of the column containing the date 
#'   or DOY (e.g., "Date", "Clock.Today").
#' @param ignore_zeros Logical. If TRUE (default), values of exactly 0 in the 
#'   `ref_yield_var` column are treated as "unmeasured" and excluded.
#'
#' @return A tibble with three columns: 
#'   \code{SimulationName}, \code{Clock.Today} (Date format YYYY-MM-DD), 
#'   and \code{Wheat.Phenology.CurrentStageName} (set to "HarvestRipe").
#'
#' @importFrom dplyr filter group_by summarise mutate select arrange pull
#' @importFrom rlang .data
#' @export
get_harvestRipe_dates <- function(obs_data, ref_yield_var, date_col, ignore_zeros = TRUE) {
  
  # 1. Handle Polymorphic Input (File path vs Dataframe)
  # This is crucial for targets: if `obs_data` is a file path tracked by tar_file(), read it.
  if (is.character(obs_data) && length(obs_data) == 1 && file.exists(obs_data)) {
    if (grepl("\\.csv$", obs_data, ignore.case = TRUE)) {
      obs_data <- read.csv(obs_data, stringsAsFactors = FALSE)
    } else if (grepl("\\.rds$", obs_data, ignore.case = TRUE)) {
      obs_data <- readRDS(obs_data)
    } else {
      stop("Unsupported file type provided to `obs_data`. Please provide a .csv or .rds file.")
    }
  }
  
  # Ensure it's a dataframe at this point
  if (!is.data.frame(obs_data)) {
    stop("`obs_data` must be a data.frame or a valid file path.")
  }
  
  # 2. Input Validation
  required_cols <- c("SimulationName", ref_yield_var, date_col)
  missing_cols <- setdiff(required_cols, names(obs_data))
  
  if (length(missing_cols) > 0) {
    stop(sprintf(
      "The following required columns are missing from `obs_data`: %s", 
      paste(missing_cols, collapse = ", ")
    ))
  }
  
  # 3. Data Processing Pipeline
  result <- obs_data |> 
    # Standardize the date column immediately to catch parsing errors
    dplyr::mutate(Clock.Today = as.Date(.data[[date_col]])) |> 
    
    # Filter for valid measurements
    dplyr::filter(!is.na(.data[[ref_yield_var]]))
  
  # Optional: Drop 0s if they represent non-measurements rather than true 0 yield
  if (ignore_zeros) {
    result <- result |> 
      dplyr::filter(.data[[ref_yield_var]] > 0)
  }
  
  # 4. Core Logic: Find the latest date per simulation
  final_dates <- result |> 
    dplyr::group_by(.data$SimulationName) |> 
    dplyr::summarise(
      Clock.Today = max(.data$Clock.Today, na.rm = TRUE),
      .groups = "drop"
    ) |> 
    
    # Append the required APSIM phenology stage column
    dplyr::mutate(
      Wheat.Phenology.CurrentStageName = "HarvestRipe"
    ) |> 
    
    # Sort for cleaner downstream diffs/version control
    dplyr::arrange(.data$SimulationName)
  
  # 5. Edge Case Handling: Unmeasured simulations
  if (nrow(final_dates) == 0) {
    warning("No valid harvest dates found. All measurements were NA or 0. Returning an empty dataframe.")
  }
  
  return(final_dates)
}