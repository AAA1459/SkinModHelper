using Monocle;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.UI;
using System.Collections;
using System.Reflection;
using System.Threading;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper
{
    public class SkinModHelperUI
    {
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;


        public enum NewMenuCategory {
            SkinFreeConfig, None, SkinsExtended
        }
        public void CreateAllOptions(NewMenuCategory category, bool includeMasterSwitch, bool includeCategorySubmenus, bool includeRandomizer,
            Action submenuBackAction, TextMenu menu, bool inGame, bool forceEnabled) {

            if (category == NewMenuCategory.None) {
                BuildPlayerSkinSelectMenu(menu, inGame);
                BuildSilhouetteSkinSelectMenu(menu, inGame);
                menu.Add(BuildExSkinSubMenu(menu, inGame));

                menu.Add(BuildMoreOptionsMenu(menu, inGame, includeCategorySubmenus, submenuBackAction));
            }
            if (category == NewMenuCategory.SkinFreeConfig) {
                Build_SkinFreeConfig_NewMenu(menu, inGame);
            }
            if (category == NewMenuCategory.SkinsExtended) {
                Build_SkinsExtended_NewMenu(menu, inGame);
            }
        }
        private TextMenu.SubHeader buildHeading(TextMenu menu, string headingNameResource) {
            return new TextMenu.SubHeader(Dialog.Clean($"SkinModHelper_NewSubMenu_{headingNameResource}"));
        }











        private void BuildPlayerSkinSelectMenu(TextMenu menu, bool inGame)
        {
            TextMenu.Option<string> skinSelectMenu = new(Dialog.Clean("SkinModHelper_Settings_PlayerSkin_Selected"));

            skinSelectMenu.Add(Dialog.Clean("SkinModHelper_Settings_DefaultPlayer"), DEFAULT, true);

            foreach (SkinModHelperConfig config in skinConfigs.Values) {

                if (!config.Player_List || Settings.HideSkinsInOptions.Contains(config.SkinName)) {
                    continue;
                }
                bool selected = config.SkinName == Settings.SelectedPlayerSkin;
                string name = "SkinModHelper_Player__" + config.SkinName;
                name = Dialog.Clean(!string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : name);

                skinSelectMenu.Add(name, config.SkinName, selected);
            }

            // Set our update action on our complete menu
            skinSelectMenu.Change(skinId => UpdateSkin(skinId, inGame));

            if (Disabled(inGame)) {
                skinSelectMenu.Disabled = true;
            }
            menu.Add(skinSelectMenu);
        }

        private void BuildSilhouetteSkinSelectMenu(TextMenu menu, bool inGame)
        {
            TextMenu.Option<string> skinSelectMenu = new(Dialog.Clean("SkinModHelper_Settings_SilhouetteSkin_Selected"));

            skinSelectMenu.Add(Dialog.Clean("SkinModHelper_Settings_DefaultSilhouette"), DEFAULT, true);

            foreach (SkinModHelperConfig config in skinConfigs.Values) {

                if (!config.Silhouette_List || Settings.HideSkinsInOptions.Contains(config.SkinName)) {
                    continue;
                }

                bool selected = config.SkinName == Settings.SelectedSilhouetteSkin;
                string name = "SkinModHelper_Player__" + config.SkinName;
                name = Dialog.Clean(!string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : name);

                skinSelectMenu.Add(name, config.SkinName, selected);
            }

            skinSelectMenu.Change(skinId => UpdateSilhouetteSkin(skinId, inGame));

            menu.Add(skinSelectMenu);
        }

        public TextMenuExt.SubMenu BuildExSkinSubMenu(TextMenu menu, bool inGame) {

            return new TextMenuExt.SubMenu(Dialog.Clean("SkinModHelper_Settings_Otherskin"), false).Apply(subMenu => {
                if (Disabled(inGame)) {
                    subMenu.Disabled = true;
                }

                foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {

                    if (config.General_List == false) {
                        continue;
                    }

                    string Options_name = ("SkinModHelper_ExSprite__" + config.SkinName);
                    bool Options_OnOff = false;

                    if (!Settings.ExtraXmlList.ContainsKey(config.SkinName)) {
                        Settings.ExtraXmlList.Add(config.SkinName, false);
                    } else {
                        Options_OnOff = Settings.ExtraXmlList[config.SkinName];
                    }

                    Options_name = !string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : Options_name;
                    TextMenu.OnOff Options = new TextMenu.OnOff(Dialog.Clean(Options_name), Options_OnOff);
                    Options.Change(OnOff => UpdateExtraXml(config.SkinName, OnOff, inGame));

                    subMenu.Add(Options);
                }
            });
        }

        public TextMenuExt.SubMenu BuildMoreOptionsMenu(TextMenu menu, bool inGame, bool includeCategorySubmenus, Action submenuBackAction) {
            return new TextMenuExt.SubMenu(Dialog.Clean("SkinModHelper_MORE_OPTIONS"), false).Apply(subMenu => {

                TextMenuButtonExt SpriteSubmenu;
                subMenu.Add(SpriteSubmenu = AbstractSubmenu.BuildOpenMenuButton<OuiCategorySubmenu>(menu, inGame, submenuBackAction, new object[] { NewMenuCategory.SkinFreeConfig }));

                TextMenuButtonExt SkinsExtended = AbstractSubmenu.BuildOpenMenuButton<OuiCategorySubmenu>(menu, inGame, submenuBackAction, new object[] { NewMenuCategory.SkinsExtended });
            });
        }


        public void Build_SkinFreeConfig_NewMenu(TextMenu menu, bool inGame) {

            TextMenu.OnOff SkinFreeConfig_OnOff = new TextMenu.OnOff(Dialog.Clean("SkinModHelper_SkinFreeConfig_OnOff"), Settings.FreeCollocations_OffOn);
            SkinFreeConfig_OnOff.Change(OnOff => RefreshSkinValues(OnOff, inGame));

            if (Disabled(inGame)) {
                SkinFreeConfig_OnOff.Disabled = true;
            }
            menu.Add(SkinFreeConfig_OnOff);

            if (SpriteSkins_records.Count > 0) {
                menu.Add(buildHeading(menu, "SpritesXml"));
            }
            foreach (KeyValuePair<string, List<string>> recordID in SpriteSkins_records) {

                string SpriteID = recordID.Key;
                string SpriteText = Dialog.Has($"SkinModHelper_Sprite__{SpriteID}") ? $"{SpriteID} ({Dialog.Clean($"SkinModHelper_Sprite__{SpriteID}")})" : SpriteID;
                TextMenu.Option<string> skinSelectMenu = new(SpriteText);


                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Original"), ORIGINAL, true);
                if (!Settings.FreeCollocations_Sprites.ContainsKey(SpriteID)) {
                    Settings.FreeCollocations_Sprites[SpriteID] = DEFAULT;
                }
                string selected = Settings.FreeCollocations_Sprites[SpriteID];

                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Default"), DEFAULT, selected == DEFAULT);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_LockedToPlayer"), LockedToPlayer, selected == LockedToPlayer);

                foreach (string SkinName in recordID.Value) {
                    string SkinText = Dialog.Has($"SkinModHelper_Sprite__{SpriteID}__{SkinName}") ? Dialog.Clean($"SkinModHelper_Sprite__{SpriteID}__{SkinName}") : Dialog.Clean($"SkinModHelper_anySprite__{SkinName}");
                    
                    if (!string.IsNullOrEmpty(OtherskinConfigs[SkinName].SkinDialogKey)) {
                        SkinText = Dialog.Clean(OtherskinConfigs[SkinName].SkinDialogKey);
                    }
                    skinSelectMenu.Add(SkinText, SkinName, (SkinName == selected));
                }
                skinSelectMenu.Change(skinId => RefreshSkinValues_Sprites(SpriteID, skinId, inGame));
                menu.Add(skinSelectMenu);
            }


            if (PortraitsSkins_records.Count > 0) {
                menu.Add(buildHeading(menu, "PortraitsXml"));
            }
            foreach (KeyValuePair<string, List<string>> recordID in PortraitsSkins_records) {

                string SpriteID = recordID.Key;
                string SpriteText = Dialog.Has($"SkinModHelper_Portraits__{SpriteID}") ? $"{SpriteID} ({Dialog.Clean($"SkinModHelper_Portraits__{SpriteID}")})" : SpriteID;
                TextMenu.Option<string> skinSelectMenu = new(SpriteText);


                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Original"), ORIGINAL, true);
                if (!Settings.FreeCollocations_Portraits.ContainsKey(SpriteID)) {
                    Settings.FreeCollocations_Portraits[SpriteID] = DEFAULT;
                }
                string selected = Settings.FreeCollocations_Portraits[SpriteID];

                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Default"), DEFAULT, selected == DEFAULT);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_LockedToPlayer"), LockedToPlayer, selected == LockedToPlayer);

                foreach (string SkinName in recordID.Value) {
                    string SkinText = Dialog.Has($"SkinModHelper_Portraits__{SpriteID}__{SkinName}") ? Dialog.Clean($"SkinModHelper_Portraits__{SpriteID}__{SkinName}") : Dialog.Clean($"SkinModHelper_anyPortraits__{SkinName}");

                    if (!string.IsNullOrEmpty(OtherskinConfigs[SkinName].SkinDialogKey)) {
                        SkinText = Dialog.Clean(OtherskinConfigs[SkinName].SkinDialogKey);
                    }
                    skinSelectMenu.Add(SkinText, SkinName, (SkinName == selected));
                }
                skinSelectMenu.Change(skinId => RefreshSkinValues_Portraits(SpriteID, skinId, inGame));
                menu.Add(skinSelectMenu);
            }


            if (OtherSkins_records.Count > 0) {
                menu.Add(buildHeading(menu, "OtherExtra"));
            }
            foreach (KeyValuePair<string, List<string>> recordID in OtherSkins_records) {

                string SpriteID = recordID.Key;
                string SpriteText = Dialog.Clean($"SkinModHelper_Other__{SpriteID}");
                TextMenu.Option<string> skinSelectMenu = new(SpriteText);


                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Original"), ORIGINAL, true);
                if (!Settings.FreeCollocations_OtherExtra.ContainsKey(SpriteID)) {
                    Settings.FreeCollocations_OtherExtra[SpriteID] = DEFAULT;
                }

                string selected = Settings.FreeCollocations_OtherExtra[SpriteID];
                bool select = Settings.FreeCollocations_OtherExtra[SpriteID] == DEFAULT;

                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Default"), DEFAULT, selected == DEFAULT);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_LockedToPlayer"), LockedToPlayer, selected == LockedToPlayer);

                foreach (string SkinName in recordID.Value) {
                    string SkinText = Dialog.Has($"SkinModHelper_Other__{SpriteID}__{SkinName}") ? Dialog.Clean($"SkinModHelper_Other__{SpriteID}__{SkinName}") : Dialog.Clean($"SkinModHelper_anyOther__{SkinName}");

                    if (!string.IsNullOrEmpty(OtherskinConfigs[SkinName].SkinDialogKey)) {
                        SkinText = Dialog.Clean(OtherskinConfigs[SkinName].SkinDialogKey);
                    }
                    skinSelectMenu.Add(SkinText, SkinName, (SkinName == selected));
                }


                skinSelectMenu.Change(skinId => RefreshSkinValues_OtherExtra(SpriteID, skinId, inGame));
                menu.Add(skinSelectMenu);
            }
        }




        public static bool Disabled(bool inGame) {
            if (inGame) {
                Player player = Engine.Scene?.Tracker.GetEntity<Player>();
                if (player != null && player.StateMachine.State == Player.StIntroWakeUp) {
                    return true;
                }
            }
            return false;
        }




        public static Dictionary<string, MethodInfo> ExtendedOptions = new();
        public void Build_SkinsExtended_NewMenu(TextMenu menu, bool inGame) {

            //You can use be-like this code in your mod for put your options to this menu of skinmodhelper:
            /*
            MethodInfo GetMethod = typeof(SkinModHelperUI).GetMethod("Build_SkinFreeConfig_NewMenu", BindingFlags.Public | BindingFlags.Instance);
            SkinModHelperUI.ExtendedOptions.Add("skinName", GetMethod);
            */
            foreach (string Skin in ExtendedOptions.Keys) {
                if (Settings.SelectedPlayerSkin == Skin || Settings.SelectedSilhouetteSkin == Skin) {
                    menu.Add(buildHeading(menu, Skin));
                    ExtendedOptions[Skin].Invoke(this, new object[] { menu, inGame });
                }
            }
        }
        public bool DetectSkinExtended() {
            foreach (string Skin in ExtendedOptions.Keys) {
                if (Settings.SelectedPlayerSkin == Skin || Settings.SelectedSilhouetteSkin == Skin) {
                    return true;
                }
            }
            return false;
        }


    }

    public class OuiSkinModHelperSubmenu : AbstractSubmenu {
        private int savedMenuIndex = -1;
        private TextMenu currentMenu;

        public OuiSkinModHelperSubmenu() : base("SkinModHelper_mod_name", null) { }

        protected override void addOptionsToMenu(TextMenu menu, bool inGame, object[] parameters) {
            currentMenu = menu;

            // variants submenus + randomizer options
            new SkinModHelperUI().CreateAllOptions(SkinModHelperUI.NewMenuCategory.None, false, true, true,
                () => OuiModOptions.Instance.Overworld.Goto<OuiSkinModHelperSubmenu>(),
                menu, inGame, false /* we don't care since there is no master switch */);
        }

        public override IEnumerator Enter(Oui from) {
            // start running Enter, so that the menu is initialized
            IEnumerator enterEnum = base.Enter(from);
            if (enterEnum.MoveNext())
                yield return enterEnum.Current;

            // finish running Enter
            while (enterEnum.MoveNext())
                yield return enterEnum.Current;
        }

        public override IEnumerator Leave(Oui next) {
            savedMenuIndex = currentMenu.Selection;
            currentMenu = null;
            return base.Leave(next);
        }

        protected override void gotoMenu(Overworld overworld) {
            overworld.Goto<OuiSkinModHelperSubmenu>();
        }

        protected override string getMenuName(object[] parameters) {
            return base.getMenuName(parameters).ToUpperInvariant();
        }

        protected override string getButtonName(object[] parameters) {
            return Dialog.Clean($"SkinModHelper_{(((bool)parameters[0]) ? "PAUSEMENU" : "MODOPTIONS")}_BUTTON");
        }
    }











    public static class CommonExtensions
    {
        public static EaseInSubMenu Apply<EaseInSubMenu>(this EaseInSubMenu obj, Action<EaseInSubMenu> action)
        {
            action(obj);
            return obj;
        }
    }


    public abstract class AbstractSubmenu : Oui, OuiModOptions.ISubmenu {

        private TextMenu menu;

        private const float onScreenX = 960f;
        private const float offScreenX = 2880f;

        private float alpha = 0f;

        private readonly string menuName;
        private readonly string buttonName;
        private Action backToParentMenu;
        private object[] parameters;

        /// <summary>
        /// Builds a submenu. The names expected here are dialog IDs.
        /// </summary>
        /// <param name="menuName">The title that will be displayed on top of the menu</param>
        /// <param name="buttonName">The name of the button that will open the menu from the parent submenu</param>
        public AbstractSubmenu(string menuName, string buttonName) {
            this.menuName = menuName;
            this.buttonName = buttonName;
        }

        /// <summary>
        /// Adds all the submenu options to the TextMenu given in parameter.
        /// </summary>
        protected abstract void addOptionsToMenu(TextMenu menu, bool inGame, object[] parameters);

        /// <summary>
        /// Gives the title that will be displayed on top of the menu.
        /// </summary>
        protected virtual string getMenuName(object[] parameters) {
            return Dialog.Clean(menuName);
        }

        /// <summary>
        /// Gives the name of the button that will open the menu from the parent submenu.
        /// </summary>
        protected virtual string getButtonName(object[] parameters) {
            return Dialog.Clean(buttonName);
        }

        /// <summary>
        /// Builds the text menu, that can be either inserted into the pause menu, or added to the dedicated Oui screen.
        /// </summary>
        private TextMenu buildMenu(bool inGame) {
            TextMenu menu = new TextMenu();

            menu.Add(new TextMenu.Header(getMenuName(parameters)));
            addOptionsToMenu(menu, inGame, parameters);

            return menu;
        }

        // === some Oui plumbing

        public override IEnumerator Enter(Oui from) {
            menu = buildMenu(false);
            Scene.Add(menu);

            menu.Visible = Visible = true;
            menu.Focused = false;

            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
                menu.X = offScreenX + -1920f * Ease.CubeOut(p);
                alpha = Ease.CubeOut(p);
                yield return null;
            }

            menu.Focused = true;
        }

        public override IEnumerator Leave(Oui next) {
            Audio.Play(SFX.ui_main_whoosh_large_out);
            menu.Focused = false;

            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f) {
                menu.X = onScreenX + 1920f * Ease.CubeIn(p);
                alpha = 1f - Ease.CubeIn(p);
                yield return null;
            }

            menu.Visible = Visible = false;
            menu.RemoveSelf();
            menu = null;
        }

        public override void Update() {
            if (menu != null && menu.Focused && Selected && Input.MenuCancel.Pressed) {
                Audio.Play(SFX.ui_main_button_back);
                backToParentMenu();
            }

            base.Update();
        }

        public override void Render() {
            if (alpha > 0f) {
                Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * alpha * 0.4f);
            }
            base.Render();
        }

        // === / some Oui plumbing

        /// <summary>
        /// Supposed to just contain "overworld.Goto<ChildType>()".
        /// </summary>
        protected abstract void gotoMenu(Overworld overworld);

        /// <summary>
        /// Builds a button that opens the menu with specified type when hit.
        /// </summary>
        /// <param name="parentMenu">The parent's TextMenu</param>
        /// <param name="inGame">true if we are in the pause menu, false if we are in the overworld</param>
        /// <param name="backToParentMenu">Action that will be called to go back to the parent menu</param>
        /// <param name="parameters">some arbitrary parameters that can be used to build the menu</param>
        /// <returns>A button you can insert in another menu</returns>
        public static TextMenuButtonExt BuildOpenMenuButton<T>(TextMenu parentMenu, bool inGame, Action backToParentMenu, object[] parameters) where T : AbstractSubmenu {
            return getOrInstantiateSubmenu<T>().buildOpenMenuButton(parentMenu, inGame, backToParentMenu, parameters);
        }

        private static T getOrInstantiateSubmenu<T>() where T : AbstractSubmenu {
            if (OuiModOptions.Instance?.Overworld == null) {
                // this is a very edgy edge case. but it still might happen. :maddyS:
                Logger.Log(LogLevel.Warn, "SkinModHelper/AbstractSubmenu", $"Overworld does not exist, instanciating submenu {typeof(T)} on the spot!");
                return (T)Activator.CreateInstance(typeof(T));
            }
            return OuiModOptions.Instance.Overworld.GetUI<T>();
        }

        /// <summary>
        /// Method getting called on the Oui instance when the method just above is called.
        /// </summary>
        private TextMenuButtonExt buildOpenMenuButton(TextMenu parentMenu, bool inGame, Action backToParentMenu, object[] parameters) {
            if (inGame) {
                // this is how it works in-game
                return (TextMenuButtonExt)new TextMenuButtonExt(getButtonName(parameters)).Pressed(() => {
                    Level level = Engine.Scene as Level;

                    // set up the menu instance
                    this.backToParentMenu = backToParentMenu;
                    this.parameters = parameters;

                    // close the parent menu
                    parentMenu.RemoveSelf();

                    // create our menu and prepare it
                    TextMenu thisMenu = buildMenu(true);

                    // notify the pause menu that we aren't in the main menu anymore (hides the strawberry tracker)
                    bool comesFromPauseMainMenu = level.PauseMainMenuOpen;
                    level.PauseMainMenuOpen = false;

                    thisMenu.OnESC = thisMenu.OnCancel = () => {
                        // close this menu
                        Audio.Play(SFX.ui_main_button_back);

                        Instance.SaveSettings();
                        thisMenu.Close();

                        // and open the parent menu back (this should work, right? we only removed it from the scene earlier, but it still exists and is intact)
                        // "what could possibly go wrong?" ~ famous last words
                        level.Add(parentMenu);

                        // restore the pause "main menu" flag to make strawberry tracker appear again if required.
                        level.PauseMainMenuOpen = comesFromPauseMainMenu;
                    };

                    thisMenu.OnPause = () => {
                        // we're unpausing, so close that menu, and save the mod Settings because the Mod Options menu won't do that for us
                        Audio.Play(SFX.ui_main_button_back);

                        Instance.SaveSettings();
                        thisMenu.Close();

                        level.Paused = false;
                        Engine.FreezeTimer = 0.15f;
                    };

                    // finally, add the menu to the scene
                    level.Add(thisMenu);
                });
            } else {
                // this is how it works in the main menu: way more simply than the in-game mess.
                return (TextMenuButtonExt)new TextMenuButtonExt(getButtonName(parameters)).Pressed(() => {
                    // set up the menu instance
                    this.backToParentMenu = backToParentMenu;
                    this.parameters = parameters;

                    gotoMenu(OuiModOptions.Instance.Overworld);
                });
            }
        }
    }


    public class TextMenuButtonExt : TextMenu.Button {
        /// <summary>
        /// Function that should determine the button color.
        /// Defaults to false.
        /// </summary>
        public Func<Color> GetHighlightColor { get; set; } = () => Color.White;

        public TextMenuButtonExt(string label) : base(label) { }

        /// <summary>
        /// This is the same as the vanilla method, except it calls getUnselectedColor() to get the button color
        /// instead of always picking white.
        /// This way, when we change Highlight to true, the button is highlighted like all the "non-default value" options are.
        /// </summary>
        public override void Render(Vector2 position, bool highlighted) {
            float alpha = Container.Alpha;
            Color color = Disabled ? Color.DarkSlateGray : ((highlighted ? Container.HighlightColor : GetHighlightColor()) * alpha);
            Color strokeColor = Color.Black * (alpha * alpha * alpha);
            bool flag = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
            Vector2 position2 = position + (flag ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0f));
            Vector2 justify = (flag && !AlwaysCenter) ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            ActiveFont.DrawOutline(Label, position2, justify, Vector2.One, color, 2f, strokeColor);
        }
    }


    public class OuiCategorySubmenu : AbstractSubmenu {

        public OuiCategorySubmenu() : base(null, null) { }

        protected override void addOptionsToMenu(TextMenu menu, bool inGame, object[] parameters) {
            SkinModHelperUI.NewMenuCategory category = (SkinModHelperUI.NewMenuCategory)parameters[0];

            // only put the category we're in
            new SkinModHelperUI().CreateAllOptions(category, false, false, false, null /* we don't care because there is no submenu */,
                menu, inGame, false /* we don't care because there is no master switch */);
        }

        protected override void gotoMenu(Overworld overworld) {
            Overworld.Goto<OuiCategorySubmenu>();
        }

        protected override string getButtonName(object[] parameters) {
            return Dialog.Clean($"SkinModHelper_NewMenu_{(SkinModHelperUI.NewMenuCategory)parameters[0]}");
        }

        protected override string getMenuName(object[] parameters) {
            return Dialog.Clean($"SkinModHelper_NewMenu_{(SkinModHelperUI.NewMenuCategory)parameters[0]}_opened");
        }
    }
}