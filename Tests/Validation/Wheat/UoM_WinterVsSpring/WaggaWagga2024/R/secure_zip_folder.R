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
  
  # 3. STRICT CLEANUP: Delete the old zip if it exists
  if (file.exists(output_zip)) {
    # Attempt to delete and store the result (TRUE/FALSE)
    was_deleted <- file.remove(output_zip)
    
    # If Windows refused to let R delete it, crash the pipeline and warn the user
    if (!was_deleted) {
      stop(
        "\n========================================================\n",
        "🚨 WINDOWS FILE LOCK DETECTED 🚨\n",
        "R cannot delete the old '", basename(output_zip), "'.\n",
        "It is currently locked by another program.\n\n",
        "HOW TO FIX:\n",
        "1. Close any open Excel files.\n",
        "2. Close the 7-Zip File Manager.\n",
        "3. Click away from the file in Windows File Explorer.\n",
        "========================================================\n"
      )
    }
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
  
  # 6. Execute the command quietly
  message("Encrypting archive...")
  sys_result <- system(cmd, intern = TRUE, ignore.stderr = TRUE)
  
  # 7. Verify creation
  if (!file.exists(output_zip)) {
    stop("Failed to create encrypted archive. Check 7-Zip installation and paths.")
  }
  
  message("✅ Successfully created password-protected archive: ", basename(output_zip))
  return(output_zip)
}