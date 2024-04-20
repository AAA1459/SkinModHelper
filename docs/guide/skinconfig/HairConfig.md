
HairConfig.yaml
-----------------------------------
If you want to set hair color or more for your target, then you can check here.

The content here involves a new config, its skeleton and function is like this
```yaml
  HairLengths:
  - < HairLength >
  HairColors:
  - < HairColor >
  HairFlash: [true/false]
  OutlineColor: [use six digit RGB hex code]
```

If here contains what you need then you can try those to uses them:
1. Into the directory of target sprite.
2. Then create a new folder called `skinConfig` here.
3. And put a "`HairConfig.yaml`" file to that "`skinConfig`" folder.
   * `../Gameplay/[target sprites's directory]/skinConfig/HairConfig.yaml`
4. Then copy the fields you need and write the value for it
   * For details, you can scroll down


HairLengths
-----------------------------------
If you want your target's hair to be longer or shorter,
Then you can use this:
```
  HairLengths:
  - Dashes: [use -1 to 32]     
      # using -1 mean apply this length to player in feather status
    Length: [use 1 to 99]
```

HairColors
-----------------------------------
If you want your target to get new hair color, other than the default maddy's color, 
Then you can use this:
```
  HairColors:
  - Dashes: [use 0 to 32]
    Color: [use six digit RGB hex code]     # such as: "9B3FB5", that is baddy's 1-dash color
	SegmentsColors:
	- Segment: [Which segments of hair]     # use [negative numbers] to get reverse order
          Color: [use six digit RGB hex code]
```

HairFlash
-----------------------------------
player's hair will flashing when player's dashes be used or refill.
but if you not want that, Then you can use this:
```
  HairFlash: false
```

OutlineColor
-----------------------------------
If you want to recolor target's hair border to non-black, so you can use this:
```
  OutlineColor: [use six digit RGB hex code]
```



[previous page](/docs/guide/README.md#more-miscellaneous)
