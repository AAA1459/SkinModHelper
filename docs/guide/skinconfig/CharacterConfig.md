
## _CharacterConfig.yaml_
If you wish to apply certain effects to your target, you can check here.

The content here involves a new config, its structure and function is like this
```yaml
SilhouetteMode: [true/false]

LowStaminaFlashHair: [true/false]
LowStaminaFlashColor: [use six digit RGB hex code]

TrailsColor: [use six digit RGB hex code]
DeathParticleColor: [use six digit RGB hex code]
```

If this contains what you need, follow these steps to use them:
1. Navigate the directory of target sprite.
2. Create a new folder named `skinConfig` here.
3. Place a file named "`CharacterConfig.yaml`" within "`skinConfig`" folder.
   * Example path: `../Gameplay/[target sprites's directory]/skinConfig/CharacterConfig.yaml`
4. Copy the fields you need and specify their values in `CharacterConfig.yaml`
   * For field details, refer below.

---
### SilhouetteMode
If you want to Color the entire target's sprites with its hair color, be like a silhouette.
So use this:
```
SilhouetteMode: true
```
Note: This also affects target's hair border color, just by default it is unaffected black.

---
### LowStaminaFlash
When the player's stamina is almost deplete, the player will start flashing red.
If you want to customize this flash color (especially when red is too intense for your skin), use this:
```
LowStaminaFlashColor: [use six digit RGB hex code]     # default color is "ff0000"
```
If you want this flash effect to apply to the skin's hair as well, use:
```
LowStaminaFlashHair: true
```

---
### TrailsColor
Certain entities will generate trails at times... such as bird, oshiro boss, seeker...
if you want to recolor these trails, use this:
```
TrailsColor: [use six digit RGB hex code]
  # If the target is Badeline Chaser, you can set this to a special "HairColor"
```
NOTE: this is not applicable for players or silhouettes.

---
### DeathParticleColor
Certain entities generate death particles with their color, 
if you want to recolor these particlet, use this:
```
DeathParticleColor: [use six digit RGB hex code]
```
---
[previous page](/docs/guide/README.md#more-miscellaneous)
