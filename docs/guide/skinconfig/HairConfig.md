
HairConfig.yaml
-----------------------------------
If you want to set hair color etc for specified-ID in Sprites.xml, then you can come here

The content here involves a new config, if you want use them, 
you need to create a new config file for that object-ID, be like: "../Gameplay/[IDself's rootPath]/skinConfig/HairConfig.yaml"

Here is a skeleton of that new config file. 
Each of the fields will be explained below.
```yaml
  HairColors:
  - < HairColors >
```



HairColors
-----------------------------------
If you want object-ID to have a new hair color, other than the default maddy's color, 
Then you can use this:
```
  HairColors:     # The following content can be set multiple times, but do not let [Dashes] repeat same number
  - Dashes: [use 0 to 5]
    Color: [use six digit RGB hex code]     # such as: ["9B3FB5"], that is baddy's 1-dash color
```
that object-ID also can is NPC badeline, As long as you want.



[previous page](/docs/guide/README.md#more-miscellaneous)