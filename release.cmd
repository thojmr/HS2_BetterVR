::Zips the dll into the correct directory structure for release
::Make sure to increment the version

set version=0.1
set name=BetterVR

IF EXIST "./bin/%name%/BepinEx/plugins/%name%.dll" "%ProgramFiles%\7-Zip\7z.exe" a -tzip "%HOMEPATH%/downloads/%name% v%version%.zip" "./bin/%name%/BepinEx" -mx0

start %HOMEPATH%/downloads