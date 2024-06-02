
# _Skin Mod Helper Plus Guide_
This guide will walk you through making your skin mod compatible with Skin Mod Helper Plus.

---
## _A brief introduction to config files_
Config files give SMH+ information about your skinmod. 
>
You need to create a file named "SkinModHelperConfig.yaml" in your mod root (next to your everest.yaml).

Here is a skeleton of a SkinModHelperConfig.yaml file.
Each of the fields will be explained below.
```yaml
- SkinName: [an SkinName that is required]

  # ---Player skin---
  Player_List: [true/false]
  Silhouette_List: [true/false]
  
  Character_ID: [new Player ID]
  hashSeed: [SkinName]
  
  OtherSprite_Path: [a path]
  # ---
  
  # ---General skin---
  General_List: [true/false]
  OtherSprite_ExPath: [a path]
  # ---
  
  Mod: [Skins grouping]
```

---
## _SkinName_
In the config file, we first need to write this information:
```
- SkinName: [Set a base option for your skin]     # required
```

---
## _Creating a player skin_
### Character_ID
If you want your skin to be a unique player skin, so set this:
```
  Character_ID: [your new player id]
```
This becomes the ID that you modify in the sprites.xml instead of the normal `<player>`, 
so need to do more for it:
1. Open or create a file named "Sprites.xml" within your mod's "Graphics" folder.
2. write these in that `Sprites.xml`:
```xml
<?xml version="1.0" encoding="utf-8" ?>

<Sprites>
  <!-- assume id "MySkin" is Character_ID you setted -->
  <MySkin copy="player" path="characters/MySkin/">
    <!-- shortened -->
  </MySkin>
</Sprites>
```
3. Make sure Sprites.xml there have your `Character_ID` or it is matched, otherwise the game will crash.

---
### OtherSprite_Path
SMH+ uses xml files to edit other sprites if you noticed. Visit [everest's wiki](https://github.com/EverestAPI/Resources/wiki/Reskinning-Entities#reskinning-entities-through-spritesxml) for a more comprehensive guide on xml files.

here is used to set the directory of Sprites.xml, or Portraits.xml. it take works when the skin is activated.
```
  OtherSprite_Path: [the directory of xmls]     # The starting point of below path: "...Graphics/"
```
This is recommended to reskin IDs related to Madeline. Sprites.xml also have certain IDs is from other helper instead of vanilla, [see wiki](https://github.com/AAA1459/SkinModHelper/wiki/Textures-list-of-Various-Type#maddy-or-baddy-related). ****
Also, please don't set `<player>` IDs there.

---
### Make player skin appear in Mod-Options
To make player skins to appear in the options of smh+, use:
```
  Player_List: true    # Affects the "Player Skin" option
  Silhouette_List: true    # Affects the "Silhouette Skin" option
```

---
### hashSeed
The player skins are celestenet compatible. The hashSeed is a unique value that is used to identify your skin. 
>
If not included, it defaults to SkinName you setted, but can be overwritten if it conflicts with another skinmod.
```
  hashSeed: [unique keys]
```

---
### Automatically Jump of Player skin
After you select a Player Skin, If has some config's name close to this player skin, and you meet some conditions.
Then SkinModHelper will try to automatically switch to that new player skin.

The purpose of Those Automatically Jump are, 
for let player(maddy) looks different in the same entity. such as "payphone" ID of Sprites.xml.

We will introduce those automatically jumps and their jump conditions:
* "[SkinName] + _NB" 
   * conditions: When the player is in no_backpack state
* "[SkinName] + _lantern"
   * If you want reskin `<player>` ID of Jungle Helper, please use this and set its [Character_ID] based on that `<player>` ID.
   * conditions: the player holds lantern from JungleHelper
* "[SkinName] + _lantern_NB"
   * conditions: the player holds lantern from JungleHelper and is in no_backpack state.

Note: automatically-jump to new player skin after, you used skin's info will all is from that new player skin's config info

---
## _Creating a general skin_
Use a general skin if your skinmod is not going to change the player skin. They only need 2 parameters except `SkinName`.
```
  General_List: false          # Makes this skins not shows up in the options menu, Can be ignored if you don't want.
  OtherSprite_ExPath: [a path]        # Works the same way as OtherSprite_Path
```
General skins work just like [OtherSprite_Path](#othersprite_path), except they use OtherSprite_ExPath.

---
## _Skins grouping_
Skins will be grouped according to their mod id, but you can change its grouping, just use:
```
  Mod: [grouping name]
```
Except setting it's grouping here, 
you can also go into the language file and redefine it, just like as a dialog key.

btw, don't forget to define other dialog key for your skin, you can find them in the game.

and there also have an hidden key _`[skinkey]__Description`_, you can to use this for descriptiony skin info when the skin be chosen.

---
## _Standard example of config file_
You can download them as examples for making skins: 
* [Touhou-cirno](https://gamebanana.com/mods/316584)
* [OshiroBoss But Badeline](https://gamebanana.com/mods/444994)
* [Niko - Oneshot](https://gamebanana.com/mods/251814)
* [Ralsei - Deltarune](https://gamebanana.com/mods/385893)
And, there is also a test skin under the banana-page of skinmodhelperplus, that also is an example.

---
## _More Miscellaneous_
1. SkinModHelper have some introduce of special textures, and collected various sources of IDs animation.
   [clike here for check wiki](https://github.com/AAA1459/SkinModHelper/wiki/Textures-list-of-Various-Type#special-texture-settingreskin)

2. more complicated things
   * [Setting ColorGrade for skin](/docs/guide//skinconfig/ColorGrade.md)
   * [Setting HairConfig for skin](/docs/guide//skinconfig/HairConfig.md)
   * [Setting some effects for skin](/docs/guide/skinconfig/CharacterConfig.md)

---
## _Troubleshooting_
If your skin is not be registered (or does not appear in the menu):
* Make sure your configuration file is named correctly and in the right place
* Check your log to see your skin report anything when trying to register

If your sprites/portraits are not appearing in-game:
* Make sure your XML is valid. You can compare to the vanilla files or use an [online syntax checker](https://www.xmlvalidation.com/)
* Make sure the "path" fields to your sprites/portraits are correct and the files are in the right place
* Make sure the "start" field references an animation you have reskinned.

If you get missing textures or unexpected vanilla textures:
* Check your log to see what textures are missing -- these messages can point you in the right direction
* Make sure the number of images matches the number of animation "frames"

If skins is always works or not, not affected by options:
* Check if the sprite path is consistent with vanilla or maps, please avoid it.
* Check smh's "Precisely skin choose" menu, check if there is the IDs setted always or never enabled when it related to your skin.

If some textures are misaligned, even xml-undefined:
1. In the same folder as that texture, create a .meta.yaml file with the name of that texture
   * If the texture is "madeline.png", So create "madeline.meta.yaml" flie
2. Write that texture information to the .meta.yaml file you create
```
X: [X offset value of texture in game]
Y: [Y offset value of texture in game]
Width: [pixel Width of texture]     # maybe, game need get its center point
Height: [pixel Height of texture]
Premultiplied: [true/false]    # about this i don't know.
```
3. Restart the game to make it work or reload

If you get crashes:
* Check your log to see if it's a missing texture
* Make sure you don't have any "Metadata" sections for missing animations in Sprites.xml
* Contact me!

If you want to find all vanilla textures (png format):
* You can find it here: [Celeste Graphics Dump v1400](https://drive.google.com/file/d/1ITwCI2uJ7YflAG0OwBR4uOUEJBjwTCet/view)

This process can be pretty involved, especially if you are porting over an existing skin mod,
so feel free to [contact me](../../README.md#contact) if you need help, find an issue, or would
like a new feature supported! You can also use a currently supported skin mod as a reference.

