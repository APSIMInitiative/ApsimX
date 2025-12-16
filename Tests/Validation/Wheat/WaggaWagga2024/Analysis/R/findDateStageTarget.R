findDateStageTarget <- function(df_list_PCDS, StageTargetPerc) {
  
  stopifnot(is.list(df_list_PCDS))
  stopifnot(length(StageTargetPerc) == 1)
  
  require(dplyr)
  
  results <- vector("list", length(df_list_PCDS))
  nm_list <- names(df_list_PCDS)
  
  for (i in seq_along(df_list_PCDS)) {
    
    nm <- nm_list[[i]]
    df <- df_list_PCDS[[i]]
    
    # identify the progress variable
    value_col <- names(df)[grepl("DateToProgress", names(df))]
    
    if (length(value_col) != 1) {
      stop("Expected exactly one DateToProgress column in ", nm)
    }
   
    # extract stage name (clean but robust)
    StageName <- value_col[[1]]
    
    # find first date reaching target
    res <- df %>%
      arrange(Cultivar, Date) %>%
      group_by(Cultivar) %>%
      mutate(
        max_value    = max(.data[[value_col]], na.rm = TRUE),
        target_value = max_value * StageTargetPerc / 100
      ) %>%
      filter(.data[[value_col]] >= target_value) %>%
      slice_min(Date, n = 1, with_ties = FALSE) %>%
      ungroup() %>%
      transmute(
        Cultivar        = Cultivar,
        StageName       = StageName,
        TargetPerc      = StageTargetPerc,
        Maxvalue        = max_value,
        TargetValue     = target_value,
        DateReached     = Date
      )
    
    results[[i]] <- res
  }
  
  bind_rows(results)
}
