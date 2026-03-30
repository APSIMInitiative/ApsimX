#' Interpolate and Format Wheat Phenology Dates for APSIM-X
#'
#' @description
#' This function takes observed wheat phenology dates, interpolates missing stages 
#' (Stage 4 and Stage 7) based on a fractional progress value, renames columns 
#' to match APSIM-X parameter paths, and formats dates for Excel export.
#'
#' @param df A data frame containing at least `SimulationName`, `PhenoDate`, 
#' and `Wheat.Phenology.Stage`.
#' @param btwStgPerc Numeric (0-1). The fractional progress to interpolate 
#' between adjacent stages (e.g., 0.5 for a midpoint).
#'
#' @return A wide-format data frame with metadata and APSIM-formatted 
#' character dates ("dd-mm-yyyy").
#' 
#' @export
#'
#' @examples
#' # add_interp_pheno_dates(obs_data, btwStgPerc = 0.5)


add_interp_pheno_dates <- function(df, btwStgPerc) {
  
  df_wide <- df %>%
    dplyr::select(-PhenoEvent) %>%
    mutate(Wheat.Phenology.Stage = paste0("Stage_",Wheat.Phenology.Stage)) %>%
    tidyr::pivot_wider(names_from = Wheat.Phenology.Stage, 
                       values_from = PhenoDate)
  
  
  # 2. Perform Interpolation
  # Stage 4 is 50% between Stage 3 and Stage 6
  # Stage 7 is 50% between Stage 6 and Stage 8
  # format as per apsim needs
  df_interp <- df_wide %>%
    mutate(
      Stage_4 = Stage_3 + (as.numeric(Stage_6 - Stage_3) * btwStgPerc),
      Stage_7 = Stage_6 + (as.numeric(Stage_8 - Stage_6) * btwStgPerc)
    ) %>%
    pivot_longer(
      cols = contains("Stage_"), # Selects all columns with the string "_score"
      names_to = "PhenoStage",      # Name of the new 'key' column
      values_to = "PhenoDate"        # Name of the new 'value' column
    ) %>%
    # REFORMAT STEP: Convert to dd-mm-yyyy string
    mutate(PhenoDate = format(PhenoDate, "%d-%m-%Y")) %>%
    tidyr::pivot_wider(names_from = PhenoStage, 
                       values_from = PhenoDate)
  
  # 3. Rename to APSIM ParameterInputNames and order
  df_renamed <- df_interp %>%
    dplyr::rename(
      `[Wheat].Phenology.Emerging.DateToProgress`                 = Stage_3,
      `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress` = Stage_4,
      `[Wheat].Phenology.StemElongating.DateToProgress`           = Stage_6,
      `[Wheat].Phenology.Heading.DateToProgress`                  = Stage_7,
      `[Wheat].Phenology.Flowering.DateToProgress`                = Stage_8,
      `[Wheat].Phenology.GrainFilling.DateToProgress`             = Stage_10
    ) %>%
    dplyr::select(SimulationName,
                  `[Wheat].Phenology.Emerging.DateToProgress`,
                  `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress`,
                  `[Wheat].Phenology.StemElongating.DateToProgress`,
                  `[Wheat].Phenology.Heading.DateToProgress`,
                  `[Wheat].Phenology.Flowering.DateToProgress`,
                  `[Wheat].Phenology.GrainFilling.DateToProgress`
                  # Note: Stage_10 does not exist in APSIM so it was not included
                  )
  
  
  
  return(df_renamed)
}