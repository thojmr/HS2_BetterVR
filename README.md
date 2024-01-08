# Introduction
Forked from thojmr/HS2_BetterVR, this plugin for `Honey Select 2 VR` fixes a handful of bugs and add numerous motion control features and quality-of-life improvements.

</br>

## Features
- Adds colliders to controller (so you can boop), headset, toy, and floor
- Adds config option `Squeeze to Turn` to turn the camera when squeezing the grips and triggers and rotating your wrists.
    - One-handed mode: hold trigger and grip and rotate hand
    - Two-handed mode: hold both grips and move hands like turning a wheel
- Adds config option `Fix World Scale` to change the world scale.
    - Can adjust the scale in config options
    - Can also hold both grips and both triggers and stretch to adjust the scale
- Adds config option to allow vertical rotation.
    - `View reset` option in the radial menu will reset vertical rotation too
- Adds a `Random` button to the Character selection screen that will select a random female/male, and start the HScene.
    - You can select a map, and then hit `random` in the UI or return/enter on the keyboard to use a specific map with random characters
    - (May not work in some cases) The config option `Multiple Heroine when Random`: will add two random heroine to the HScene
- Adds radial menu with quick actions to recenter view, toggle player visibility, move onto next H stage, etc.
    - Long press A or X to activate radial menu
    - Move hand to select quick actions
    - Press trigger or close radial menu to perform quick actions
- Adds feature of using hand movement to dress/undress.
    - Press down trigger close to cloth, hold trigger and drag away to undress
    - Press down trigger some distance away from character, hold trigger and drag onto character to dress
- Adds option to display VR controllers as hands/gloves in VR.
    - If enabled, use radial menu quick action to start adjusting VR hand pose of the other hand
    - Hold either grip to pause adjustment and have the VR glove move with controller
    - Press either trigger to finish adjustment
- Adds a hand-held toy that can be toggled on in the radial menu
    - Use radial menu to toggle it again to change it into silhouette mode
    - When holding it, press A or X to attach to it to body (approximately using camera position)
- Adds feature of using hand movement to adjust H speed.
    - Start H animation regularly using controller pad/stick first
    - Move hand, mouth, or toy close to certain body parts and start moving
    - Haptic feedback (if enabled) indicates that the this feature is in action
    - Look for a heart icon that may show up which indicates pleasure gauge hit
- Adds option to skip title scene on game start and go straight to select scene.
- Adds option to unlock all positions regardless of character state.
- Adds option to tilt VR laser pointers up or down. 
- Fixes the bug of vanilla game not detecting thumb stick input on some platforms.
- Fixes the bug of vanilla game resets camera when changing animation even if the camera initialization option is toggled off.
- Fixes the non-interactable silhouette palette in game settings.
- Fixes the bug that all animations are frozen after opening mod config dialog and closing game settings dialog sometimes.


## How to install
Almost all plugins are installed in the same way. If there are any extra steps needed they will be added to the plugin descriptions below.
1. Make sure you have at least BepInEx 5.1 and latest BepisPlugins and KKAPI (Any BetterRepack will do).
2. Download the latest release of the plugin you want [here](https://github.com/thojmr/BetterVR/releases).
3. Extract the archive into your game directory. The file HS2_BetterVR.dll should end up in \BepInEx\plugins\ directory.
4. Check if there are no warnings on game startup, if the plugin has settings it should appear in plugin settings.

## Compiling with Visual Studio 2019 (The official way)
<details>
  <summary>Click to expand</summary>
Simply clone this repository to your drive and use the free version of Visual Studio 2019 for C# to compile it. Hit build and all necessary dependencies should be automatically downloaded. Check the following links for useful tutorials. If you are having trouble or want to try to make your own plugin/mod, feel free to ask for help in modding channels of either the [Koikatsu](https://discord.gg/hevygx6) or [IllusionSoft](https://discord.gg/F3bDEFE) Discord servers.
- https://help.github.com/en/github/creating-cloning-and-archiving-repositories/cloning-a-repository
- https://docs.microsoft.com/en-us/visualstudio/get-started/csharp/?view=vs-2019
- https://docs.microsoft.com/en-us/visualstudio/ide/troubleshooting-broken-references?view=vs-2019
</details>
    
## Compiling with Visual Studio Code (Not the suggested way, but my way)
<details>
  <summary>Click to expand</summary>
    
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
</details>
    
