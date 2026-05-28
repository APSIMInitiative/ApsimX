#' Interpolate and Format Wheat Phenology Dates for APSIM-X
#'
#' @description
#' This function takes observed wheat phenology dates, deduplicates them, 
#' interpolates missing stages (Stage 4 and Stage 7) based on a fractional 
#' progress value, and formats dates for Excel export.
#'
#' @param df A data frame containing at least `SimulationName`, `PhenoDate`, 
#'   and `Wheat.Phenology.Stage`.
#' @param btwStgPerc Numeric (0-1). The fractional progress to interpolate 
#'   between adjacent stages (e.g., 0.5 for a midpoint).
#'
#' @return A wide-format data frame with metadata and APSIM-formatted 
#'   character dates ("dd-mm-yyyy").
#' 
#' @importFrom dplyr select group_by summarise mutate across starts_with rename if_else
#' @importFrom tidyr pivot_wider
#' @export
add_interp_pheno_dates <- function(df, btwStgPerc) {
  
  require(dplyr)
  require(tidyr)
  
  # 1. Clean, Deduplicate, and Pivot Wide
  df_wide <- df %>%
    dplyr::select(SimulationName, Wheat.Phenology.Stage, PhenoDate) %>%
    dplyr::group_by(SimulationName, Wheat.Phenology.Stage) %>%
    dplyr::summarise(PhenoDate = max(PhenoDate, na.rm = TRUE), .groups = "drop") %>%
    dplyr::mutate(Wheat.Phenology.Stage = paste0("Stage_", Wheat.Phenology.Stage)) %>%
    tidyr::pivot_wider(
      names_from = Wheat.Phenology.Stage, 
      values_from = PhenoDate
    )
  
  # ------------------------------------------------------------------
  # NEW: SAFETY NET FOR MISSING COLUMNS
  # ------------------------------------------------------------------
  # Ensure all expected columns exist so the mutate math doesn't crash
  expected_cols <- c("Stage_3", "Stage_6", "Stage_8", "Stage_10")
  for (col in expected_cols) {
    if (!col %in% names(df_wide)) {
      # If the column is missing, create it and fill it with Date NAs
      df_wide[[col]] <- as.Date(NA)
    }
  }
  
  # 2. Perform Interpolation and Format Dates Natively
  df_interp <- df_wide %>%
    dplyr::mutate(
      # Stage 4 is x% between Stage 3 and Stage 6 (safely resolves to NA if components are missing)
      Stage_4 = Stage_3 + (as.numeric(Stage_6 - Stage_3) * btwStgPerc),
      
      # Stage 7 is x% between Stage 6 and Stage 8
      Stage_7 = Stage_6 + (as.numeric(Stage_8 - Stage_6) * btwStgPerc)
    ) %>%
    # REFORMAT STEP: Format all Stage columns in place
    dplyr::mutate(
      dplyr::across(
        dplyr::starts_with("Stage_"),
        # if_else ensures NAs stay as true missing values, instead of the text string "NA"
        ~ dplyr::if_else(is.na(.x), NA_character_, format(.x, "%d-%m-%Y"))
      )
    )
  
  
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
    dplyr::select(
      SimulationName,
      `[Wheat].Phenology.Emerging.DateToProgress`,
      `[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress`,
      `[Wheat].Phenology.StemElongating.DateToProgress`,
      `[Wheat].Phenology.Heading.DateToProgress`,
      `[Wheat].Phenology.Flowering.DateToProgress`,
      `[Wheat].Phenology.GrainFilling.DateToProgress`
    )
  
  return(df_renamed)
}