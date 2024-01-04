::Zips the dll into the correct directory structure for release
::Make sure to increment the version

set version=0.45
set name=HS2_BetterVR

IF NOT EXIST  "../release/%name%/BepInEx/plugins" MKDIR  "../release/%name%/BepInEx/plugins"
COPY "../bin/%name%/BepinEx/plugins\%name%.dll" "../release/%name%/BepInEx/plugins"

"%ProgramFiles%\7-Zip\7z.exe" a -tzip "%HOMEPATH%/downloads/%name% v%version%.zip" "../release/%name%" -mx0
start %HOMEPATH%/downloads

