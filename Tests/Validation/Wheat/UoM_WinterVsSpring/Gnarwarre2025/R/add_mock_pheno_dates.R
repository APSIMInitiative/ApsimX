#' Add Mock Phenology Dates to Observation Data (Temporary Fix)
#'
#' @description
#' This is a temporary pipeline step to append missing phenological stages 
#' (e.g., emergence, flowering) to the main observations dataset. 
#' It uses row-binding (`bind_rows`) so that if the mock data contains 
#' new variables, they are created as new columns, and if they share 
#' existing column names, the data is appended under them.
#' 
#' Prints a highly visible warning to the console reminding the user 
#' to remove this step before final analysis.
#'
#' @param obs_data A data.frame, tibble, or file path to the main observations.
#' @param mock_data A data.frame, tibble, or file path to the mock phenology CSV.
#'
#' @return A tibble with the combined rows and dynamically merged columns.
#'
#' @importFrom dplyr bind_rows mutate arrange
#' @importFrom rlang .data
#' @export
add_mock_pheno_dates <- function(obs_data, mock_data) {
  
  # 1. Helper function to handle Polymorphic Input 
  read_data <- function(data_input) {
    if (is.character(data_input) && length(data_input) == 1 && file.exists(data_input)) {
      if (grepl("\\.csv$", data_input, ignore.case = TRUE)) {
        return(read.csv(data_input, stringsAsFactors = FALSE))
      } else if (grepl("\\.rds$", data_input, ignore.case = TRUE)) {
        return(readRDS(data_input))
      } else {
        stop("Unsupported file type. Please provide a .csv or .rds file.")
      }
    } else if (is.data.frame(data_input)) {
      return(data_input)
    } else {
      stop("Input must be a valid data.frame or an existing file path.")
    }
  }
  
  # Load the data
  obs_df <- read_data(obs_data)
  mock_df <- read_data(mock_data)
  
  # 2. Input Validation
  if (!all(c("SimulationName", "Clock.Today") %in% names(mock_df))) {
    stop("The mock pheno dataset must contain 'SimulationName' and 'Clock.Today' columns.")
  }
  
  # 3. Identify the injected variables for the warning message
  # We look at what columns are in the mock data, ignoring the join keys
  injected_vars <- setdiff(names(mock_df), c("SimulationName", "Clock.Today"))
  
  # 4. Print the Highly Visible Warning
  if (length(injected_vars) > 0) {
    warning_box <- c(
      "",
      "======================================================================",
      " ⚠️  WARNING: TEMPORARY MOCK PHENOLOGY DATA ADDED ⚠️ ",
      "======================================================================",
      " TEST values were added to the following variables:",
      sprintf(" -> %s", paste(injected_vars, collapse = ", ")),
      "",
      " ACTION REQUIRED:",
      " Please update the raw dataset with actual observed values",
      " and EXCLUDE the `add_mock_pheno_dates()` function from `_targets.R`.",
      "======================================================================",
      ""
    )
    # Using message() ensures it prints visibly to the console during tar_make()
    message(paste(warning_box, collapse = "\n"))
  }
  
  # 5. Date Normalization (CRITICAL)
  obs_df <- obs_df |> 
    dplyr::mutate(Clock.Today = as.Date(.data$Clock.Today))
  
  mock_df <- mock_df |> 
    dplyr::mutate(Clock.Today = as.Date(.data$Clock.Today))
  
  # 6. Core Logic: Row Binding
  result <- dplyr::bind_rows(obs_df, mock_df) |> 
    dplyr::arrange(.data$SimulationName, .data$Clock.Today)
  
  return(result)
}