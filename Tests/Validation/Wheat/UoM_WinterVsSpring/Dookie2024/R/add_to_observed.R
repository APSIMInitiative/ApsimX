#' Append and Clean New Observation Data
#'
#' @description
#' Appends a new dataframe to an existing nested list of observations (tibble 
#' format). Automatically sanitizes both the existing nested dataframes and the 
#' newly added dataframe by removing excess metadata columns, ensuring strict 
#' APSIM format compatibility (Date, SimulationName, and the variable of interest).
#'
#' @details
#' Because the target variable name fluctuates across different dataframes, this 
#' function uses negative selection. It targets a specific list of excess columns 
#' (`Dataset`, `SowTime`, `Cultivar`, `SowingDate`, `EmergenceDate`) for removal. 
#' The `dplyr::any_of()` helper ensures the function won't crash if one of those 
#' columns is already missing.
#'
#' @param list_obs A tibble containing `df_name` and a list-column `data`.
#' @param df_new A data.frame containing the new observations to append.
#' @param new_name Character. The name to assign to the new dataframe in the `df_name` column.
#'
#' @return A row-bound tibble of the cleaned `list_obs` and the cleaned `df_new`.
#'
#' @importFrom dplyr select any_of everything mutate bind_rows
#' @importFrom purrr map
#' @importFrom tibble tibble
#' @export
add_to_observed <- function(list_obs, df_new, new_name) {
  
  # 1. Safety checks (targets-friendly)
  stopifnot(
    is.data.frame(list_obs),
    all(c("df_name", "data") %in% names(list_obs)),
    is.data.frame(df_new),
    is.character(new_name),
    length(new_name) == 1
  )
  
  # Prevent silent overwrites
  if (new_name %in% list_obs$df_name) {
    stop("df_name already exists in list_obs: ", new_name)
  }
  
  # 2. Define the cleanup logic as an internal helper
  clean_df <- function(df) {
    df |>
      # Drop exactly these columns (fails safely if they don't exist)
      dplyr::select(-dplyr::any_of(c(
        "Dataset", 
        "SowTime", 
        "Cultivar", 
        "SowingDate", 
        "EmergenceDate"
      ))) |>
      # Reorder to ensure Date and SimulationName are always the first two columns
      dplyr::select(Date, SimulationName, dplyr::everything())
  }
  
  # 3. Apply the cleanup to the NEW dataframe
  df_new_clean <- clean_df(df_new)
  
  # 4. Apply the cleanup to ALL EXISTING nested dataframes in list_obs
  list_obs_clean <- list_obs |>
    dplyr::mutate(
      data = purrr::map(data, clean_df)
    )
  
  # 5. Bind and return
  dplyr::bind_rows(
    list_obs_clean,
    tibble::tibble(
      df_name = new_name,
      data    = list(df_new_clean)
    )
  )
}