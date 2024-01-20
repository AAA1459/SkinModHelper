
ColorGrades
-----------------------------------
If you want to set color grades for specified-ID in Sprites.xml, then you can come here.

colorGrades will make specified-ID are rendered differently at different dash counts of IDself.
If you want to add its, then you need to do the following steps:

1. create a folder for specified-ID, be like："../Gameplay/[IDself's rootPath]/ColorGrading/"

2. in that folder, name the images "dash[X].png", where [X] is the number of dashes the color grade should apply to.
   * For example, if I had a 0-dash color grade, I would name the file "../Gameplay/[IDself's rootPath]/ColorGrading/dash0.png"
   * You can grab the base color grade from "Celeste/Content/Graphics/ColorGrading/none.png"
    
3. Pick the color you want to replace on the sprite of specified-ID, find that color on the color grade, and then replace it with the color you want for that dash count.

At this point, color grades for specified-ID is done

more things
-----------------------------------
* you can add an extra color grades "flash.png". it will works when player's hair flash.
* color grades can work to NPC badeline, if you want do, you can to test it.
* color grades not supported CelesteNet yet.


[previous page](/docs/guide/README.md#more-miscellaneous)
