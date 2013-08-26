
if exist TEMP rmdir /S /Q TEMP
mkdir TEMP
cd TEMP

git checkout master
git merge develop

cd ..
rmdir /S /Q TEMP