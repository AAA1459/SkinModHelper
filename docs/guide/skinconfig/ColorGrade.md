
ColorGrades
-----------------------------------
The color grades will make the player or target are rendered differently at different dash counts, 
Can be used to modify sprites's local colors

If you want to set color grades works for your target, then you can try those to do.
1. Into the directory of target sprite.
2. Then create a new folder called `ColorGrading` here.
   * `../Gameplay/[target sprites's directory]/ColorGrading`
3. find `none.png` in `Celeste/Content/Graphics/ColorGrading` directory, and copy it to you created `ColorGrading` folder
4. rename `none.png` to `dash[X].png`, where `X` is the number of dashes it should apply to.
   * The range of `X` is 0 to 32.
   * `dash0.png`, `dash1.png`, `dash2.png`... can exist at the same time.
5. Pick the colors you want to replace on the target sprite, find that colors on `dash[X].png` image, and then replace it with the color you want.
   * If doesn't find target color on `dash[X].png` , then you just need to find the closest color
   

more things
-----------------------------------
* you can add an extra color grades `flash.png` be like `dash[X].png`. it will works when player's hair flash.
* color grades can work to NPC badeline, If you are curious, you can try.
* color grades not supported CelesteNet yet.


[previous page](/docs/guide/README.md#more-miscellaneous)
