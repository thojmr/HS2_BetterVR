::Zips the dll into the correct directory structure for release
::Make sure to increment the version

set version=0.53
set name=HS2_BetterVR

IF NOT EXIST  "../releases/%name%/BepInEx/plugins" MKDIR  "../releases/%name%/BepInEx/plugins"
COPY "../bin/%name%/BepinEx/plugins\%name%.dll" "../releases/%name%/BepInEx/plugins"

IF EXIST "%ProgramFiles%\7-Zip\7z.exe" "%ProgramFiles%\7-Zip\7z.exe" a -tzip "../releases/%name%.v%version%.zip" "../releases/%name%" -mx0
IF EXIST "C:\Program Files\7-Zip\7z.exe" "C:\Program Files\7-Zip\7z.exe" a -tzip "../releases/%name%.v%version%.zip" "../releases/%name%" -mx0

start .\..\releases
