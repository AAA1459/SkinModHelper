
CharacterConfig.yaml
-----------------------------------
If you want to set some effects for your target, then you can check here.

The content here involves a new config, its skeleton and function is like this
```yaml
SilhouetteMode: [true/false]

LowStaminaFlashHair: [true/false]
LowStaminaFlashColor: [use six digit RGB hex code]

TrailsColor: [use six digit RGB hex code]
DeathParticleColor: [use six digit RGB hex code]
```

If here contains what you need then you can try those to uses them:
1. Into the directory of target sprite.
2. Then create a new folder called `skinConfig` here.
3. And put a "`CharacterConfig.yaml`" file to that "`skinConfig`" folder.
   * `../Gameplay/[target sprites's directory]/skinConfig/CharacterConfig.yaml`
4. Then copy the fields you need and write the value for it
   * For details, you can scroll down


SilhouetteMode
-----------------------------------
If you want to Color the entire target's sprites with its hair color, be like as silhouette.
Then you can use this:
```
SilhouetteMode: true
```
Note: This also affects target's hair border color, although it defaults is black.


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
  # if target is badeline chaser, so you can setting this to special "HairColor"
```
NOTE: this is unusable for player or silhouette.


DeathParticleColor
-----------------------------------
Some entities will generate their own death particle with it's color, you can use this to recolor that particle.
```
DeathParticleColor: [use six digit RGB hex code]
```


[previous page](/docs/guide/README.md#more-miscellaneous)
