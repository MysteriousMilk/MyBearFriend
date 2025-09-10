# My Bear Friend
A Valheim mod that makes Bears tameable.

<img src="https://github.com/MysteriousMilk/MyBearFriend/blob/master/Screenshots/Screenshot1.png?raw=true" width="700" alt="My Bear Friend!" />

## Manual Install Instructions
BepInEx is required for this mod. Assuming BepInEx is already installed, locate the BepInEx folder in the Valheim installation directory (By default: C:\ProgramFiles(x86)\Steam\steamapps\common\Valheim\BepInEx) and navigate to the plugins folder. Unzip the contents of this package to a folder called "MyBearFriend" within the plugins folder. This should be all that is required, assuming all mod dependencies are installed.

## Features
- Bear's can now be tamed
- Default food: Blueberries; Honey; RawMeat; DeerMeat;
- Food and Taming settings are configurable

#### Localization
Localization is implemented and English localization is provided.

#### Can I... ?
**Breed Bears** - No, not at the moment. This is an easy feature to add, but the new bear type currently does not a "bear cub" model. I could potentially use the same bear model and scale it down. I might experiment with this in the future.

**Ride Bears** - No riding bears yet either. Theoretically it should be easy to implement. The Lox prefab has an offset for the saddle. The bear does not. I'd either have to add the offset to the prefab in Unity or try to inject it via code. I'll look into this in a future update.

## Contact and Issue Reporting
You can report issues with the mod at the github link below.\
<https://github.com/MysteriousMilk/MyBearFriend>

Additionally, you can reach me in the [Valheim Modding Discord](https://discord.com/invite/GUEBuCuAMz) under the name Milk.

## Changelog
**v0.1.0 - Initial Release**\
Implemented base mod functionality.