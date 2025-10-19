
## _HairConfig.yaml_
If you want to customize hair color or more to your target, you can check here.

The content here involves a new config, its structure and function is like this
```yaml
HairAttrWithDashes:
- < AttrWithDashes >  # color, lengths, scale.

OutlineColor: [use six digit RGB hex code]

HairFlipMode: [None/SyncBangs/FacingBangs/FacingPrevHair]

BangsOrigin: [x],[y]
HairOrigin: [x],[y]
  
HairFlash: [true/false]
HairFloatingDashCount: [number]
  
```
You don’t have to include all of them until you see what you want.

If this contains what you need, follow these steps to use them:
1. Navigate the directory of target sprite.
2. Create a new folder named `skinConfig` here.
3. Place a file named "`HairConfig.yaml`" within "`skinConfig`" folder.
   * Example path: `../Gameplay/[target sprites's directory]/skinConfig/HairConfig.yaml`
4. Copy the fields you need and specify their values in `HairConfig.yaml`
   * For fields details, refer below.

---
### CustomHair
if you want to add custom bangs and hair.
just put the `bangs00~02` and `hair00` etc textures in the sprites directory. they will work

You can also add more `hair01`, `hair02` on this basis. They will be displayed together with `bangs01`, etc.

If you want something more advanced, You can set independent textures for specific segments of hair.
<br>e.g `hair00_3` as the _third_ segment. `hair00_-1` as the _one to last_ segment.

(No need to operate the config file but still write it here)

---
### HairAttrWithDashes
Here you can setting something according to the dashes, 
e.g hair color, length. to make them different from madeline:
```yaml
HairAttrWithDashes:
- Dashes: [any integers]     # except for this must be set, all others are optional
  
  Color: [use six digit RGB hex code]     
    # example: "9B3FB5", which represents Baddy's 1-dash color
  
  Scale: [floats as root], [floats as end]
    # The segments's scale will gradually transition between two floats from root to end.
 
  Length: [integers from 1 to 99]
```  
* If there is a set with `Dashes` is -1, its value will work on the player feather state.
  * but color won't affect the body color during feather. because the colored feathers state from modded.
* The default scale is `1, 0.25`
* The default length is `4`, `5`, `7` for normal, two-dashes, feather.

If you want to go a step further, set attr ​​for individual segments, so use this in the each set's ends
```yaml
  SegmentAttrs:
  - Segment: [Which segment of hair]     # work in reverse order if it is a negative number
    Color: [use six digit RGB hex code]
    Scale: [floats]
```
* Make the value of the segments work require set the corresponding value of the parent set also.
* if color value is "orig". so the corresponding segment color will keep in its original color. can used for the colored feathers
* We have some "unique" segments. they will change color in other stuff than hair.
  * the segment 101, used for dash trail color.
  * the segment 102, used for dash particle color.
  * the segment -101, used for hair border color.

---
### OutlineColor
If you need to recolor the hair border for target, use:
```
OutlineColor: [use six digit RGB hex code]     # default color is "000000"
```
You may have noticed that [HairAttrWithDashes](/docs/guide/skinconfig/HairConfig.md#hairattrwithdashes)] can also recolor the OutlineColor. 
But the OutlineColor color set here will be used as the default hair border color. used when any dashes

---
### HairFlipMode
Vanilla only flips bangs based on player facing, not hair.

So we provide the following ways to flip your hair. You can choose them.
```
HairFlipMode: [None/SyncBangs/FacingBangs/FacingPrevHair]    # The default value is "None"
```
* If its value is `None`. they just not to flips. just vanilla.
* If its value is `SyncBangs`. all segments will sync the bangs facing.
* If its value is `FacingBangs`... the segments will facing to bangs.
* If its value is `FacingPrevHair`. the segments will facing its previous segment.

---
### HairOrigin
Vanilla assumes the center position of the hair texture is `5,5`. then flip and render them from there.

If your hair texture size is larger than vanilla hair with 10x10. The player may become baldelline. Or hair cannot be flipped correctly

And here. can re-set the center position of the hair to avoid these.
```yaml
BangsOrigin: [number as X], [number as Y]
HairOrigin: [number as X], [number as Y]
```

---
### HairFlash
By default, the player's hair flashes when dashes are used or refilled. 
If you wish to disable this feature, use:
```yaml
HairFlash: false
```

---
### HairFloatingDashCount
By default, the player's hair floating when have at least 2 dashes. If you want that floating to require more or less dashes, use:
```yaml
HairFloatingDashCount: [any number]
  # using "0" to make it always floating.
  # using "-1" to make it never floating.
```

---
[previous page](/docs/guide/README.md#more-miscellaneous)
