#!/bin/bash
export apsimx=/ApsimX
export setup=$apsimx/Setup
export osx=$setup/osx
pushd $osx > /dev/null

# Delete any .dmg files leftover from previous builds.
find $setup -name "*.dmg" -exec rm "{}" \;

if [ -f $apsimx/bin.zip ]; then
	unzip -o $apsimx/bin.zip -d $apsimx/bin
	rm -f $apsimx/bin.zip
fi

# Delete all files from DeploymentSupport/Windows which may exist in Bin directory.
for f in $(find $apsimx/DeploymentSupport/Windows -name '*'); 
do 
	echo Deleting $apsimx/Bin/$(basename -- $f);
	rm -rf $apsimx/Bin/$(basename -- $f)
done

export version=$(mono $apsimx/Bin/Models.exe /Version | grep -oP '(\d+\.){3}\d+')
export short_version=$(echo $version | cut -d'.' -f 1,2)
export issue_id=$(echo $version | cut -d'.' -f 4)
echo Apsim version: $version
echo Short version: $short_version
echo Issue number: 	$issue_id


if [ -d ./MacBundle ]; then
	rm -rf ./MacBundle
fi

mkdir MacBundle
mkdir ./MacBundle/APSIM$version.app
mkdir ./MacBundle/APSIM$version.app/Contents
mkdir ./MacBundle/APSIM$version.app/Contents/MacOS
mkdir ./MacBundle/APSIM$version.app/Contents/Resources
mkdir ./MacBundle/APSIM$version.app/Contents/Resources/Bin

dos2unix ./Template/Contents/MacOS/ApsimNG
cp ./Template/Contents/MacOS/ApsimNG ./MacBundle/APSIM$version.app/Contents/MacOS/ApsimNG
cp ./Template/Contents/Resources/ApsimNG.icns ./MacBundle/APSIM$version.app/Contents/Resources/ApsimNG.icns
cp -rf $apsimx/Examples ./MacBundle/APSIM$version.app/Contents/Resources/Examples
cp -rf $apsimx/ApsimNG/Resources/world ./MacBundle/APSIM$version.app/Contents/Resources/ApsimNG/Resources/world
cp -rf $apsimx/Tests/UnderReview ./MacBundle/APSIM$version.app/Contents/Resources/UnderReview
cp -f $apsimx/Bin/*.dll ./MacBundle/APSIM$version.app/Contents/Resources/Bin
cp -f $apsimx/Bin/*.exe ./MacBundle/APSIM$version.app/Contents/Resources/Bin
cp -f $apsimx/ApsimNG/Assemblies/Mono.TextEditor.dll.config ./MacBundle/APSIM$version.app/Contents/Resources/Bin/
cp -f $apsimx/ApsimNG/Assemblies/webkit-sharp.dll ./MacBundle/APSIM$version.app/Contents/Resources/Bin/
cp -f $apsimx/Bin/Models.xml ./MacBundle/APSIM$version.app/Contents/Resources/Bin/
cp -f $apsimx/APSIM.Documentation/Resources/APSIM.bib ./MacBundle/APSIM$version.app/Contents/Resources/

export PLIST_FILE=./MacBundle/APSIM$version.app/Contents/Info.plist
(
echo "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
echo "<!DOCTYPE plist PUBLIC \"-//Apple Computer//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">"
echo "<plist version=\"1.0\">"
echo "<dict>"
echo "<key>CFBundleDevelopmentRegion</key>"
echo "<string>English</string>"
echo "<key>CFBundleExecutable</key>"
echo "<string>ApsimNG</string>"
echo "<key>CFBundleIconFile</key>"
echo "<string>ApsimNG</string>"
echo "<key>CFBundleIdentifier</key>"
echo "<string>au.csiro.apsim.apsimx</string>"
echo "<key>CFBundleInfoDictionaryVersion</key>"
echo "<string>6.0</string>"
echo "<key>CFBundlePackageType</key>"
echo "<string>APPL</string>"
echo "<key>CFBundleSignature</key>"
echo "<string>xmmd</string>"
echo "<key>NSAppleScriptEnabled</key>"
echo "<string>NO</string>"
) > $PLIST_FILE
echo "<key>CFBundleName</key>" >> $PLIST_FILE
echo "<string>APSIM$version</string>" >> $PLIST_FILE
echo "<key>CFBundleVersion</key>" >> $PLIST_FILE
echo "<string>"$version"</string>" >> $PLIST_FILE
echo "<key>CFBundleShortVersionString</key>" >> $PLIST_FILE
echo "<string>"$SHORT_VERSION"</string>" >> $PLIST_FILE
echo "</dict>" >> $PLIST_FILE
echo "</plist>" >> $PLIST_FILE

genisoimage -V APSIM$version -D -R -apple -no-pad -file-mode 755 -dir-mode 755 -o ApsimSetup.dmg MacBundle
if [ $? -ne 0 ]; then
	echo Errors encountered!
	exit $?
fi
mv $osx/ApsimSetup.dmg $osx/ApsimSetup$issue_id.dmg
echo Uploading $osx/ApsimSetup$issue_id.dmg
curl -u $APSIM_SITE_CREDS -T $osx/ApsimSetup$issue_id.dmg ftp://www.apsim.info/APSIM/ApsimXFiles/

export err_code=$?
popd > /dev/null
echo Done.
exit $err_code