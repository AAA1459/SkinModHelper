
## _CharacterConfig.yaml_
If you wish to apply certain effects to your target, you can check here.

The content here involves a new config, its structure and function is like this
```yaml
SilhouetteMode: [true/false]

LowStaminaFlashHair: [true/false]
LowStaminaFlashColor: [use six digit RGB hex code]

TrailsColor: [use six digit RGB hex code]
DeathParticleColor: [use six digit RGB hex code]

HoldableFacingFlipable: [true/false]

EntityTweaks:
- < Tweaks >
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
### HoldableFacingFlipable
Theo Crystal or Glider entities is holdable for player, but their's sprites do not flip with moving...
if you want change their to do, so: 
```
HoldableFacingFlipable: true
```
Theory this also should work for the holdable entities of helpers, pls feedback if not.


---
### _EntityTweaks_
there maybe required you have some code knowledge... 
it'll allow customize entity's any initial-value, any sprites:
```
EntityTweaks:
- Name: [Field name]  
  Value: [Field's new value]
  LimitOnType: [Limit this tweaks to on specific entity type]
  
  subTweaks:     # If the field type is special, you may need this to tweaks its sub-fields.
  - < Tweaks >     # A self-nesting, its structure same as "EntityTweaks".
  
  subTEST: true     # Output all sub-fields from target field.
TweaksTEST: true     # Output all fields from sprite's entity.
```
and, customize certain type's field require special values:
* if field type is _`Monocle.Sprite`_, so value should be `[a ID from Sprite.xml]`
* if field type is _`Monocle.Image`_ or _`Monocle.MTexture`_, so value should be `[sprite path]`
  * its starting point at the previous folder of "skinConfig", aka sprites folder.
* if field type is _`Microsoft.Xna.Framework.Color`_, so value should be `[six digit RGB hex code, or eight digit RGBA]`
* if field type is an _`enum`_... so value should be `[number]`

and and... here is a demo for refill.
```
EntityTweaks:
  - Name: "outline"     # MTexture type : sprite path
    Value: "flash04"
  - Name: "p_glow"   # ParticleType Type
    subTweaks: 
      - Name: "Size"       # Float type : number
        Value: 1.6
      - Name: "SizeRange"  # Float type : number
        Value: 1.2
      - Name: "Color"      # Color type : hex code
        Value: "8351c888"
      - Name: "Color2"     # Color type : hex code
        Value: "5e29a8"
```

---

[previous page](/docs/guide/README.md#more-miscellaneous)
