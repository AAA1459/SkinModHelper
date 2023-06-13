
CharacterConfig.yaml
-----------------------------------
If you want to set some effects for specified-ID in Sprites.xml, then you can come here

The content here involves a new config, if you want use them, 
you need to create a new config file for that object-ID, be like: "../Gameplay/[IDself's rootPath]/skinConfig/CharacterConfig.yaml"

Here is a skeleton of that new config file. 
Each of the fields will be explained below.
```yaml
BadelineMode: [true/false]
SilhouetteMode: [true/false]

# ---yes, this config only has these at present---
```


Character's effect setting
-----------------------------------
If you want to set it's character-effect for object-ID, 
Then you can choose to add the effect you want (you can add multiple):
```
BadelineMode: [true/false]     # if true: Let the default hair color etc of object-ID be baddy.
SilhouetteMode: [true/false]     # if true: Color the object-ID's entire body with its hair color, like a silhouette
```



[previous page](/docs/guide/README.md#more-miscellaneous)
