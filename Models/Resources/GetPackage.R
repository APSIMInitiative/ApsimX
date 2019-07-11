#' Loads a package, installing it if necessary.
#'
#' @param pkg Name of the package to be installed/loaded.
#' @param pkgpath Path where package is to be installed.
#' @return Nothing.
getPackage <- function(pkg, pkgpath) {
    if (!pkg %in% rownames(installed.packages())) {
		i = which(!grepl("Program Files", .libPaths()))[1]
		location = ""
        if (is.na(i) || i > length(.libPaths()) || identical(i, integer(0))) {
		    # No lib paths outside of program files exist....
			location = pkgpath
		} else {
			location = .libPaths()[i]
		}
		print(paste('Installing package', pkg, 'to location', location))
		install.packages(pkg,repos = "https://cran.csiro.au/", lib = location)
	} else {
		print(paste('Package', pkg, 'is already installed.'))
	}
}

args = commandArgs(TRUE)
getPackage(args[1], args[2])

