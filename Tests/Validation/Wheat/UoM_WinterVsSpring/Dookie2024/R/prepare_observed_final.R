#' Prepare Final Observed Data for APSIM
#'
#' @description
#' Compiles a nested list of cleaned observation dataframes into a single, master 
#' dataframe ready for APSIM ingestion. 
#'
#' @details
#' Utilizes a stack-then-crush approach to perfectly align variables and completely 
#' avoid .x and .y column mutations caused by full_join overlapping data.
#'
#' @param list_observed_clean A tibble containing a `data` list-column.
#' @return A flattened tibble containing `SimulationName`, `Clock.Today`, etc.
#'
#' @importFrom dplyr pull bind_rows mutate select everything group_by summarise across if_else filter contains starts_with first
#' @importFrom purrr walk
#' @importFrom stats na.omit
#' @export
prepare_observed_final <- function(list_observed_clean) {
  
  require(dplyr)
  require(purrr)
  
  # ------------------------------------------------------------------
  # 1. STRICT SAFETY CHECKS
  # ------------------------------------------------------------------
  purrr::walk(list_observed_clean$data, function(df) {
    if (!all(c("SimulationName", "Date") %in% names(df))) {
      stop("Validation Failed: Missing 'SimulationName' or 'Date'.")
    }
  })
  
  # ------------------------------------------------------------------
  # 2. STACK AND AGGREGATE (The .x / .y Fix)
  # ------------------------------------------------------------------
  df_list <- list_observed_clean %>%
    dplyr::pull(data)
  
  # Step A: Stack everything. bind_rows NEVER creates .x or .y columns.
  # It aligns identical column names, temporarily creating staggered NA rows.
  df_stacked <- dplyr::bind_rows(df_list)
  
  # Step B: Crush the staggered rows down into a perfect 1-to-1 grid.
  # This mathematically merges any overlapping data seamlessly.
  df_crushed <- df_stacked %>%
    dplyr::group_by(SimulationName, Date) %>%
    dplyr::summarise(
      dplyr::across(
        where(is.numeric), 
        ~ mean(.x, na.rm = TRUE)
      ),
      dplyr::across(
        where(is.character),
        ~ dplyr::first(stats::na.omit(.x))
      ),
      .groups = "drop"
    ) %>%
    # Replace NaN (created by taking the mean of all NAs) back to NA
    dplyr::mutate(
      dplyr::across(where(is.numeric), ~ dplyr::if_else(is.nan(.x), NA_real_, .x))
    )
  
  # ------------------------------------------------------------------
  # 3. FORMAT FOR APSIM & STRICT CLEANUP
  # ------------------------------------------------------------------
  df_final <- df_crushed %>%
    
    # A. FIREWALL: Drop any row that doesn't have a valid Date
    dplyr::filter(!is.na(Date)) %>%
    
    # B. Format to APSIM standard
    dplyr::mutate(
      Clock.Today = format(as.Date(Date), "%Y-%m-%d")
    ) %>%
    
    # C. PURGE GARBAGE COLUMNS:
    # - Drop bleeding APSIM params (like [Wheat].Leaf.StemPopulation)
    # - Drop DateToProgress
    # - Drop the empty Excel ghost columns ("...10")
    dplyr::select(
      -dplyr::contains("[Wheat]"),
      -dplyr::contains("DateToProgress"),
      -dplyr::starts_with("...")
    ) %>%
    
    # D. Reorder perfectly: SimulationName, Clock.Today, then everything else
    dplyr::select(SimulationName, Clock.Today, dplyr::everything(), -Date)
  
  return(df_final)
}