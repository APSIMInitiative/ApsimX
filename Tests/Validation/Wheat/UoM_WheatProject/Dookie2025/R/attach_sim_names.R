#' Attach Simulation Names to Compiled Observations
#'
#' @param compiled_obs A nested tibble of observations (output of compile_all_observed).
#' @param df_simNameByCult A mapping dataframe containing Dataset, SowTime, Cultivar, and SimulationName.
#' @return The nested tibble with SimulationName attached to all inner dataframes.
#' 
#' @importFrom dplyr mutate inner_join
#' @importFrom purrr map
#' @export
attach_sim_names <- function(compiled_obs, df_simNameByCult) {
  
  result <- compiled_obs |>
    dplyr::mutate(
      # Map over the 'data' list-column and apply the join to each dataframe inside
      data = purrr::map(data, function(df) {
        
        dplyr::inner_join(
          x = df, 
          y = df_simNameByCult, 
          by = c("Dataset", "SowTime", "Cultivar")
        )
        
      })
    )
  
  return(result)
}