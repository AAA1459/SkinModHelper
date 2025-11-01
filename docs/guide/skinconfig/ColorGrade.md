
## _ColorGrades_
The color grades will make the player are rendered differently at different dash counts, 
Can be used to modify sprites's local colors

If you want to set color grades works for your target, To do follow these steps:
1. Navigate the directory of target sprite.
2. Create a new folder named `ColorGrading` here.
   * Example path: `Gameplay/[target sprites's directory]/ColorGrading`
3. find `none.png` in `Celeste/Content/Graphics/ColorGrading` directory, and copy it to you created `ColorGrading` folder
4. rename `none.png` to `dash[X].png`, where `X` is the number of dashes it should apply to.
   * `dash0.png`, `dash1.png`, `dash2.png`... can exist at the same time.
5. Pick the colors you want to replace on the target sprite, find that colors on `dash[X].png` image, and to replace it with the color you want.
   * If you doesn't find color you want on `dash[X].png` , then you just find the closest color
   
   
--- 
### "Flash" ColorGrade
For player. If a color grade named `flash[X].png` or `flash.png` near `dash[X].png`. Then it will override `dash[X].png` to applying when the player's hair flashes.


---
### more misc
* Color grades can also function for NPC Badeline.
* `CharacterConfig.yaml` has [[ColorGrading before/after Colored]](/docs/guide/skinconfig/CharacterConfig.md#colorgrading-beforeafter-colored) related to here

---
[previous page](/docs/guide/README.md#more-miscellaneous)
