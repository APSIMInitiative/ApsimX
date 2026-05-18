#' Apply Date Corrections to Nested Biomass Dataframes
#'
#' @description
#' This function iterates through a nested tibble of raw datasets and updates 
#' missing or incorrect dates for specific biomass variables based on a 
#' reference observational dataset (`df_stages_Observ`). It replaces dates for 
#' Group 6 and Group 8 variables with their respective phenological stage dates.
#'
#' @param df_tbl A nested data.frame or tibble containing a `df_name` character 
#'   column and a `data` list-column containing the raw dataframes.
#' @param df_stages_Observ A data.frame containing phenological stage observations. 
#'   Must contain `Cultivar`, `Date`, and `Wheat.Phenology.Stage`.
#'
#' @return A nested tibble with updated `Date` columns in the specified dataframes.
#'
#' @importFrom dplyr filter select mutate left_join any_of
#' @importFrom tidyr pivot_wider
#' @importFrom purrr map2
#' @export
apply_corrections <- function(df_tbl, df_stages_Observ) {
  
  df_tbl<-df2
  df_stages_Observ<-df3
  
  # 1. Package Checks (Defensive Programming)
  if (!requireNamespace("lubridate", quietly = TRUE)) stop("Package 'lubridate' is required.")
  if (!requireNamespace("dplyr", quietly = TRUE)) stop("Package 'dplyr' is required.")
  if (!requireNamespace("tidyr", quietly = TRUE)) stop("Package 'tidyr' is required.")
  if (!requireNamespace("purrr", quietly = TRUE)) stop("Package 'purrr' is required.")
  
  # 2. Define target variables (Keeps the mapping logic below clean)
  group_6_vars <- c("stemYield_6_raw", "spikeYield_6_raw", "senescLeafYield_6_raw", 
                    "totalAboveGround_6_raw", "par_6_raw", "greenLeaf_6_raw")
  
  group_8_vars <- c("stemYield_8_raw", "spikeYield_8_raw", "senescLeafYield_8_raw", 
                    "totalAboveGround_8_raw", "par_8_raw", "greenLeaf_8_raw")
  
  # 3. Create the Date Lookup Table (Wide Format)
  date_lookup <- df_stages_Observ %>%
    dplyr::filter(Wheat.Phenology.Stage %in% c(6, 8)) %>%
    dplyr::select(SimulationName, Date, Wheat.Phenology.Stage) %>%
    dplyr::mutate(PhenoDate = paste0("PhenoDate_", Wheat.Phenology.Stage)) %>%
    dplyr::select(-Wheat.Phenology.Stage) %>%
    tidyr::pivot_wider(names_from = PhenoDate, values_from = Date) # Modern replacement for spread()
  
  # Sanity check for expected columns
  if (!all(c("PhenoDate_6", "PhenoDate_8") %in% names(date_lookup))) {
    stop("Lookup table is missing 'PhenoDate_6' or 'PhenoDate_8'. Check observation data.")
  }
  
  # 4. Apply corrections using purrr::map2
  df_tbl <- df_tbl %>%
    dplyr::mutate(
      data = purrr::map2(
        .x = data,
        .y = df_name,
        .f = function(df, nm) {
          
          # Check if the current dataframe needs date corrections
          if (nm %in% c(group_6_vars, group_8_vars)) {
            
            # Perform the join once
            df <- df %>%
              dplyr::left_join(date_lookup, by = "SimulationName")
            
            # Apply the specific date based on the group mapping
            if (nm %in% group_6_vars) {
              df <- df %>% dplyr::mutate(Date = PhenoDate_6)
            } else if (nm %in% group_8_vars) {
              df <- df %>% dplyr::mutate(Date = PhenoDate_8)
            }
            
            # Clean up the temporary lookup columns safely
            df <- df %>%
              dplyr::select(-dplyr::any_of(c("PhenoDate_6", "PhenoDate_8")))
          }
          
          return(df)
        }
      )
    )
  
  return(df_tbl)
}