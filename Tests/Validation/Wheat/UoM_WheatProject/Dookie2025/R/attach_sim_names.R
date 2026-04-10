#' Attach Simulation Names to Compiled Observations with Fuzzy Matching
#'
#' @param compiled_obs A nested tibble of observations (output of compile_all_observed).
#' @param df_simNameByCult A mapping dataframe containing Dataset, SowTime, Cultivar, and SimulationName.
#' @return The nested tibble with SimulationName attached to all inner dataframes.
#'
#' @importFrom dplyr mutate inner_join
#' @importFrom purrr map map_chr
#' @importFrom stats setNames
#' @importFrom utils adist
#' @export
attach_sim_names <- function(compiled_obs, df_simNameByCult) {
  
  # 1. Get the "Ground Truth" Cultivars from the master list
  truth_cults <- unique(as.character(df_simNameByCult$Cultivar))
  
  # 2. Get all unique raw Cultivars scattered across the nested dataframes
  raw_cults <- compiled_obs$data |>
    purrr::map(~ unique(as.character(.x$Cultivar))) |>
    unlist() |>
    unique()
  
  # 3. Internal Helper: Robust Matching Logic
  match_cultivar <- function(raw_c, truth_vec) {
    # a. Exact match
    if (raw_c %in% truth_vec) return(raw_c)
    
    # Normalization (lowercase, strip all non-alphanumeric characters)
    norm_raw <- tolower(gsub("[^a-z0-9]", "", raw_c))
    norm_truth <- tolower(gsub("[^a-z0-9]", "", truth_vec))
    
    # b. Normalized exact match (Fixes: "Big_Red" vs "BigRed")
    idx <- which(norm_truth == norm_raw)
    if (length(idx) == 1) return(truth_vec[idx])
    
    # c. Substring match: Is truth inside raw? (Fixes: "Zanzibar" in "RGT_Zanzibar")
    idx_sub <- which(sapply(norm_truth, function(t) grepl(t, norm_raw, fixed = TRUE)))
    if (length(idx_sub) == 1) return(truth_vec[idx_sub])
    
    # d. Substring match: Is raw inside truth?
    idx_sub2 <- which(sapply(norm_truth, function(t) grepl(norm_raw, t, fixed = TRUE)))
    if (length(idx_sub2) == 1) return(truth_vec[idx_sub2])
    
    # e. Fuzzy Match: Minor Typos (Levenshtein distance <= 2)
    dists <- utils::adist(norm_raw, norm_truth)[1, ]
    min_dist <- min(dists)
    if (min_dist <= 2 && sum(dists == min_dist) == 1) {
      return(truth_vec[which.min(dists)])
    }
    
    # Unmatched (Returns original, which will safely be dropped by the inner_join)
    return(raw_c)
  }
  
  # 4. Build the Translation Dictionary
  translation_dict <- stats::setNames(
    purrr::map_chr(raw_cults, ~ match_cultivar(.x, truth_cults)),
    raw_cults
  )
  
  # Identify which cultivars actually had to be translated
  approximations <- translation_dict[names(translation_dict) != translation_dict]
  
  # 5. The LOUD Warning Box
  if (length(approximations) > 0) {
    warning_lines <- c(
      "",
      "======================================================================",
      " ⚠️  WARNING: CULTIVAR NAME APPROXIMATIONS APPLIED ⚠️ ",
      "======================================================================",
      " The following Cultivars in the raw data did not exactly match the",
      " simulation mapping, but were successfully fuzzy-matched:",
      "",
      sprintf(" -> RAW: '%s'  ====>  ACCEPTED: '%s'", names(approximations), approximations),
      "",
      " NOTE: The accepted standard name from df_simNameByCult was applied.",
      "======================================================================",
      ""
    )
    # message() prints the beautiful box to the console
    message(paste(warning_lines, collapse = "\n"))
    
    # warning() ensures targets flags the node as yellow in tar_meta()
    warning("Fuzzy matching applied to Cultivar names. See console output for details.", call. = FALSE)
  }
  
  # 6. Apply mapping and Join
  result <- compiled_obs |>
    dplyr::mutate(
      data = purrr::map(data, function(df) {
        
        # Safely overwrite the Cultivar column with the accepted dictionary name
        df <- df |>
          dplyr::mutate(
            Cultivar = unname(translation_dict[as.character(Cultivar)])
          )
        
        # Now perform the strict join with standardized names
        dplyr::inner_join(
          x = df, 
          y = df_simNameByCult, 
          by = c("Dataset", "SowTime", "Cultivar")
        )
        
      })
    )
  
  return(result)
}