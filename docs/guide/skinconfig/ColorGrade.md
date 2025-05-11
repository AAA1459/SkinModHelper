
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
For player. If a color grade named `flash.png` near `dash[X].png`. Then it will override `dash[X].png` to applying when the player's hair flashes.

--- 
### ColorGrading before/after sprite is colored
By default. SMH+ applying color grades _**after**_ hair and sprite is colored. 
it makes the character (especially hair) to be grayscale, solid etc by color grades are impossible...

If you happen to want the character to be grayscale etc. we have a way to do: 
* add a file named `-ColorGradeBefColored.txt` near `dash[X].png`. it'll change the default _**after**_ to _**before**_.
  * `-ColorGradeBefColored.txt` no need to contain any text, just make sure its name case is correctly.
* and nothing more, except makes the hues of `dash[X].png` to be grayscale etc your want.





---
### more misc
* Color grades can also function for NPC Badeline.

---
[previous page](/docs/guide/README.md#more-miscellaneous)
