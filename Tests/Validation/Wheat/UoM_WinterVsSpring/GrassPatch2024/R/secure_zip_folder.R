#' Securely Zip a Folder using 7-Zip
#'
#' @param input_folder The directory to compress.
#' @param output_zip The full path of the output .zip file.
#' @param pass_file The text file containing the password.
#'
#' @export
secure_zip_folder <- function(input_folder, output_zip, pass_file) {
  
  # 1. Check if the password file actually exists
  if (!file.exists(pass_file)) {
    stop("CRITICAL: Password file not found at ", pass_file)
  }
  
  # 2. Read the password string
  secret_pass <- trimws(readLines(pass_file, warn = FALSE)[1])
  
  if (nchar(secret_pass) == 0) {
    stop("CRITICAL: Password file is empty!")
  }
  
  # 3. Clean up the old zip if it exists
  if (file.exists(output_zip)) {
    file.remove(output_zip)
  }
  
  # 4. THE PATHFINDER: Find 7-Zip explicitly
  exe_7z <- Sys.which("7z")
  
  if (exe_7z == "") {
    # If the background worker can't find it, hardcode the Windows path
    fallback_path <- "C:/Program Files/7-Zip/7z.exe"
    if (file.exists(fallback_path)) {
      # Wrap in quotes because of the space in "Program Files"
      exe_7z <- sprintf('"%s"', fallback_path) 
    } else {
      stop("CRITICAL: 7-Zip is not in PATH and not at C:/Program Files/7-Zip/7z.exe")
    }
  } else {
    exe_7z <- "7z"
  }
  
  # 5. Construct the 7-Zip system command using the explicit executable
  cmd <- sprintf(
  #  '%s a -tzip -p"%s" -mem=AES256 "%s" "%s\\*"', # needs 7z installed- - powerEncription
    '%s a -tzip -p"%s" -mem=AES256 "%s" "%s\\*"',
    exe_7z,
    secret_pass, 
    output_zip, 
    input_folder
  )
  
  # 6. Execute the command and capture the exit status
  message("Encrypting archive...")
  sys_result <- system(cmd, intern = TRUE)
  
  # R stores the error code as an attribute. If it succeeded, it is NULL.
  exit_status <- attr(sys_result, "status")
  
  if (!is.null(exit_status) && exit_status != 0) {
    stop(
      "\n========================================================\n",
      "🚨 CRITICAL 7-ZIP ERROR (Status ", exit_status, ") 🚨\n",
      "========================================================\n",
      "7-Zip failed to zip the files. It likely couldn't find the source data.\n",
      "Last lines from 7-Zip:\n  ", 
      paste(tail(sys_result, 5), collapse = "\n  "), "\n",
      "========================================================\n"
    )
  }
  
  # 7. Verify creation AND verify it's not a "Ghost Zip"
  if (!file.exists(output_zip)) {
    stop("CRITICAL: The archive was not created at all.")
  }
  
  # An empty AES-256 zip file wrapper is tiny (usually ~22 to 100 bytes).
  # If it's less than 500 bytes, we know absolutely no data was added.
  zip_size <- file.info(output_zip)$size
  
  if (zip_size < 500) {
    # Delete the empty ghost file so it doesn't trick you later
    file.remove(output_zip)
    
    stop(
      "\n========================================================\n",
      "🚨 GHOST ZIP DETECTED 🚨\n",
      "========================================================\n",
      "The zip file was created, but NO FILES WERE ADDED to it.\n",
      "Double-check that this path actually contains files:\n", 
      " -> ", input_folder, "\n",
      "========================================================\n"
    )
  }
  
  message("✅ Successfully created password-protected archive: ", basename(output_zip))
  return(output_zip)
  message("✅ Successfully created password-protected archive: ", basename(output_zip))
  return(output_zip)
}