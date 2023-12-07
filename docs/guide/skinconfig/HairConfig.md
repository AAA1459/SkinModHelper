
HairConfig.yaml
-----------------------------------
If you want to set hair color etc for specified-ID in Sprites.xml, then you can come here

The content here involves a new config, if you want use them, 
you need to create a new config file for that object-ID, be like: "../Gameplay/[IDself's rootPath]/skinConfig/HairConfig.yaml"

Here is a skeleton of that new config file. 
Each of the fields will be explained below.
```yaml
  HairLengths:
  - < HairLength >
  HairColors:
  - < HairColor >
  HairFlash: [true/false]
  OutlineColor: [use six digit RGB hex code]
```

HairLengths
-----------------------------------
If you want object-ID's hair to be longer or shorter,
Then you can use this:
```
  HairLengths:
  - Dashes: [use -1 to 32]     
      # using [-1] mean apply this length to player in feather status
    Length: [use 1 to 99]
```

HairColors
-----------------------------------
If you want object-ID to get a new hair color, other than the default maddy's color, 
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
If you want to recolor hair border of object-ID, so you can use this:
```
  OutlineColor: [use six digit RGB hex code]
```


[previous page](/docs/guide/README.md#more-miscellaneous)
