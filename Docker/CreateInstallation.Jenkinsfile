pipeline {
	agent none
    stages {
        stage('Build') {
			agent {
				label "windows && docker && build"
			}
			steps {
				bat '''
					@echo off
					echo.
					echo.
					echo	 ____        _ _     _ 
					echo	^|  _ \\      ^(_^) ^|   ^| ^|
					echo	^| ^|_^) ^|_   _ _^| ^| __^| ^|
					echo	^|  _ ^<^| ^| ^| ^| ^| ^|/ _^` ^|
					echo	^| ^|_^) ^| ^|_^| ^| ^| ^| ^(_^| ^|
					echo	^|____/ \\__,_^|_^|_^|\\__,_^|
					echo.
					echo.
					if not exist ApsimX (
						git config --system core.longpaths true
						git clone https://github.com/APSIMInitiative/ApsimX ApsimX
					)
					cd ApsimX
					git checkout master
					git clean -fxdq
					git pull origin master
					if %errorlevel% neq 0 (
						exit 1
					)
					cd ..
					if not exist APSIM.Shared (
						git clone https://github.com/APSIMInitiative/APSIM.Shared APSIM.Shared
					)
					git -C APSIM.Shared pull origin master
					
					if "%PULL_ID%"=="" (
						echo Error: No issue number provided.
						exit 1
					)
					docker build -m 16g -t buildapsimx ApsimX\\Docker\\build
					docker run -m 16g --cpu-count %NUMBER_OF_PROCESSORS% --cpu-percent 100 -e PULL_ID -v %cd%\\ApsimX:C:\\ApsimX -v %cd%\\APSIM.Shared:C:\\APSIM.Shared buildapsimx
				'''
				archiveArtifacts artifacts: 'ApsimX\\bin.zip', onlyIfSuccessful: true
				archiveArtifacts artifacts: 'ApsimX\\datetimestamp.txt', onlyIfSuccessful: true
			}
		}
		stage('Validation') {
			agent {
				label "windows && docker"
			}
			steps {
				bat '''
					@echo off
					echo.
					echo.
					echo	 __      __   _ _     _       _   _             
					echo	 \\ \\    / /  ^| (_)   ^| ^|     ^| ^| (_)            
					echo	  \\ \\  / /_ _^| ^|_  __^| ^| __ _^| ^|_ _  ___  _ __  
					echo	   \\ \\/ / _` ^| ^| ^|/ _` ^|/ _` ^| __^| ^|/ _ \\^| '_ \\ 
					echo	    \\  / (_^| ^| ^| ^| (_^| ^| (_^| ^| ^|_^| ^| (_) ^| ^| ^| ^|
					echo	     \\/ \\__,_^|_^|_^|\\__,_^|\\__,_^|\\__^|_^|\\___/^|_^| ^|_^|                                                                                            
					echo.
					echo.
					rem We want to copy the build artifacts into ApsimX directory, however this directory may not exist yet.
					if not exist ApsimX (
						git config --system core.longpaths true
						git clone https://github.com/APSIMInitiative/ApsimX ApsimX
					)
					cd ApsimX
					git checkout master
					rem Don't cleanup nuget packages for now....this will be a problem in the long run!!
					git clean -fxdq -e packages
					git pull origin master
					if %errorlevel% neq 0 (
						exit 1
					)
					cd ..
				'''
				copyArtifacts filter: 'ApsimX\\bin.zip', fingerprintArtifacts: true, projectName: 'CreateInstallation', selector: specific('${BUILD_NUMBER}')
				copyArtifacts filter: 'ApsimX\\datetimestamp.txt', fingerprintArtifacts: true, projectName: 'CreateInstallation', selector: specific('${BUILD_NUMBER}')
				bat '''
					@echo off
					if not exist APSIM.Shared (
						git clone https://github.com/APSIMInitiative/APSIM.Shared APSIM.Shared
					)
					git -C APSIM.Shared pull origin master
					set /P DATETIMESTAMP=<ApsimX\\datetimestamp.txt
					docker build -m 16g -t runtests ApsimX\\Docker\\runtests
					docker run -m 16g --cpu-count %NUMBER_OF_PROCESSORS% --cpu-percent 100 -e "DATETIMESTAMP=%DATETIMESTAMP%" -e "PULL_ID=%PULL_ID%" -e "COMMIT_AUTHOR=%COMMIT_AUTHOR%" -e "RUN_PERFORMANCE_TESTS=no_thanks" -v %cd%\\ApsimX:C:\\ApsimX -v %cd%\\APSIM.Shared:C:\\APSIM.Shared runtests Validation
				'''
			}
		}
		stage('CreateInstallations') {
			parallel {
				stage('Documentation') {
					agent {
						label "windows"
					}
					steps {
						bat '''
							@echo off
							rem We want to copy the build artifacts into ApsimX directory, however this directory may not exist yet.
							if not exist ApsimX (
								git config --system core.longpaths true
								git clone https://github.com/APSIMInitiative/ApsimX ApsimX
							)
							call ApsimX\\Docker\\cleanup.bat
						'''
						copyArtifacts filter: 'ApsimX\\bin.zip', fingerprintArtifacts: true, projectName: 'CreateInstallation', selector: specific('${BUILD_NUMBER}')
						bat '''
							@echo off
							if not exist APSIM.Shared (
								git clone https://github.com/APSIMInitiative/APSIM.Shared APSIM.Shared
							)
							git -C APSIM.Shared pull origin master
							call ApsimX\\Documentation\\GenerateDocumentation.bat
						'''
						
					}
				}
				stage('Windows') {
					agent {
						label "windows && docker"
					}
					environment {
						APSIM_SITE_CREDS = credentials('apsim-site-creds')
					}
					steps {
						bat '''
							@echo off
							rem We want to copy the build artifacts into ApsimX directory, however this directory may not exist yet.
							if not exist ApsimX (
								git config --system core.longpaths true
								git clone https://github.com/APSIMInitiative/ApsimX ApsimX
							)
							call ApsimX\\Docker\\cleanup.bat
						'''
						copyArtifacts filter: 'ApsimX\\bin.zip', fingerprintArtifacts: true, projectName: 'CreateInstallation', selector: specific('${BUILD_NUMBER}')
						bat '''
							call ApsimX\\Docker\\CreateInstallations.bat windows
							rem move ApsimX\\Setup\\Output\\APSIMSetup.exe .\\APSIMSetup
						'''
					}
				}
				stage('MacOS') {
					agent {
						label "linux && docker"
					}
					environment {
						APSIM_SITE_CREDS = credentials('apsim-site-creds')
					}
					steps {
						bat '''
							@echo off
							rem We want to copy the build artifacts into ApsimX directory, however this directory may not exist yet.
							if not exist ApsimX (
								git config --system core.longpaths true
								git clone https://github.com/APSIMInitiative/ApsimX ApsimX
							)
							call ApsimX\\Docker\\cleanup.bat
						'''
						copyArtifacts filter: 'ApsimX\\bin.zip', fingerprintArtifacts: true, projectName: 'CreateInstallation', selector: specific('${BUILD_NUMBER}')
						bat '''
							call ApsimX\\Docker\\CreateInstallations.bat macos
						'''
						archiveArtifacts artifacts: 'ApsimX\\Setup\\osx\\ApsimSetup.dmg', onlyIfSuccessful: true
					}
				}
				stage('Linux') {
					agent {
						label "windows && docker"
					}
					environment {
						APSIM_SITE_CREDS = credentials('apsim-site-creds')
					}
					steps {
						bat '''
							@echo off
							rem We want to copy the build artifacts into ApsimX directory, however this directory may not exist yet.
							if not exist ApsimX (
								git config --system core.longpaths true
								git clone https://github.com/APSIMInitiative/ApsimX ApsimX
							)
							call ApsimX\\Docker\\cleanup.bat
						'''
						copyArtifacts filter: 'ApsimX\\bin.zip', fingerprintArtifacts: true, projectName: 'CreateInstallation', selector: specific('${BUILD_NUMBER}')
						bat '''
							call ApsimX\\Docker\\CreateInstallations.bat linux
						'''
						archiveArtifacts artifacts: 'ApsimX\\Setup\\Linux\\APSIMSetup.deb', onlyIfSuccessful: true
					}
				}
			}
		}
		stage('Deploy') {
			agent {
				label "windows"
			}
			steps {
				bat '''
					echo TBI
				'''
			}
		}
	}
}