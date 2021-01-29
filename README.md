# Introduction
This repository contains the BetterVR plugin for HS2VR, that adds a few enhancements for VR in Main Game mode.  Still a work in progress, and only for Index/Vive users at the moment.

## Features
- Adds colliders to your controllers, so you can boop.
- Adds config option "Laser Pointer Angle" to adjust the angle of the controller laser pointer.
- Adds config option "Squeeze to Turn" to turn the camera when squeezing the grips by turning your wrist


## How to download
You can grab the first plugin release [here](https://github.com/thojmr/BetterVR/releases), or build it yourself.  Explained further below.

## How to install
Almost all plugins are installed in the same way. If there are any extra steps needed they will be added to the plugin descriptions below.
1. Make sure you have at least BepInEx 5.1 and latest BepisPlugins and KKAPI.
2. Download the latest release of the plugin you want.
3. Extract the archive into your game directory. The plugin .dll should end up inside your BepInEx\plugins directory.
4. Check if there are no warnings on game startup, if the plugin has settings it should appear in plugin settings.

## Compiling with Visual Studio 2019 (The official way)
Simply clone this repository to your drive and use the free version of Visual Studio 2019 for C# to compile it. Hit build and all necessary dependencies should be automatically downloaded. Check the following links for useful tutorials. If you are having trouble or want to try to make your own plugin/mod, feel free to ask for help in modding channels of either the [Koikatsu](https://discord.gg/hevygx6) or [IllusionSoft](https://discord.gg/F3bDEFE) Discord servers.
- https://help.github.com/en/github/creating-cloning-and-archiving-repositories/cloning-a-repository
- https://docs.microsoft.com/en-us/visualstudio/get-started/csharp/?view=vs-2019
- https://docs.microsoft.com/en-us/visualstudio/ide/troubleshooting-broken-references?view=vs-2019

## Compiling with Visual Studio Code (Not the suggested way, but my way)
Simply clone this repository to your drive and use Visual Studio Code.  
Install the C# extension for VSCode. 
Make sure the following directory exists `C:/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Current/Bin/msbuild.exe`.  If not you will need to install the VS2019 MS build tools (There may be other ways to build, but this is the one that eventually worked for me)
Install nuget.exe and set the environment path to it. 
Then use `nuget install -OutputDirectory ../packages` to install the dependancies from the \BetterVR\ directory.  
You will need to grab the VR version of Assembly-CSharp.dll, and SteamVR.dll from the HS2 game directory as well. The standard Assembly-CSharp.dll does not include the HS2VR class.
Finally create a build script with tasks.json in VSCode.
Example build task:
```json
{
    "label": "build-BetterVR",
    "command": "C:/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Current/Bin/msbuild.exe",
    "type": "process",
    "args": [
        "${workspaceFolder}/BetterVR/BetterVR.csproj",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
    ],
    "presentation": {
        "reveal": "silent"
    },
    "problemMatcher": "$msCompile",
},
{
    "label": "build-and-copy",
    "type": "shell",
    "command": "cp ./bin/HS2_BetterVR.dll '<HS2_Install_DIR>/BepInEx/plugins/'",
    "dependsOn": "build-BetterVR",
    "group": {
        "kind": "build",
        "isDefault": true
    },
    "presentation": {
        "echo": true,
        "reveal": "silent",
        "focus": false,
        "panel": "shared",
        "showReuseMessage": true,
        "clear": false
    }
}
```
If sucessfull you should see a HS2_BetterVR.dll file in .\bin\
