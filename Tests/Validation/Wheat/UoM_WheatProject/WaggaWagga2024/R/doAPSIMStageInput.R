doAPSIMStageInput <- function(df_dateStageTargetReached,
                              df_simNameByCult,
                              BtwStgPerc) {
  
  require(dplyr)
  require(tidyr)
  require(lubridate)
  
  frac <- BtwStgPerc / 100
  
  # ------------------------------------------------------------------
  # (i) Attach SimulationName
  # ------------------------------------------------------------------
  df <- df_dateStageTargetReached %>%
    left_join(df_simNameByCult, by = "Cultivar")
  
  # ------------------------------------------------------------------
  # (ii) Spread stages into columns
  # ------------------------------------------------------------------
  df_wide <- df %>%
    select(SimulationName, StageName, DateReached) %>%
    tidyr::pivot_wider(
      names_from  = StageName,
      values_from = DateReached
    )
  
  # ------------------------------------------------------------------
  # Column names (explicit and readable)
  # ------------------------------------------------------------------
  col_emerg  <- "[Wheat].Phenology.Emerging.DateToProgress"
  col_stem   <- "[Wheat].Phenology.StemElongating.DateToProgress"
  col_flower <- "[Wheat].Phenology.Flowering.DateToProgress"
  
  col_spikelets <- "[Wheat].Phenology.SpikeletsDifferentiating.DateToProgress"
  col_heading   <- "[Wheat].Phenology.Heading.DateToProgress"
  
  # ------------------------------------------------------------------
  # (iii) Create synthetic stages
  # ------------------------------------------------------------------
  df_wide <- df_wide %>%
    mutate(
      !!col_spikelets :=
        .data[[col_emerg]] +
        (.data[[col_stem]] - .data[[col_emerg]]) * frac,
      
      !!col_heading :=
        .data[[col_stem]] +
        (.data[[col_flower]] - .data[[col_stem]]) * frac
    )
  
  # ------------------------------------------------------------------
  # (iv) Enforce correct column order
  # ------------------------------------------------------------------
  df_final <- df_wide %>%
    select(
      SimulationName,
      all_of(col_emerg),
      all_of(col_spikelets),
      all_of(col_stem),
      all_of(col_heading),
      all_of(col_flower)
    )
  
  # ------------------------------------------------------------------
  # (v) Sanity check: dates must increase
  # ------------------------------------------------------------------
  date_cols <- names(df_final)[-1]
  
  bad_rows <- df_final %>%
    rowwise() %>%
    mutate(
      ok = all(diff(as.numeric(c_across(all_of(date_cols)))) >= 0)
    ) %>%
    ungroup() %>%
    filter(!ok)
  
  if (nrow(bad_rows) > 0) {
    warning("Some SimulationNames have non-monotonic stage dates")
  }
  
  return(df_final)
}
