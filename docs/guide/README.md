
Skin Mod Helper Guide
======================
This guide will walk you through making your skin mod compatible with Skin Mod Helper.

A brief introduction to config files
------------------------------------
Config files can help the Skin Mod Helper find information about your skin mod.
If your mod provides a skin, then you need to create a file named "SkinModHelperConfig.yaml" in your mod root (next to your everest.yaml).

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
  
  # ---General skin---
  General_List: [true/false]
  OtherSprite_ExPath: [a path]
```


SkinName
-----------------------------------
In the config file, we first need to write this information:
```
- SkinName: [Set a base option for your skin]     # required
```

`Character_ID`
---------------------------
If your skin type is "Player Skin",
Then, you need to set a PlayerSkin ID for the SkinName information you wrote, use this to do it:
```
  Character_ID: [your new PlayerID]
```

OtherSprite / Some thing about skin's Xml
-----------------------------------
Skin will need you to have an Xml file, 
Now let we first set root directory for those Xml, or called them is skin's xml.
```
# The starting point of below path: "Graphics/"
  OtherSprite_Path: [Root directory path]     # for paleyr skin
  OtherSprite_ExPath: [Root directory path]     # for general skin
```
For information about those Xml files, we quote some [everest's wiki](https://github.com/EverestAPI/Resources/wiki/Reskinning-Entities#reskinning-entities-through-spritesxml).
After viewing, we need to do those:
* Remove the some content, or called ID in the skin's Xml that you not have or not want reskin.
  * ! SkinModHelper has a menu to listing all of them, so this very important.
* Then if your skin type is "Player Skin":
  1. Create another file calls "Sprites.xml" in "Graphics/" directory
  2. Copy some PlayerID in the skin's xml to that another xml.
  3. Renaming that PlayerID to [Character_ID] that comes from you setted in this config
    * ! If you do this wrong, the game will crash when you enter the map
  4. If you have some questions about playerID is what, so [check here](https://github.com/AAA1459/SkinModHelper/wiki/Textures-list-of-Various-Type#maddy-or-baddy-related)
*  Skin's xml not only include "Sprites.xml", it also can include "Portraits.xml"
  * The specific operation is as above.
now you have finished them, Let's check out the other parts.


let your skin appear in Mod-Options
-----------------------------------
If your skin type is "Player Skin", Then We need to use some more content to let it get there:
```
  Player_List: true    # Affects the "Player Skin" option
  Silhouette_List: true    # Affects the "Silhouette Skin" option
```
If your skin type is "General Skin", Then when you set "[OtherSprite_ExPath]" after, them will appear in "General Skin" list.
Or use this to prevent it get there:
```
  General_List: false
```

hashSeed
-----------------------------------
We should have mentioned that the SkinModHelper will make your player skin compatible with CelesteNet.
SkinModHelper use a "hashSeed" to do it, that "hashSeed" defaults is "[SkinName]"

If your skin happens to conflict with other skins when compatible with CelesteNet, 
Then you can use this to change and fix it.
```
  hashSeed: [any]
```

You can write multiple skin info to your config file, 
this just need repeats everything above steps.



Special Jump of Player skin
-----------------------------------
If there are some special names close to that player skin in the config file, and you meet some conditions.
Then SkinModHelper will try to do a special jump.

The purpose of Those Special Jump are, 
for let player(maddy) looks different in the same entity. such as "payphone" ID of Sprites.xml.

We will introduce those special jumps and their jump conditions:
* "[SkinName] + _NB" 
   * conditions: When the player is no_backpack state
* "[SkinName] + _lantern"
   * If you want reskin some Player ID from JungleHelper, then you can use this special jump
   * conditions: When the player get lantern from JungleHelper
* "[SkinName] + _lantern_NB"
   * conditions: When the player get lantern from JungleHelper and is no_backpack state
Note: special-jump to other skin after, you used skin's info will all is from that other skin's config info



Standard example of config file
-----------------------------------
You can download them as examples for making skins: 
* [Touhou-cirno](https://gamebanana.com/mods/316584)
* [OshiroBoss But Badeline](https://gamebanana.com/mods/444994)


More Miscellaneous
---------------------
1. SkinModHelper have some introduce of special textures, and collected various sources of IDs animation.
   [clike here for check](https://github.com/AAA1459/SkinModHelper/wiki/Textures-list-of-Various-Type#special-texture-settingreskin)

2. more complicated things
   * [Setting ColorGrade for skin](/docs/guide//skinconfig/ColorGrade.md)
   * [Setting HairConfig for skin](/docs/guide//skinconfig/HairConfig.md)
   * [Setting some effects for skin](/docs/guide/skinconfig/CharacterConfig.md)



Troubleshooting
-----------------------
If your skin is not be registered (or does not appear in the menu):
* Make sure your configuration file is named correctly and in the right place
* Check your log to see your skin report anything when trying to register
* If the log says nothing, see this section: 
  [let your skin appear in Mod-Options](#let-your-skin-appear-in-mod-options)

If your sprites/portraits are not appearing in-game:
* Make sure your XML is valid. You can compare to the vanilla files or use an [online syntax checker](https://www.xmlvalidation.com/)
* Make sure the "path" fields to your sprites/portraits are correct and the files are in the right place
* Make sure the "start" field references an animation you have reskinned.

If you get missing textures or unexpected vanilla textures:
* Check your log to see what textures are missing -- these messages can point you in the right direction
* Make sure the number of images matches the number of animation "frames"

If some xml-undefined textures are misaligned:
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

