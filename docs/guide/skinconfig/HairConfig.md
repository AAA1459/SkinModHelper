
## _HairConfig.yaml_
If you want to customize hair color or more to your target, you can check here.

The content here involves a new config, its structure and function is like this
```yaml
  HairLengths:
  - < HairLength >
  HairColors:
  - < HairColor >
  
  HairFlash: [true/false]
  HairFloatingDashCount: [true/false]
  
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
### HairLengths
If you wish to adjust the length of the hair for target, use:
```
  HairLengths:
  - Dashes: [any number]     
      # using "-1" applies this length to the player in feather status
    Length: [use 1 to 99]
```

---
### HairColors
If you want to assign a new hair color to target, 
different from maddy's default color, follow this setup:
```
  HairColors:
  - Dashes: [any number]
    Color: [use six digit RGB hex code]     # example: "9B3FB5", which represents Baddy's 1-dash color
	SegmentsColors:
	- Segment: [Which segments of hair]     # use negative numbers for reverse order
          Color: [use six digit RGB hex code]
```

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
