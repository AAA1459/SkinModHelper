
## _HairConfig.yaml_
If you want to customize hair color or more to your target, you can check here.

The content here involves a new config, its structure and function is like this
```yaml
HairAttrWithDashes:
- < AttrWithDashes >  # color, lengths, scale.
  
HairFlash: [true/false]
HairFloatingDashCount: [number]
  
OutlineColor: [use six digit RGB hex code]
```

If this contains what you need, follow these steps to use them:
1. Navigate the directory of target sprite.
2. Create a new folder named `skinConfig` here.
3. Place a file named "`HairConfig.yaml`" within "`skinConfig`" folder.
   * Example path: `../Gameplay/[target sprites's directory]/skinConfig/HairConfig.yaml`
4. Copy the fields you need and specify their values in `HairConfig.yaml`
   * For fields details, refer below.

---
### HairAttrWithDashes
Here you can setting something according to the dashes, 
e.g hair color, length. to make them different from madeline:
```
HairAttrWithDashes:
- Dashes: [any integers]     # except for this must be set, all others are optional
  
  Color: [use six digit RGB hex code]     
    # example: "9B3FB5", which represents Baddy's 1-dash color
	  
  Scale: [floats as root], [floats as end]
    # The segments's scale will gradually transition between two floats from root to end.
	  
  Length: [integers from 1 to 99]
```  
* If there is a set with `Dashes` is -1, its value will work on the player feather state except for color.

If you want to go a step further, set attr ​​for individual segments, so use this in the each set's ends
```
  SegmentAttrs:
  - Segment: [Which segment of hair]     # work in reverse order if it is a negative number
    Color: [use six digit RGB hex code]
    Scale: [floats]
```
* Make the value of the segments work require set the corresponding value of the parent set also.


---
### HairFlash
By default, the player's hair flashes when dashes are used or refilled. 
If you wish to disable this feature, use:
```
HairFlash: false
```

---
### HairFloatingDashCount
By default, the player's hair floating when have at least 2 dashes. If you want that floating to require more or less dashes, use:
```
HairFloatingDashCount: [any number]
  # using "0" to make it always floating.
  # using "-1" to make it never floating.
```


---
### OutlineColor
If you need to recolor the hair border for target, use:
```
OutlineColor: [use six digit RGB hex code]     # default color is "000000"
```

---
[previous page](/docs/guide/README.md#more-miscellaneous)
