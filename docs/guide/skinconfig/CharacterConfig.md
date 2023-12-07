
CharacterConfig.yaml
-----------------------------------
If you want to set some effects for specified-ID in Sprites.xml, then you can come here

The content here involves a new config, if you want use them, 
you need to create a new config file for that object-ID, be like: "../Gameplay/[IDself's rootPath]/skinConfig/CharacterConfig.yaml"

Here is a skeleton of that new config file. 
Each of the fields will be explained below.
```yaml
BadelineMode: [true/false]
SilhouetteMode: [true/false]

LowStaminaFlashHair: [true/false]
LowStaminaFlashColor: [use six digit RGB hex code]
```


Character's effect setting
-----------------------------------
If you want to set it's character-effect for object-ID, 
Then you can choose to add the effect you want (you can add multiple):
```
BadelineMode: true     # Let the default hair color etc of object-ID be baddy.
SilhouetteMode: true     # Color the entire object-ID with its hair color, like a silhouette
```

LowStaminaFlash
-----------------------------------
When the player's stamina is almost exhausted, the player will start to flash red.
If you want to change that flash color (when it very harsh on your skin), so you can use this:
```
  LowStaminaFlashColor: [use six digit RGB hex code]     # default color is "ff0000"
```
and if you want that flash to work on skin's hair, then use this:
```
  LowStaminaFlashHair: true
```

[previous page](/docs/guide/README.md#more-miscellaneous)
