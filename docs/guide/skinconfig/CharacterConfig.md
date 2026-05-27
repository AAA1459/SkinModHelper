
## _CharacterConfig.yaml_
If you wish to apply certain effects to your target, you can check here.

The content here involves a new config, its structure and function is like this
```yaml
LowStaminaFlashHair: [true/false]
LowStaminaFlashColor: [use six digit RGB hex code]

SilhouetteMode: [true/false]

TintMaskWithHair:  [true/false]
MaskMode: [Red/Green/Blue/Grayscale]

ColorGradingSuchAsPlayer: [true/false]
ColorGradingAfterColored: [true/false]

IdleColdOptions: [Lists with identifiers and weights]
IdleWarmOptions: [Lists with identifiers and weights]
IdleAnimationChance: [floats]

TrailsColor: [use six digit RGB hex code]
DeathParticleColor: [use six digit RGB hex code]

HoldableFacingFlipable: [true/false]

ParticleModify:
- < particleModifier >

EntityTweaks:
- < Tweaks >
```
You don’t have to include all of them until you see what you want.

If this contains what you need, follow these steps to use them:
1. Navigate the directory of target sprite.
2. Create a new folder named `skinConfig` here.
3. Place a file named "`CharacterConfig.yaml`" within "`skinConfig`" folder.
   * Example path: `../Gameplay/[target sprites's directory]/skinConfig/CharacterConfig.yaml`
4. Copy the fields you need and specify their values in `CharacterConfig.yaml`
   * For field details, refer below.
   
---
### LowStaminaFlash
When the player's stamina is almost deplete, the player will start flashing red.
If you want to customize this flash color (especially when red is too intense for your skin), use this:
```yaml
LowStaminaFlashColor: [use six digit RGB hex code]     # default color is "ff0000"
```
If you want this flash effect to apply to the skin's hair as well, use:
```yaml
LowStaminaFlashHair: true
```

---
### SilhouetteMode
If you want to Color the entire target's sprites with its hair color, be like a silhouette.
So enable it with:
```yaml
SilhouetteMode: true
```
* Always enable [LowStaminaFlashHair](#lowstaminaflash) when this is enabled.
* color the hair border also when this is enabled.

---
### TintMaskWithHair
If you want to color the local pixels of sprites with its hair color. 
You can enable this and draw the mask part in sprites.
```yaml
TintMaskWithHair: true
```
And about the mask part, 
we need a `MaskMode` to confirm if you want to replace the pure red, green, blue, or grayscale (basen on rgb value) with the hair color.
```yaml
MaskMode: [Red/Green/Blue/Grayscale]
```
* it almost is a better silhouette mode?  enabling this will always disable [it](#silhouettemode).
* also replace the hair border when its color matches `MaskMode`

* when the sprite has no hair, enabling this will tint mask with player's hair color.

---
### ColorGradingSuchAsPlayer
The following content is related to [Skin ColorGrade](/docs/guide//skinconfig/ColorGrade.md).
```yaml
ColorGradingSuchAsPlayer: true
```
Enabling this will make the sprite try to apply its own colorgrades to self based on the player's dashes.
* only working if the sprite has no hair


---
### ColorGrading before/after Colored
The following content is related to [Skin ColorGrade](/docs/guide//skinconfig/ColorGrade.md).

By default. SMH+ applying color grades _**before**_ hair and sprite is colored. 
it makes the character (especially hair) to be grayscale, solid etc by color grades are impossible...

If you happen to want the character to be grayscale etc. add this in character config: 
```yaml
ColorGradingAfterColored: true
```
Like literally, it changes the default _**before**_ to _**after**_ there. <br>
**If you want to change the local colors of character by color grading. ignore this.**

---
### IdleAnimationChance
When the player is playing the idle animation, the idle variant animations idleA, idleB, idleC will be plays randomly with different weight.
If you think A, B, C are not enough. want even D, E... check this:
```yaml
IdleColdOptions:
- A, 3
- B, 5
- C, 1
IdleWarmOptions: [Format is same as the above]    # Override IdleColdOptions to work when a core-like map is in fire mode
```
A, B, C are identifiers there. Suppose a identifier is D there, the game will try to play the animation idleD.
And the numbers are the weight of each animation when the game is going to play the idle variants. C will be rare there.

NOTE: Don't ignore the basic A, B, C and only have D, E. Otherwise the game won't play them.

And this, it can changes the frequency of the idle variant animations
```yaml
IdleAnimationChance: [Numbers between 0 and 1]    
  # 1 is 100% to play the idle variants. The default value is 0.2 as 20%
```



---
### TrailsColor
Certain entities will generate trails at times... such as bird, oshiro boss, seeker...
if you want to recolor these trails, use this:
```yaml
TrailsColor: [use six digit RGB hex code]
```
NOTE: this is not applicable for players or silhouettes.

---
### DeathParticleColor
Certain entities generate death particles with their color, 
if you want to recolor these particlet, use this:
```yaml
DeathParticleColor: [use six digit RGB hex code]
```

---
### HoldableFacingFlipable
Theo Crystal or Glider entities is holdable for player, but their's sprites do not flip with moving...
if you want change their to do, so: 
```yaml
HoldableFacingFlipable: true
```
Theory this also should work for the holdable entities of helpers, pls feedback if not.

---
### ParticleModify
Used to modify specific particles emitted by an entity that have this skin. 

Requires a certain code knowledge. 
checking the content of `Celeste.ParticleType`, `Celeste.ParticleTypes` may also helps. and [here and search _**particleModifier**_](/Code/Config/CharacterConfig.cs)

The way of use is to specify a field of `ParticleType` type and overwrite partial values. Here is a entire structure.
```yaml
ParticleModify:
- TargetFullName: [ClassFullName]::[FieldName]    # e.g `Celeste.Player::P_CassetteFly`

  Source: [String as texture path]
    # Find and set a texture to particles near the folder that containing this config.
  SourceChooser: [String list]
    # Set multiple textures for particles... to randomly get one at every emitted.
  # `Source` and `SourceChooser` cannot be set at the same time
  
  Color: [six digit RGB hex code]
  Color2: [six digit RGB hex code]
  ColorMode: [Static/Choose/Blink/Fade]    # Select one as value like `true` of [true/false]
  FadeMode: [None/Linear/Late/InAndOut]
  SpeedMin: [floats]
  SpeedMax: [floats]
  SpeedMultiplier: [floats]
  Acceleration: [floats, floats]
  Friction: [floats]
  Direction: [floats]
  DirectionRange: [floats]
  LifeMin: [floats]
  LifeMax: [floats]
  Size: [floats]
  SizeRange: [floats]
  SpinMin: [floats]
  SpinMax: [floats]
  SpinFlippedChance: [true/false]
  RotationMode: [None/Random/SameAsDirection]
  ScaleOut: [true/false]
  UseActualDeltaTime: [true/false]

```

---
### _EntityTweaks_
there maybe required you have some code knowledge... 
it'll allow customize entity's any initial-value, any sprites:
```yaml
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
* if field type is an _`enum`_... so value should be `[number]`, or enum value's name from code.

---

[previous page](/docs/guide/README.md#more-miscellaneous)
