
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
  
  BadelineMode: [true/false]    
  SilhouetteMode: [true/false]
  JungleLanternMode: [true/false]
  
  SpecificPlayerSprite_Path: [a path]
  OtherSprite_Path: [a path]
  colorGrade_Path: [a path]
  
  HairColors:
  - < HairColors >
  
  # ---non-Player skin---
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
  Character_ID: [your new Player ID]     # this also needs you to Create a "Sprites.Xml", will be detailed description later
```

HairColors
-----------------------------------
If you want your player skin to have a new hair color, other than the default maddy's color, 
Then you can use this:
```
  HairColors:     # The following content can be set multiple times, but do not let [Dashes] repeat same number
  - Dashes: [use 0 to 5]
    Color: [use six digit RGB hex code]
```

Character's effect setting
-----------------------------------
If you want to set their character-effect for your player skin, 
Then you can choose to add the effect you want (you can add multiple):
```
  BadelineMode: true     # Let the default hair color of PlayerSkin be baddy
  SilhouetteMode: true     # Color the player's entire body with its hair color, like a silhouette
  JungleLanternMode: true     # This involves some gameplay mechanisms of JungleHelper, probably don't add this unless you know what you're doing
```

A brief introduction to Sprites.xml
-----------------------------------
If you want to know more about config file, you may need to know a little about XMLs first

SkinModHelper can only replace sprites included in the default Sprites.xml location. 
("Celeste/Mods/[mod_name]/Graphics/Sprites.xml" or Vanilla's "Celeste/Content/Graphics/Sprites.xml")

If, the skin you make is a player skin. then we have these things: 
1. SkinModHelper will make player get your "[Character_ID]", no longer get Player IDs in vanilla.
2. So we need to create a Sprites.xml of default location.
3. inside it create a new ID called "[Character_ID]".
   * Use the "player_badeline" ID of vanilla as a guide -- that new ID should have all animations
   * Note: If the new ID does not match "[Character_ID]". Then it will directly crash the game


Specific Player Sprite
-----------------------------------
A player skin has some skin textures that cannot be easily replaced in vanilla, 
such as the "bangs" and "startStarFlyWhite" textures.

Skin Mod Helper will make those textures no longer use "characters/player" root path, 
Then try to use "[SourcesPath of Character_ID in sprites.xml]" as the new root path.

If you don't want to do that, and if you want to manually set a more unique new root path, then you can use this:
```
  SpecificPlayerSprite_Path: [path to the root directory of some specific texture]     
    # Path's starting point is "Graphics/Atlases/Gameplay/"
```

OtherSprite
-----------------------------------
Regarding the Player IDs in Sprites.xml, there are many IDs that are not classified as player IDs, 
but maddy appears in the animation texture of those IDs.

Such as "lookout", "payphone" and other IDs, or the "HonlyHelper_Petter" ID from HonlyHelper.
Below we will introduce a method to let SkinModHelper reskin them with the same ID:
```
  OtherSprite_Path: [Root directory path of non-default Sprites.xml]    # Path's starting point is "Graphics/".
```

If your skin type is "non-Player Skin", you just want to simply reskin some IDs. Then use this:
```
  OtherSprite_ExPath: [same as OtherSprite_Path]
```

ColorGrades
-----------------------------------
You can add color grades, let your playerSkin self are rendered differently at different dash counts by placing them
```
  colorGrade_Path: [custom colorGrade's root directory Path]    # Path's starting point is "Graphics/ColorGrading/"
```
in "Graphics/ColorGrading/[colorGrade_Path]/" and name the images "dashX.png", where X is the number
of dashes the color grade should apply to. For example, if I had a 0-dash color grade, I would name
the file "Graphics/ColorGrading/Bigkahuna/MySkin/dash0.png".
   * You can grab the base color grade from "Celeste/Content/Graphics/ColorGrading/none.png"
   * Pick the color you want to replace on the sprite, find that color on the color grade, and then
   replace it with the color you want for that dash count.


let your skin appear in Mod-Options
-----------------------------------
If your skin type is "Player Skin", Then We need to use some more content to let it get there:
```
  Player_List: true    # Affects the "Player Skin" option
  Silhouette_List: true    # Affects the "Silhouette Skin" option
```
If your skin type is "non-Player Skin", Then when you set "[OtherSprite_ExPath]" after, them will appear in "General Skin" list


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


Special Jump of config files
-----------------------------------
You select a skin in SkinModHelper. 
If there are some special names close to that skin in the config file, and you meet some conditions.
Then SkinModHelper will try to do a special jump.

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
The following content can be copied directly into your config file for test: 
```
- SkinName: "SkinTest_TestA"
  OtherSprite_ExPath: "SkinTest/TestA"

- SkinName: "SkinTest_TestB"
  OtherSprite_ExPath: "SkinTest/TestB"


- SkinName: "vanilla_player"
  Player_List: true
  Character_ID: "player"
  OtherSprite_Path: "SkinTest/TestA"

- SkinName: "vanilla_player_NB"
  Character_ID: "player_no_backpack"


- SkinName: "vanilla_Silhouette"
  Player_List: true

  SilhouetteMode: true
  Character_ID: "player_playback"
```
(Regarding the files and sprites required for the above configurations, SkinModHelper's own files already contain them, you can refer to those files)


More Miscellaneous
---------------------
1. About reskin method mentioned in "OtherSprite", it also can work for Portraits.xml, 
Just consider the corresponding "Sprites.xml" as "Portraits.xml".

2. You can add a custom death particle (the circles that appear around Madeline when she dies) by
creating a small image named death_particle.png and place it in your "[SpecificPlayerSprite_Path]" folder. Use white
as the only color -- it will be filled in by your current hair color on death.
   * For reference, the vanilla death particle is an 8x8 white circle (hair00.png).

3. A few extra things that can be reskinned
   * The particles for feathers: "../Gameplay/[OtherSprite_Path]/particles/feather.png"
   * The particles for dream blocks: "../Gameplay/[OtherSprite_Path]/objects/dreamblock/particles.png"
      * Use the vanilla image as a guide -- you need to space out the three particle sizes in a specific way for them to be used correctly.
   * The new bangs for all hasHair-ID in Sprites.xml: "../Gameplay/[IDself's SourcesPath]/bangs[number].png"
   * The new hair for all hasHair-ID in Sprites.xml: "../Gameplay/[IDself's SourcesPath]/hair00.png"

Note: some specific sprites's reskin path, can also use [OtherSprite_ExPath] to complete the reskin for them



Troubleshooting
-----------------------
If your skin is not be registered (or does not appear in the menu):
* Make sure your configuration file is named correctly and in the right place
* Check your log to see your skin report anything when trying to register
* If the log says nothing, see this section: 
  [let your skin appear in Mod-Options](...docs/guide/README.md#let-your-skin-appear-in-mod-options)

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
Width: [pixel Width of texture]
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

