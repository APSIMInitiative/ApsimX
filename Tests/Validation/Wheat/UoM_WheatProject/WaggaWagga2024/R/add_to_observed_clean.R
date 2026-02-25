add_to_observed_clean <- function(list_observed_clean, df_new, new_name) {
  
  # Safety checks (targets-friendly)
  stopifnot(
    is.data.frame(list_observed_clean),
    all(c("df_name", "data") %in% names(list_observed_clean)),
    is.character(new_name),
    length(new_name) == 1
  )
  
  # Prevent silent overwrites
  if (new_name %in% list_observed_clean$df_name) {
    stop("df_name already exists in list_observed_clean: ", new_name)
  }
  
  dplyr::bind_rows(
    list_observed_clean,
    tibble::tibble(
      df_name = new_name,
      data    = list(df_new)
    )
  )
}
