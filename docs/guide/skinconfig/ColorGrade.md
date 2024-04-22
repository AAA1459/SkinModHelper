
## _ColorGrades_
The color grades will make the player are rendered differently at different dash counts, 
Can be used to modify sprites's local colors

If you want to set color grades works for your target, To do follow these steps:
1. Navigate the directory of target sprite.
2. Create a new folder named `ColorGrading` here.
   * Example path: `Gameplay/[target sprites's directory]/ColorGrading`
3. find `none.png` in `Celeste/Content/Graphics/ColorGrading` directory, and copy it to you created `ColorGrading` folder
4. rename `none.png` to `dash[X].png`, where `X` is the number of dashes it should apply to.
   * The range of `X` is 0 to 32.
   * `dash0.png`, `dash1.png`, `dash2.png`... can exist at the same time.
5. Pick the colors you want to replace on the target sprite, find that colors on `dash[X].png` image, and to replace it with the color you want.
   * If you doesn't find color you want on `dash[X].png` , then you just find the closest color
   
---
### more things
* You have the option to include an extra color grade named "`flash.png`". It will take effect when the player's hair flashes.
* Color grades can also function for NPC Badeline.
* Please note that color grades not supported CelesteNet yet.

---
[previous page](/docs/guide/README.md#more-miscellaneous)