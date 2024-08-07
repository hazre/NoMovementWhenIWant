# NoMovementWhenIWant

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) that disables movement based on a user-defined cloud variable's value. You can configure which hand(s) will be affected. This mod is designed for VR input movement.

> [!NOTE]
> This mod is compatible with the [NoTankControls mod](https://github.com/Nytra/NoTankControls). If you don't want this compatibility, you can disable the NoTankControls mod.

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place [NoMovementWhenIWant.dll](https://github.com/art0007i/NoMovementWhenIWant/releases/latest/download/NoMovementWhenIWant.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
1. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

## Usage
1. Set up a cloud variable with a boolean value at the path specified in the configuration.
2. When the cloud variable is set to `true`, movement will be disabled for the configured hand(s).
3. Set the cloud variable to `false` to re-enable movement.

## Credits
- [Based on Banane9's NeosGoToBedAlready Code](https://github.com/Banane9/NeosGoToBedAlready)
- [Based on Nytra's NoTankControls Code](https://github.com/Nytra/NoTankControls)
