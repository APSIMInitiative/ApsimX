#' Interpolate Dates for Missing Phenology Stages
#'
#' Pivots observed stages to a wide format, calculates dates for stages 4 and 7
#' based on user-defined percentages, and pivots back to a tidy long format.
#'
#' @param df_pheno_start_date Dataframe. Contains known observed stages in long format.
#' @param btwStgFrac List. Fractions used for interpolation (e.g., list(stage4 = 0.4, stage7 = 0.6)).
#' @return A dataframe (tibble) containing both observed and interpolated stages.
#' @export
interpolate_and_create_phenoStages <- function(df_pheno_start_date, btwStgFrac) {
  
  # 1. Pivot to wide format for easy date math
  df_wide <- df_pheno_start_date %>%
    # Keep only the columns we need to pivot
    dplyr::select(SimulationName, VarValue, Clock.Today) %>%
    tidyr::pivot_wider(
      names_from = VarValue,
      names_prefix = "Stage_",
      values_from = Clock.Today
    )
  
  
  # 2. Perform the date interpolation
  df_interp <- df_wide %>%
    dplyr::mutate(
      # Stage 4 falls between Stage 3 and Stage 6
      Stage_4 = Stage_3 + (as.numeric(Stage_6 - Stage_3) * btwStgFrac/(3*btwStgFrac)), # conan-the-barbarian style interp (FIXME) ;-)
      
      # Stage 7 falls between Stage 6 and Stage 8
      Stage_7 = Stage_6 + (as.numeric(Stage_8 - Stage_6) * btwStgFrac/(2*btwStgFrac))
    )
  
  # 3. Pivot back to long format, clean up, and sort
  df_final <- df_interp %>%
    tidyr::pivot_longer(
      cols = tidyselect::starts_with("Stage_"),
      names_to = "VarValue",
      names_prefix = "Stage_",
      values_to = "Clock.Today",
      values_drop_na = TRUE # Drops stages if some sims were missing observed bounds
    ) %>%
    dplyr::mutate(
      # Ensure data types are correct after pivoting
      VarValue = as.numeric(VarValue),
      Clock.Today = as.Date(Clock.Today),
      VarName = "Wheat.Phenology.Stage",
      
      # 4. Map numeric stages to their APSIM parameter names
      ParamName = dplyr::case_match(
        VarValue,
        3 ~ "[Wheat].Phenology.Emerging.DateToProgress",
        4 ~ "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress",
        6 ~ "[Wheat].Phenology.StemElongating.DateToProgress",
        7 ~ "[Wheat].Phenology.Heading.DateToProgress",
        8 ~ "[Wheat].Phenology.Flowering.DateToProgress",
        .default = NA_character_ # Fills with NA if there are other stages (like Stage 1)
      )
    ) %>%
    na.omit() %>%
    dplyr::arrange(SimulationName, Clock.Today)
  
  
  
  return(df_final)
}