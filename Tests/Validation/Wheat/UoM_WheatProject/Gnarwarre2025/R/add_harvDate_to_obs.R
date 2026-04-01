#' Add Harvest-Ripe Dates to Observation Data
#'
#' @description
#' This function merges the derived "HarvestRipe" phenology stages back into 
#' the main observational dataset. It matches records based on `SimulationName` 
#' and `Clock.Today`. If the phenology column already exists in the observations, 
#' it updates the missing values; otherwise, it creates the new column.
#'
#' @param obs_data A data.frame, tibble, or character string (file path) 
#'   containing the main observation data.
#' @param harvest_dates_df A data.frame containing the harvest dates, typically 
#'   the output of `get_harvestRipe_dates()`. Must contain `SimulationName`, 
#'   `Clock.Today`, and `Wheat.Phenology.CurrentStageName`.
#'
#' @return A tibble of the observation data with the updated phenology stages.
#'
#' @importFrom dplyr left_join mutate select coalesce all_of
#' @importFrom rlang .data `:=`
#' @export
add_harvDate_to_obs <- function(obs_data, harvest_dates_df) {
  
  # 1. Handle Polymorphic Input for obs_data (File path vs Dataframe)
  # This allows tar_target() to pass a file tracked by tar_file() directly.
  if (is.character(obs_data) && length(obs_data) == 1 && file.exists(obs_data)) {
    if (grepl("\\.csv$", obs_data, ignore.case = TRUE)) {
      obs_data <- read.csv(obs_data, stringsAsFactors = FALSE)
    } else if (grepl("\\.rds$", obs_data, ignore.case = TRUE)) {
      obs_data <- readRDS(obs_data)
    } else {
      stop("Unsupported file type provided to `obs_data`. Please provide a .csv or .rds file.")
    }
  }
  
  # Type checking
  if (!is.data.frame(obs_data)) stop("`obs_data` must be a data.frame or valid file path.")
  if (!is.data.frame(harvest_dates_df)) stop("`harvest_dates_df` must be a data.frame.")
  
  # 2. Input Validation (Ensure join keys exist)
  join_keys <- c("SimulationName", "Clock.Today")
  pheno_col <- "Wheat.Phenology.CurrentStageName"
  
  if (!all(join_keys %in% names(obs_data))) {
    stop("`obs_data` is missing required join columns: ", paste(setdiff(join_keys, names(obs_data)), collapse = ", "))
  }
  if (!all(c(join_keys, pheno_col) %in% names(harvest_dates_df))) {
    stop("`harvest_dates_df` is missing required columns from the previous step.")
  }
  
  # 3. Date Normalization
  # Crucial step: Prevent silent join failures by ensuring both are Date objects
  obs_data <- obs_data |> 
    dplyr::mutate(Clock.Today = as.Date(.data$Clock.Today))
  
  harvest_dates_df <- harvest_dates_df |> 
    dplyr::mutate(Clock.Today = as.Date(.data$Clock.Today))
  
  # 4. Merge Logic
  # Scenario A: The phenology column already exists in the raw data
  if (pheno_col %in% names(obs_data)) {
    
    # Rename the incoming column temporarily to avoid .x and .y conflicts
    incoming_data <- harvest_dates_df |> 
      dplyr::select(
        .data$SimulationName, 
        .data$Clock.Today, 
        new_stage = dplyr::all_of(pheno_col)
      )
    
    result <- obs_data |> 
      dplyr::left_join(incoming_data, by = join_keys) |> 
      
      # coalesce() looks at new_stage first. If there's a value, it uses it. 
      # If it's NA, it falls back to whatever was already in the phenology column.
      dplyr::mutate(
        !!pheno_col := dplyr::coalesce(.data$new_stage, .data[[pheno_col]])
      ) |> 
      
      # Clean up the temporary column
      dplyr::select(-new_stage) 
    
  } else {
    
    # Scenario B: The phenology column does not exist at all yet
    # A standard left join will naturally append the new column where dates match
    # and fill the rest with NA.
    result <- obs_data |> 
      dplyr::left_join(harvest_dates_df, by = join_keys)
    
  }
  
  return(result)
}