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
using FMOD.Studio;
using System.Linq;
using Microsoft.Xna.Framework.Input;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper
{
    public class SkinModHelperUI {
        #region
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;

        public NewMenuCategory Category;
        public enum NewMenuCategory {
            SkinFreeConfig, None
        }
        public void CreateAllOptions(NewMenuCategory category, bool includeMasterSwitch, bool includeCategorySubmenus, bool includeRandomizer,
            Action submenuBackAction, TextMenu menu, bool inGame, bool forceEnabled) {

            Category = category;
            if (category == NewMenuCategory.None) {
                BuildPlayerSkinSelectMenu(menu, inGame);
                BuildSilhouetteSkinSelectMenu(menu, inGame);
                menu.Add(BuildExSkinSubMenu(menu, inGame));

                menu.Add(BuildMoreOptionsMenu(menu, inGame, includeCategorySubmenus, submenuBackAction));
            }
            if (category == NewMenuCategory.SkinFreeConfig) {
                Build_SkinFreeConfig_NewMenu(menu, inGame);
            }

            Overworld overworld = inGame ? null : OuiModOptions.Instance.Overworld;
            InputSearchUI SearchUI = InputSearchUI.Instance;
            if (SearchUI == null || SearchUI.Overworld != overworld) {
                SearchUI = new InputSearchUI(overworld);
            }
            Engine.Scene.Add(SearchUI);
            if (category == NewMenuCategory.SkinFreeConfig) {
                SearchUI.ShowSearchUI = true;
            } else {
                SearchUI.ShowSearchUI = false;
            }
        }
        private TextMenu.SubHeader buildHeading(TextMenu menu, string headingNameResource) {
            return new TextMenu.SubHeader(Dialog.Clean($"SkinModHelper_NewSubMenu_{headingNameResource}"));
        }
        #endregion

        //-----------------------------Options-----------------------------
        #region // player skin
        private void BuildPlayerSkinSelectMenu(TextMenu menu, bool inGame) {
            TextMenu.Option<string> skinSelectMenu = new(Dialog.Clean("SkinModHelper_Settings_PlayerSkin_Selected"));

            List<Tuple<string, TextMenuExt.EaseInSubHeaderExt>> descriptions = new();
            skinSelectMenu.Add(Dialog.Clean("SkinModHelper_Settings_DefaultPlayer"), DEFAULT, true);
            menu.Add(skinSelectMenu);

            string selected = "";
            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (!config.Player_List || Settings.HideSkinsInOptions.Contains(config.SkinName))
                    continue;
                if (config.SkinName == Settings.SelectedPlayerSkin)
                    selected = config.SkinName;

                string name = !string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : "SkinModHelper_Player__" + config.SkinName;

                int i2 = menu.IndexOf(skinSelectMenu) + 1;
                int i;
                for (i = 0; Dialog.Has(name + "__Description_" + i); i++) { }
                while (i > 0) {
                    i--;
                    TextMenuExt.EaseInSubHeaderExt _text = new TextMenuExt.EaseInSubHeaderExt(Dialog.Clean(name + "__Description_" + i), false, menu) {
                        TextColor = Color.Gray,
                        HeightExtra = 0f
                    };
                    descriptions.Add(new(config.SkinName, _text));
                    menu.Insert(i2, _text);
                }
                skinSelectMenu.Add(Dialog.Clean(name), config.SkinName, config.SkinName == Settings.SelectedPlayerSkin);
            }

            // if our settings don't exist...
            if (skinSelectMenu.Index == 0 && Settings.SelectedPlayerSkin != DEFAULT)
                ChangeUnselectedColor(skinSelectMenu, 1);
            if (Disabled(inGame))
                skinSelectMenu.Disabled = true;

            // Set our update action on our complete menu
            skinSelectMenu.Change(skinId => {
                selected = skinId;
                UpdatePlayerSkin(skinId, inGame);
                ChangeUnselectedColor(skinSelectMenu, 0);
                foreach (var d in descriptions)
                    d.Item2.FadeVisible = selected == d.Item1;
            });

            skinSelectMenu.OnEnter += delegate {
                foreach (var d in descriptions)
                    d.Item2.FadeVisible = selected == d.Item1;
            };
            skinSelectMenu.OnLeave += delegate {
                foreach (var d in descriptions)
                    d.Item2.FadeVisible = false;
            };
        }
        #endregion

        #region // silhouette skin
        private void BuildSilhouetteSkinSelectMenu(TextMenu menu, bool inGame) {
            TextMenu.Option<string> skinSelectMenu = new(Dialog.Clean("SkinModHelper_Settings_SilhouetteSkin_Selected"));

            List<Tuple<string, TextMenuExt.EaseInSubHeaderExt>> descriptions = new();
            skinSelectMenu.Add(Dialog.Clean("SkinModHelper_Settings_DefaultSilhouette"), DEFAULT, true);
            menu.Add(skinSelectMenu);

            string selected = "";
            foreach (SkinModHelperConfig config in skinConfigs.Values) {
                if (!config.Silhouette_List || Settings.HideSkinsInOptions.Contains(config.SkinName))
                    continue;
                if (config.SkinName == Settings.SelectedSilhouetteSkin)
                    selected = config.SkinName;

                string name = !string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : "SkinModHelper_Player__" + config.SkinName;

                int i2 = menu.IndexOf(skinSelectMenu) + 1;
                int i;
                for (i = 0; Dialog.Has(name + "__Description_" + i); i++) { }
                while (i > 0) {
                    i--;
                    TextMenuExt.EaseInSubHeaderExt _text = new TextMenuExt.EaseInSubHeaderExt(Dialog.Clean(name + "__Description_" + i), false, menu) {
                        TextColor = Color.Gray,
                        HeightExtra = 0f
                    };
                    descriptions.Add(new(config.SkinName, _text));
                    menu.Insert(i2, _text);
                }
                skinSelectMenu.Add(Dialog.Clean(name), config.SkinName, config.SkinName == Settings.SelectedSilhouetteSkin);
            }

            // if our settings don't exist...
            if (skinSelectMenu.Index == 0 && Settings.SelectedSilhouetteSkin != DEFAULT)
                ChangeUnselectedColor(skinSelectMenu, 1);

            // Set our update action on our complete menu
            skinSelectMenu.Change(skinId => {
                selected = skinId;
                UpdateSilhouetteSkin(skinId, inGame);
                ChangeUnselectedColor(skinSelectMenu, 0);
                foreach (var d in descriptions)
                    d.Item2.FadeVisible = selected == d.Item1;
            });
            skinSelectMenu.OnEnter += delegate {
                foreach (var d in descriptions)
                    d.Item2.FadeVisible = selected == d.Item1;
            };
            skinSelectMenu.OnLeave += delegate {
                foreach (var d in descriptions)
                    d.Item2.FadeVisible = false;
            };
        }
        #endregion

        #region // general skin
        public TextMenuExt.SubMenu BuildExSkinSubMenu(TextMenu menu, bool inGame) {

            return new TextMenuExt.SubMenu(Dialog.Clean("SkinModHelper_Settings_Otherskin"), false).Apply(subMenu => {
                if (Disabled(inGame)) {
                    subMenu.Disabled = true;
                }

                foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                    if (config.General_List == false)
                        continue;
                    bool Options_OnOff = false;

                    if (!Settings.ExtraXmlList.ContainsKey(config.SkinName))
                        Settings.ExtraXmlList.Add(config.SkinName, false);
                    else
                        Options_OnOff = Settings.ExtraXmlList[config.SkinName];

                    string DialogID = !string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : ("SkinModHelper_ExSprite__" + config.SkinName);
                    string Text = Dialog.Clean(DialogID);
                    string TextDescription = "";

                    if (Text.Length > 29) {
                        int index;
                        for (index = 32; index < Text.Length; index++)
                            if (char.IsUpper(Text, index) || Text[index] == ' ' || index > 39) { break; }

                        if (index < Text.Length) {
                            TextDescription = "..." + Text.Substring(index) + " ";
                            TextDescription = TextDescription.Replace("... ", "...");
                            Text = Text.Remove(index) + "...";
                        }
                    }
                    TextMenu.OnOff Options = new TextMenu.OnOff(Text, Options_OnOff);
                    Options.Change(OnOff => UpdateGeneralSkin(config.SkinName, OnOff, inGame));

                    subMenu.Add(Options);
                    int i;
                    for (i = 0; Dialog.Has(DialogID + "__Description_" + i); i++) { }
                    while (i > 0) {
                        i--;
                        Options.AddDescription(subMenu, menu, Dialog.Clean(DialogID + "__Description_" + i));
                    }
                    Options.AddDescription(subMenu, menu, TextDescription);
                }
            });
        }
        #endregion

        #region // more options menu
        public TextMenuExt.SubMenu BuildMoreOptionsMenu(TextMenu menu, bool inGame, bool includeCategorySubmenus, Action submenuBackAction) {
            return new TextMenuExt.SubMenu(Dialog.Clean("SkinModHelper_MORE_OPTIONS"), false).Apply(subMenu => {

                TextMenuButtonExt SpriteSubmenu;
                subMenu.Add(SpriteSubmenu = AbstractSubmenu.BuildOpenMenuButton<OuiCategorySubmenu>(menu, inGame, submenuBackAction, new object[] { NewMenuCategory.SkinFreeConfig }));
            });
        }
        #endregion

        #region // precisely skin choose
        public void Build_SkinFreeConfig_NewMenu(TextMenu menu, bool inGame) {
            List<TextMenu.Option<string>> allOptions = new();
            TextMenu.OnOff SkinFreeConfig_OnOff = new TextMenu.OnOff(Dialog.Clean("SkinModHelper_SkinFreeConfig_OnOff"), Settings.FreeCollocations_OffOn);

            SkinFreeConfig_OnOff.Change(OnOff => {
                RefreshSkinValues(OnOff, inGame);
                foreach (var options in allOptions) {
                    options.Disabled = !OnOff;
                }
            });

            if (Disabled(inGame)) {
                SkinFreeConfig_OnOff.Disabled = true;
            }
            menu.Add(SkinFreeConfig_OnOff);

            Action startSearching = AddSearchBox(menu);
            menu.OnUpdate = () => {
                if (InputSearchUI.Instance?.Key.Pressed == true) {
                    startSearching.Invoke();
                }
            };

            #region
            if (SpriteSkins_records.Count > 0) {
                menu.Add(buildHeading(menu, "SpritesXml"));
            }
            foreach (KeyValuePair<string, List<string>> recordID in DictionarySort(SpriteSkins_records)) {
                string SpriteID = recordID.Key;

                string SpriteText = SpriteID;
                string TextDescription = "";

                if (SpriteText.Length > 18) {
                    int index;
                    for (index = 18; index < SpriteText.Length - 3; index++) {
                        if (char.IsUpper(SpriteText, index) || SpriteText[index] == '_' || index > 25) { break; }
                    }
                    if (index < SpriteText.Length - 3) {
                        TextDescription = "..." + SpriteText.Substring(index) + " ";
                        SpriteText = SpriteText.Remove(index) + "...";
                    }
                }
                if (Dialog.Has($"SkinModHelper_Sprite__{SpriteID}")) {
                    TextDescription = TextDescription + $"({Dialog.Clean($"SkinModHelper_Sprite__{SpriteID}")})";
                }


                TextMenu.Option<string> skinSelectMenu = new(SpriteText);
                if (!Settings.FreeCollocations_Sprites.ContainsKey(SpriteID)) {
                    Settings.FreeCollocations_Sprites[SpriteID] = DEFAULT;
                }
                allOptions.Add(skinSelectMenu);
                string actually = SpriteSkin_record[SpriteID];

                skinSelectMenu.Change(skinId => {
                    actually = RefreshSkinValues_Sprites(SpriteID, skinId, inGame);

                    if (actually == ORIGINAL)
                        ChangeUnselectedColor(skinSelectMenu, 3);
                    else if (actually == null)
                        ChangeUnselectedColor(skinSelectMenu, 1);
                    else if (actually == (GetPlayerSkinName() + playercipher) && (skinSelectMenu.Index == 1 || skinSelectMenu.Index == 2))
                        ChangeUnselectedColor(skinSelectMenu, 2);
                    else
                        ChangeUnselectedColor(skinSelectMenu, 0);
                });
                string selected = Settings.FreeCollocations_Sprites[SpriteID];
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Original"), ORIGINAL, true);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Default"), DEFAULT, selected == DEFAULT);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_LockedToPlayer"), LockedToPlayer, selected == LockedToPlayer);


                foreach (string SkinName in recordID.Value) {
                    string SkinText;
                    if (Dialog.Has($"SkinModHelper_Sprite__{SpriteID}__{SkinName}"))
                        SkinText = Dialog.Clean($"SkinModHelper_Sprite__{SpriteID}__{SkinName}");
                    else if (!string.IsNullOrEmpty(OtherskinConfigs[SkinName].SkinDialogKey))
                        SkinText = Dialog.Clean(OtherskinConfigs[SkinName].SkinDialogKey);
                    else
                        SkinText = Dialog.Clean($"SkinModHelper_anySprite__{SkinName}");


                    skinSelectMenu.Add(SkinText, SkinName, (SkinName == selected));
                }

                if (selected == ORIGINAL)
                    ChangeUnselectedColor(skinSelectMenu, 3);
                else if (actually == null || skinSelectMenu.Index == 0)
                    ChangeUnselectedColor(skinSelectMenu, 1);
                else if (actually == (GetPlayerSkinName() + playercipher) && (skinSelectMenu.Index == 1 || skinSelectMenu.Index == 2))
                    ChangeUnselectedColor(skinSelectMenu, 2);
                else
                    ChangeUnselectedColor(skinSelectMenu, 0);


                menu.Add(skinSelectMenu);
                skinSelectMenu.AddDescription(menu, TextDescription);
            }
            #endregion

            #region
            if (PortraitsSkins_records.Count > 0) {
                menu.Add(buildHeading(menu, "PortraitsXml"));
            }
            foreach (KeyValuePair<string, List<string>> recordID in DictionarySort(PortraitsSkins_records)) {
                string SpriteID = recordID.Key;

                string SpriteText = SpriteID;
                string TextDescription = "";

                if (SpriteText.Length > 18) {
                    int index;
                    for (index = 18; index < SpriteText.Length - 3; index++) {
                        if (char.IsUpper(SpriteText, index) || SpriteText[index] == '_' || index > 25) { break; }
                    }
                    if (index < SpriteText.Length - 3) {
                        TextDescription = "..." + SpriteText.Substring(index) + " ";
                        SpriteText = SpriteText.Remove(index) + "...";
                    }
                }
                if (Dialog.Has($"SkinModHelper_Portraits__{SpriteID}")) {
                    TextDescription = TextDescription + $"({Dialog.Clean($"SkinModHelper_Portraits__{SpriteID}")})";
                }


                TextMenu.Option<string> skinSelectMenu = new(SpriteText);
                if (!Settings.FreeCollocations_Portraits.ContainsKey(SpriteID)) {
                    Settings.FreeCollocations_Portraits[SpriteID] = DEFAULT;
                }
                allOptions.Add(skinSelectMenu);
                string actually = PortraitsSkin_record[SpriteID];

                skinSelectMenu.Change(skinId => {
                    actually = RefreshSkinValues_Portraits(SpriteID, skinId, inGame);

                    if (actually == ORIGINAL)
                        ChangeUnselectedColor(skinSelectMenu, 3);
                    else if (actually == null)
                        ChangeUnselectedColor(skinSelectMenu, 1);
                    else if (actually == (GetPlayerSkinName() + playercipher) && (skinSelectMenu.Index == 1 || skinSelectMenu.Index == 2))
                        ChangeUnselectedColor(skinSelectMenu, 2);
                    else
                        ChangeUnselectedColor(skinSelectMenu, 0);
                });
                string selected = Settings.FreeCollocations_Portraits[SpriteID];
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Original"), ORIGINAL, true);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Default"), DEFAULT, selected == DEFAULT);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_LockedToPlayer"), LockedToPlayer, selected == LockedToPlayer);


                foreach (string SkinName in recordID.Value) {
                    string SkinText;
                    if (Dialog.Has($"SkinModHelper_Portraits__{SpriteID}__{SkinName}"))
                        SkinText = Dialog.Clean($"SkinModHelper_Portraits__{SpriteID}__{SkinName}");
                    else if (!string.IsNullOrEmpty(OtherskinConfigs[SkinName].SkinDialogKey))
                        SkinText = Dialog.Clean(OtherskinConfigs[SkinName].SkinDialogKey);
                    else
                        SkinText = Dialog.Clean($"SkinModHelper_anyPortraits__{SkinName}");

                    skinSelectMenu.Add(SkinText, SkinName, (SkinName == selected));
                }

                if (selected == ORIGINAL)
                    ChangeUnselectedColor(skinSelectMenu, 3);
                else if (actually == null || skinSelectMenu.Index == 0)
                    ChangeUnselectedColor(skinSelectMenu, 1);
                else if (actually == (GetPlayerSkinName() + playercipher) && (skinSelectMenu.Index == 1 || skinSelectMenu.Index == 2))
                    ChangeUnselectedColor(skinSelectMenu, 2);
                else
                    ChangeUnselectedColor(skinSelectMenu, 0);

                menu.Add(skinSelectMenu);
                skinSelectMenu.AddDescription(menu, TextDescription);
            }
            #endregion

            #region
            if (OtherSkins_records.Count > 0) {
                menu.Add(buildHeading(menu, "OtherExtra"));
            }
            foreach (KeyValuePair<string, List<string>> recordID in DictionarySort(OtherSkins_records)) {
                string SpriteID = recordID.Key;

                string SpriteText = Dialog.Clean($"SkinModHelper_Other__{SpriteID}");
                string TextDescription = "";

                if (SpriteText.Length > 18) {
                    int index;

                    for (index = 18; index < SpriteText.Length - 3; index++) {
                        if (char.IsUpper(SpriteText, index) || SpriteText[index] == ' ' || index > 25) { break; }
                    }
                    if (index < SpriteText.Length - 3) {
                        TextDescription = "..." + (SpriteText[index] == ' ' ? SpriteText.Substring(index + 1) : SpriteText.Substring(index));
                        SpriteText = SpriteText.Remove(index) + "...";
                    }
                }


                TextMenu.Option<string> skinSelectMenu = new(SpriteText);
                if (!Settings.FreeCollocations_OtherExtra.ContainsKey(SpriteID)) {
                    Settings.FreeCollocations_OtherExtra[SpriteID] = DEFAULT;
                }
                allOptions.Add(skinSelectMenu);
                string actually = OtherSkin_record[SpriteID];

                skinSelectMenu.Change(skinId => {
                    actually = RefreshSkinValues_OtherExtra(SpriteID, skinId, inGame);

                    if (actually == ORIGINAL)
                        ChangeUnselectedColor(skinSelectMenu, 3);
                    else if (recordID.Value.Contains(GetPlayerSkinName() + playercipher) && (skinSelectMenu.Index == 1 || skinSelectMenu.Index == 2))
                        ChangeUnselectedColor(skinSelectMenu, 2);
                    else if (skinSelectMenu.Index == 2)
                        ChangeUnselectedColor(skinSelectMenu, 1);
                    else if (skinSelectMenu.Index == 1) {
                        ChangeUnselectedColor(skinSelectMenu, 1);
                        foreach (SkinModHelperConfig config in OtherskinConfigs.Values) {
                            if (recordID.Value.Contains(config.SkinName) && Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName]) {
                                ChangeUnselectedColor(skinSelectMenu, 0);
                                break;
                            }
                        }
                    } else
                        ChangeUnselectedColor(skinSelectMenu, 0);

                    UpdateParticles();
                });
                string selected = Settings.FreeCollocations_OtherExtra[SpriteID];
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Original"), ORIGINAL, true);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Default"), DEFAULT, selected == DEFAULT);
                skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_LockedToPlayer"), LockedToPlayer, selected == LockedToPlayer);


                foreach (string SkinName in recordID.Value) {
                    if (SkinName.EndsWith(playercipher)) { continue; }

                    string SkinText;
                    if (Dialog.Has($"SkinModHelper_Other__{SpriteID}__{SkinName}"))
                        SkinText = Dialog.Clean($"SkinModHelper_Other__{SpriteID}__{SkinName}");
                    else if (!string.IsNullOrEmpty(OtherskinConfigs[SkinName].SkinDialogKey))
                        SkinText = Dialog.Clean(OtherskinConfigs[SkinName].SkinDialogKey);
                    else
                        SkinText = Dialog.Clean($"SkinModHelper_anyOther__{SkinName}");

                    skinSelectMenu.Add(SkinText, SkinName, (SkinName == selected));
                }

                if (selected == ORIGINAL)
                    ChangeUnselectedColor(skinSelectMenu, 3);
                else if (recordID.Value.Contains(GetPlayerSkinName() + playercipher) && (skinSelectMenu.Index == 1 || skinSelectMenu.Index == 2))
                    ChangeUnselectedColor(skinSelectMenu, 2);
                else if (skinSelectMenu.Index == 2 || skinSelectMenu.Index == 0)
                    ChangeUnselectedColor(skinSelectMenu, 1);
                else if (skinSelectMenu.Index == 1) {
                    ChangeUnselectedColor(skinSelectMenu, 1);
                    foreach (SkinModHelperConfig config in OtherskinConfigs.Values)
                        if (recordID.Value.Contains(config.SkinName) && Settings.ExtraXmlList.ContainsKey(config.SkinName) && Settings.ExtraXmlList[config.SkinName]) {
                            ChangeUnselectedColor(skinSelectMenu, 0);
                            break;
                        }
                } else
                    ChangeUnselectedColor(skinSelectMenu, 0);


                menu.Add(skinSelectMenu);
                skinSelectMenu.AddDescription(menu, TextDescription);
            }
            #endregion

            foreach (var options in allOptions)
                options.Disabled = !Settings.FreeCollocations_OffOn;
        }
        #endregion

        //-----------------------------Method-----------------------------
        #region
        public static bool Disabled(bool inGame) {
            if (inGame) {
                Player player = Engine.Scene?.Tracker.GetEntity<Player>();
                if (player != null && player.StateMachine.State == Player.StIntroWakeUp) {
                    return true;
                }
            }
            return false;
        }
        /// <summary> 0 - White(default) / 1 - DimGray(false setting) / 2 - Goldenrod(special settings) / 3 - DarkGray(blocking skins)</summary>
        public static void ChangeUnselectedColor<T>(TextMenu.Option<T> options, int index) {
            Color color = Color.White;
            if (index == 1)
                color = Color.DimGray;
            else if (index == 2)
                color = Color.Goldenrod;
            else if (index == 3)
                color = Color.DarkGray;

            options.UnselectedColor = color;
        }
        public static Dictionary<string, T> DictionarySort<T>(Dictionary<string, T> dict) {
            dict = new(dict);
            var sorts = dict.OrderBy(dict => dict.Key, StringComparer.InvariantCulture).ToList();
            dict.Clear();
            foreach (var index in sorts) {
                dict[index.Key] = index.Value;
            }
            return dict;
        }
        #endregion
        #region
        // Reference to EverestCore.
        static public Action AddSearchBox(TextMenu menu, Overworld overworld = null) {
            TextMenuExt.TextBox textBox = new(overworld) {
                PlaceholderText = Dialog.Clean("MODOPTIONS_COREMODULE_SEARCHBOX_PLACEHOLDER")
            };

            TextMenuExt.Modal modal = new(textBox, null, 120);
            menu.Add(modal);

            Action<TextMenuExt.TextBox> searchNextMod(bool inReverse) => (TextMenuExt.TextBox textBox) => {
                string searchTarget = textBox.Text.ToLower();
                List<TextMenu.Item> menuItems = menu.Items;

                bool searchNextPredicate(TextMenu.Item item) {
                    string SearchTarget = item.SearchLabel();
                    int index = menu.IndexOf(item);
                    // Combine target's description into search.
                    if (index + 1 < menu.Items.Count && menu.Items[index + 1] is TextMenuExt.EaseInSubHeaderExt description) {
                        SearchTarget = (SearchTarget + description.Title).Replace("......", "");
                    }

                    return item.Visible && item.Selectable && !item.Disabled && SearchTarget != null && SearchTarget.ToLower().Contains(searchTarget);
                }


                if (TextMenuExt.TextBox.WrappingLinearSearch(menuItems, searchNextPredicate, menu.Selection + (inReverse ? -1 : 1), inReverse, out int targetSelectionIndex)) {
                    if (targetSelectionIndex >= menu.Selection) {
                        Audio.Play(SFX.ui_main_roll_down);
                    } else {
                        Audio.Play(SFX.ui_main_roll_up);
                    }
                    // make sure comment-content close when we leave or enter it by searching.
                    menu.Items[menu.Selection].OnLeave?.Invoke();
                    menu.Items[targetSelectionIndex].OnEnter?.Invoke();

                    menu.Selection = targetSelectionIndex;
                } else {
                    Audio.Play(SFX.ui_main_button_invalid);
                }
            };

            void exitSearch(TextMenuExt.TextBox textBox) {
                textBox.StopTyping();
                modal.Visible = false;
                textBox.ClearText();
            }

            textBox.OnTextInputCharActions['\t'] = searchNextMod(false);
            textBox.OnTextInputCharActions['\n'] = (_) => { };
            textBox.OnTextInputCharActions['\r'] = (textBox) => {
                if (MInput.Keyboard.CurrentState.IsKeyDown(Keys.LeftShift)
                    || MInput.Keyboard.CurrentState.IsKeyDown(Keys.RightShift)) {
                    searchNextMod(true)(textBox);
                } else {
                    searchNextMod(false)(textBox);
                }
            };
            textBox.OnTextInputCharActions['\b'] = (textBox) => {
                if (textBox.DeleteCharacter()) {
                    Audio.Play(SFX.ui_main_rename_entry_backspace);
                } else {
                    exitSearch(textBox);
                    Input.MenuCancel.ConsumePress();
                }
            };


            textBox.AfterInputConsumed = () => {
                if (textBox.Typing) {
                    if (Input.ESC.Pressed || Input.MenuLeft.Pressed || Input.MenuRight.Pressed) {
                        exitSearch(textBox);
                        Input.ESC.ConsumePress();
                    } else if (Input.MenuDown.Pressed) {
                        searchNextMod(false)(textBox);
                    } else if (Input.MenuUp.Pressed) {
                        searchNextMod(true)(textBox);
                    }
                }
            };

            return () => {
                if (menu.Focused) {
                    modal.Visible = true;
                    textBox.StartTyping();
                }
            };
        }
        #endregion
    }
    //-----------------------------Submenu System (from ExtendedVariant)-----------------------------
    #region
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
                        if (InputSearchUI.Instance != null)
                            InputSearchUI.Instance.ShowSearchUI = false;

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
                        if (InputSearchUI.Instance != null)
                            InputSearchUI.Instance.ShowSearchUI = false;

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
            InstanceUI.CreateAllOptions(category, false, false, false, null /* we don't care because there is no submenu */,
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
    #endregion

    //-----------------------------Search Button-----------------------------
    #region
    public class InputSearchUI : Entity {
        public VirtualButton Key;
        public static InputSearchUI Instance;
        public InputSearchUI(Overworld overworld) {
            Instance = this;

            Tag = Tags.HUD | Tags.PauseUpdate;
            Depth = -10000;
            Add(Wiggle);
            Overworld = overworld;
            Key = Input.QuickRestart;
        }
        private float WiggleDelay;
        private Wiggler Wiggle = Wiggler.Create(0.4f, 4f, null, false, false);

        public float inputEase;
        public bool ShowSearchUI;
        public Overworld Overworld;

        public override void Update() {
            if (Key.Pressed && WiggleDelay <= 0f) {
                Wiggle.Start();
                WiggleDelay = 0.5f;
            }
            WiggleDelay -= Engine.DeltaTime;
            inputEase = Calc.Approach(inputEase, (ShowSearchUI ? 1 : 0), Engine.DeltaTime * 4f);
            base.Update();
        }
        public override void Render() {
            if (inputEase > 0f) {
                float num = 0.5f;
                float num2 = Overworld?.ShowInputUI == true ? 48f : 0f;
                string label = Dialog.Clean("MAPLIST_SEARCH");
                float num3 = ButtonUI.Width(label, Key);

                Vector2 position = new Vector2(1880f, 1024f - num2);
                position.X += (40f + num3 * num + 32f) * (1f - Ease.CubeOut(inputEase));
                ButtonUI.Render(position, label, Key, num, 1f, Wiggle.Value * 0.05f, 1f);
            }
        }
    }
    #endregion
}