
CharacterConfig.yaml
-----------------------------------
If you want to set some effects for specified-ID in Sprites.xml, then you can come here

The content here involves a new config, if you want use them, 
you need to create a new config file for that object-ID, be like: "../Gameplay/[IDself's rootPath]/skinConfig/CharacterConfig.yaml"

Here is a skeleton of that new config file. 
Each of the fields will be explained below.
```yaml
SilhouetteMode: [true/false]

LowStaminaFlashHair: [true/false]
LowStaminaFlashColor: [use six digit RGB hex code]

TrailsColor: [use six digit RGB hex code]
DeathParticleColor: [use six digit RGB hex code]
```


SilhouetteMode
-----------------------------------
If you want to Color the entire object-ID with its hair color, be like as silhouette.
Then you can use this:
```
SilhouetteMode: true
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

TrailsColor
-----------------------------------
Some entities will generate their trails at sometimes... be like bird, oshiro_boss, seeker...
if you want to recolor those trails, so you can use this:
```
TrailsColor: [use six digit RGB hex code]
  # if object is badeline, so you can setting this to special "HairColor"
```
NOTE: this is unusable for player or silhouette.


DeathParticleColor
-----------------------------------
Some entities will generate their own particle with it's color, you can use this to recolor that particle.
```
DeathParticleColor: [use six digit RGB hex code]
```


[previous page](/docs/guide/README.md#more-miscellaneous)
