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
using static Celeste.Mod.SkinModHelper.SkinModHelperSettings;

namespace Celeste.Mod.SkinModHelper
{
    public class SkinModHelperUI {
        #region Create Options
        public static SkinModHelperSettings Settings => (SkinModHelperSettings)Instance._Settings;
        public static SkinModHelperSession Session => (SkinModHelperSession)Instance._Session;

        public NewMenuCategory Category;
        public bool OptionsDisabled;


        public enum NewMenuCategory {
            SkinFreeConfig, None
        }
        public void CreateAllOptions(NewMenuCategory category, bool includeMasterSwitch, bool includeCategorySubmenus, bool includeRandomizer,
            Action submenuBackAction, TextMenu menu, bool inGame, bool forceEnabled) {

            Category = category;
            OptionsDisabled = Disabled(inGame);

            if (category == NewMenuCategory.None) {

                int skins = 0;
                HashSet<string> categories = new();
                foreach (var config in skinConfigs.Values) {
                    if (config.Player_List || config.Silhouette_List) {
                        skins++;
                        categories.Add(config.Mod);
                    }
                }
                foreach (var config in OtherskinConfigs.Values) {
                    if (config.General_List) {
                        skins++;
                        categories.Add(config.Mod);
                    }
                }
                string desc = Dialog.Clean("SkinModHelper_LoadedSkinCount").Replace("-0-", $"{categories.Count}").Replace("-1-", $"{skins}");
                menu.Add(CreateDescriptionWithoutClean(menu, desc, new(0f, 2.5f), Color.SlateGray, 10f, true));
                // ---

                if (OptionsDisabled) {
                    menu.Add(CreateDescription(menu, "SkinModHelper_OptionsDisabled", GetColor(3), 0f, true));
                }
                BuildPlayerSkinSelectMenu(menu, inGame);
                BuildExSkinsMenu(menu, inGame);
                menu.Add(BuildMoreOptionsMenu(menu, inGame, includeCategorySubmenus, submenuBackAction));

            } else if (category == NewMenuCategory.SkinFreeConfig) {
                Build_SkinFreeConfig_NewMenu(menu, inGame);
            }

            InputSearchUI.Instance.RegisterMenuEvents(menu, showSearchUI: category == NewMenuCategory.SkinFreeConfig);
        }
        #endregion

        #region // Player Skins Options

        private int playermenu_index;
        private int playermenu_initindex;
        private bool lastShowOtherSelfVariants;

        private void BuildPlayerSkinSelectMenu(TextMenu menu, bool inGame) {
            lastShowOtherSelfVariants = Settings.ShowOtherSelfVariants;

            playermenu_index = 0;
            playermenu_initindex = 0;
            string selected = Settings.SelectedPlayerSkin;

            TextMenuExt.OptionSubMenu options_lists = new(Dialog.Clean("SkinModHelper_Settings_PlayerSkin")) { ItemIndent = 25f };

            var conf = CreateDescription(menu, "SkinModHelper_PlayerSkinConfirmed", Color.Goldenrod);
            var conf_v = CreateDescription(menu, "SkinModHelper_VanillaConfirmed", Color.Goldenrod);
            var conf_req = CreateDescription(menu, "SkinModHelper_NeedConfirm", Color.OrangeRed);

            bool doSessionHint = Engine.Scene is Level && (smh_Session?.SelectedPlayerSkin != null || smh_Session.SelectedOtherSelfSkin != null || smh_Session.SelectedSilhouetteSkin != null);
            var sessionHint = CreateDescription(menu, "SkinModHelper_SessionHint", Color.SteelBlue, 0f, doSessionHint);

            options_lists.Add(Dialog.Clean("SkinModHelper_Settings_DefaultPlayer"), new());
            options_lists.OnValueChange += (index2) => {
                // everest will call the OnEnter of first-option of currentmenu before entering there... i hate it.
                foreach (var item in options_lists.CurrentMenu)
                    if (item is TextMenuExt.EaseInSubHeaderExt item2 && item2.TextColor == Color.Gray)
                        item2.FadeVisible = false;
                conf_v.FadeVisible = conf.FadeVisible = false;
                conf_req.FadeVisible = playermenu_index != index2;
            };

            Dictionary<string, List<SkinModHelperConfig>> mods = new() {
                { "SkinModHelper_Settings_AllMod", new() }
            };
            foreach (var config in skinConfigs.Values) {
                if (!(config.Player_List || config.Silhouette_List) || Settings.HideSkinsInOptions.Contains(config.SkinName))
                    continue;
                if (!mods.ContainsKey(config.Mod))
                    mods.Add(config.Mod, new());
                mods["SkinModHelper_Settings_AllMod"].Add(config);
                mods[config.Mod].Add(config);
            }

            Action[] _OnPressed = new Action[mods.Count];

            options_lists.OnPressed += () => {
                playermenu_index = options_lists.MenuIndex;

                if (playermenu_index == 0) {
                    UpdatePlayerSkin(selected = DEFAULT, inGame);
                    UpdateOtherSelfSkin(DEFAULT, inGame);
                    UpdateSilhouetteSkin(DEFAULT, inGame);
                    if (conf_req.FadeVisible == true)
                        conf_v.FadeVisible = true;
                } else {
                    _OnPressed[playermenu_index - 1].Invoke();
                }
                if (doSessionHint) {
                    smh_Session.SetPlayerSkin(null);
                    smh_Session.SetSilhouetteSkin(null);
                    smh_Session.SetOtherSelfSkin(null);
                    sessionHint.FadeVisible = false;
                }
                conf_req.FadeVisible = false;
            };
            foreach (var configs in mods) {
                playermenu_index++;
                bool inAll = playermenu_index == 1;

                List<TextMenu.Item> options = new();
                TextMenu.Option<string> option = new(Dialog.Clean("SkinModHelper_Settings_PlayerSkinVariants"));
                if (inAll) {
                    option.Add(Dialog.Clean("SkinModHelper_Settings_DefaultPlayer"), DEFAULT);
                }
                options.Add(option);

                List<(string, TextMenuExt.EaseInSubHeaderExt)> descriptions = new();
                Dictionary<string, TextMenuExt.EaseInSubHeaderExt> modfrom_descs = new();

                foreach (var config in configs.Value) {
                    if (!config.Player_List)
                        continue;
                    string DialogID = !string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : "SkinModHelper_Player__" + config.SkinName;
                    string Text = Dialog.Clean(DialogID);
                    option.Add(Text, config.SkinName, config.SkinName == selected);

                    if (inAll && Settings.ShowSkinSourceInAllCategory) {
                        if (!modfrom_descs.TryGetValue(config.Mod, out var modfrom_desc)) {
                            string modfrom_descN = Dialog.Clean("SkinModHelper_Category").Replace("-0-", GetModName(config.Mod));
                            modfrom_descs[config.Mod] = modfrom_desc = CreateDescriptionWithoutClean(menu, modfrom_descN);
                            options.Add(modfrom_desc);
                        }
                        option.OnEnter += () => {
                            if (selected == config.SkinName) { modfrom_desc.FadeVisible = true; }
                        };
                        option.OnLeave += () => modfrom_desc.FadeVisible = false;
                    }
                    if (Dialog.Has(DialogID + "__Description")) {
                        var _desc = CreateDescription(menu, DialogID + "__Description");
                        option.OnEnter += () => _desc.FadeVisible = selected == config.SkinName;
                        option.OnLeave += () => _desc.FadeVisible = false;

                        descriptions.Add((config.SkinName, _desc));
                        options.Add(_desc);
                    }
                }
                ChangeUnselectedColor(option, 3);
                if (!inAll && selected == (option.Values.Count > 0 ? option.Values[option.PreviousIndex].Item2 : DEFAULT)) {
                    options_lists.SetInitialSelection(playermenu_initindex = playermenu_index);
                }

                option.OnValueChange += (skinId) => {
                    UpdatePlayerSkin(selected = skinId, inGame);
                    if (!lastShowOtherSelfVariants) {
                        UpdateOtherSelfSkin(selected, inGame);
                    }
                    conf.FadeVisible = true;
                    foreach (var d in descriptions)
                        d.Item2.FadeVisible = selected == d.Item1;
                };
                if (inAll) {
                    option.OnValueChange += (skinId) => {
                        string mod2 = skinId == DEFAULT ? null : skinConfigs[skinId].Mod;
                        foreach (var d in modfrom_descs)
                            d.Value.FadeVisible = mod2 == d.Key;
                    };
                }
                option.OnLeave += () => {
                    conf.FadeVisible = false;
                };
                _OnPressed[playermenu_index - 1] += () => {
                    string skin = option.Values.Count > 0 ? option.Values[option.Index].Item2 : DEFAULT;
                    if (Settings.SelectedPlayerSkin != skin) {
                        UpdatePlayerSkin(selected = skin, inGame);
                        conf.FadeVisible = true;
                    }
                    option.OnEnter?.Invoke();
                };
                InsertOtherSelfVariant(configs.Value, menu, options_lists, _OnPressed, options, inGame, conf);
                InsertSilhouetteVariant(configs.Value, menu, options_lists, _OnPressed, options, inGame, conf);

                // Prevent multi-line desc from causing the bottom option to flash
                var placeholder = CreateDescriptionWithoutClean(menu, "", Color.Goldenrod, 3f, true);
                options.Add(placeholder);

                options_lists.Add(GetModName(configs.Key), options);
                inAll = false;
            }

            menu.Add(options_lists);
            playermenu_index = options_lists.MenuIndex;
            options_lists.OnLeave += () => {
                conf_v.FadeVisible = conf.FadeVisible = false;
            };
            int index2 = menu.IndexOf(options_lists) + 1;
            menu.Insert(index2, conf);
            menu.Insert(index2, conf_v);
            menu.Insert(index2, sessionHint);
            menu.Insert(index2, conf_req);

            if (OptionsDisabled)
                options_lists.Disabled = true;
            if ((playermenu_index == 0 && selected != DEFAULT) || (!lastShowOtherSelfVariants && selected != Settings.SelectedOtherSelfSkin)) {
                playermenu_index = -1;
                conf_req.FadeVisible = true;
            }
        }
        #endregion

        #region // Silhouette Variants Options
        private void InsertSilhouetteVariant(List<SkinModHelperConfig> configs, 
            TextMenu menu, TextMenuExt.OptionSubMenu options_lists, Action[] lists_OnPressed, List<TextMenu.Item> options, bool inGame, TextMenuExt.EaseInSubHeaderExt conf) {

            string selected = Settings.SelectedSilhouetteSkin;
            TextMenu.Option<string> option = new(Dialog.Clean("SkinModHelper_Settings_PlayerSkinVariants_P"));

            bool inAll = playermenu_index == 1;
            if (inAll) {
                option.Add(Dialog.Clean("SkinModHelper_Settings_DefaultPlayer"), DEFAULT);
            }
            options.Add(option);

            List<(string, TextMenuExt.EaseInSubHeaderExt)> descriptions = new();
            Dictionary<string, TextMenuExt.EaseInSubHeaderExt> modfrom_descs = new();

            foreach (var config in configs) {
                if (!config.Silhouette_List)
                    continue;
                string DialogID = !string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : "SkinModHelper_Player__" + config.SkinName;
                string Text = Dialog.Clean(DialogID);
                option.Add(Text, config.SkinName, config.SkinName == selected);

                if (inAll && Settings.ShowSkinSourceInAllCategory) {
                    if (!modfrom_descs.TryGetValue(config.Mod, out var modfrom_desc)) {
                        string modfrom_descN = Dialog.Clean("SkinModHelper_Category").Replace("-0-", GetModName(config.Mod));
                        modfrom_descs[config.Mod] = modfrom_desc = CreateDescriptionWithoutClean(menu, modfrom_descN);
                        options.Add(modfrom_desc);
                    }
                    option.OnEnter += () => {
                        if (selected == config.SkinName) { modfrom_desc.FadeVisible = true; }
                    };
                    option.OnLeave += () => modfrom_desc.FadeVisible = false;
                }
                if (Dialog.Has(DialogID + "__Description")) {
                    TextMenuExt.EaseInSubHeaderExt _desc = CreateDescription(menu, DialogID + "__Description");
                    option.OnEnter += () => _desc.FadeVisible = selected == config.SkinName;
                    option.OnLeave += () => _desc.FadeVisible = false;

                    descriptions.Add((config.SkinName, _desc));
                    options.Add(_desc);
                }
            }
            ChangeUnselectedColor(option, 3);
            if (playermenu_initindex == playermenu_index && (selected != (option.Values.Count > 0 ? option.Values[option.PreviousIndex].Item2 : DEFAULT))) {
                options_lists.SetInitialSelection(playermenu_initindex = 1);
            } else if (playermenu_initindex == 0 && !inAll && option.Values.Count > 0 && (selected == option.Values[option.PreviousIndex].Item2)) {
                options_lists.SetInitialSelection(playermenu_initindex = 1);
            }

            option.OnValueChange += (skinId) => {
                UpdateSilhouetteSkin(selected = skinId, inGame);
                conf.FadeVisible = true;
                foreach (var d in descriptions)
                    d.Item2.FadeVisible = selected == d.Item1;
            };
            if (inAll) {
                option.OnValueChange += (skinId) => {
                    string mod2 = skinId == DEFAULT ? null : skinConfigs[skinId].Mod;
                    foreach (var d in modfrom_descs)
                        d.Value.FadeVisible = mod2 == d.Key;
                };
            }
            option.OnLeave += () => conf.FadeVisible = false;

            lists_OnPressed[playermenu_index - 1] += () => {
                string skin = option.Values.Count > 0 ? option.Values[option.Index].Item2 : DEFAULT;
                if (Settings.SelectedSilhouetteSkin != skin) {
                    UpdateSilhouetteSkin(selected = skin, inGame);
                    conf.FadeVisible = true;
                }
            };
        }
        #endregion

        #region // Other Self Variants Options
        private void InsertOtherSelfVariant(List<SkinModHelperConfig> configs,
            TextMenu menu, TextMenuExt.OptionSubMenu options_lists, Action[] lists_OnPressed, List<TextMenu.Item> options, bool inGame, TextMenuExt.EaseInSubHeaderExt conf) {

            if (!lastShowOtherSelfVariants) {
                lists_OnPressed[playermenu_index - 1] += () => {
                    if (Settings.SelectedOtherSelfSkin != Settings.SelectedPlayerSkin) {
                        UpdateOtherSelfSkin(Settings.SelectedPlayerSkin, inGame);
                        conf.FadeVisible = true;
                    }
                };
                return;
            }
            string selected = Settings.SelectedOtherSelfSkin;
            TextMenu.Option<string> option = new(Dialog.Clean("SkinModHelper_Settings_PlayerSkinVariants_Bad"));

            bool inAll = playermenu_index == 1;
            if (inAll) {
                option.Add(Dialog.Clean("SkinModHelper_Settings_DefaultPlayer"), DEFAULT);
            }
            options.Add(option);

            List<(string, TextMenuExt.EaseInSubHeaderExt)> descriptions = new();
            Dictionary<string, TextMenuExt.EaseInSubHeaderExt> modfrom_descs = new();

            foreach (var config in configs) {
                if (!config.Player_List)
                    continue;
                string DialogID = !string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : "SkinModHelper_Player__" + config.SkinName;
                string Text = Dialog.Clean(DialogID);
                option.Add(Text, config.SkinName, config.SkinName == selected);

                if (inAll && Settings.ShowSkinSourceInAllCategory) {
                    if (!modfrom_descs.TryGetValue(config.Mod, out var modfrom_desc)) {
                        string modfrom_descN = Dialog.Clean("SkinModHelper_Category").Replace("-0-", GetModName(config.Mod));
                        modfrom_descs[config.Mod] = modfrom_desc = CreateDescriptionWithoutClean(menu, modfrom_descN);
                        options.Add(modfrom_desc);
                    }
                    option.OnEnter += () => {
                        if (selected == config.SkinName) { modfrom_desc.FadeVisible = true; }
                    };
                    option.OnLeave += () => modfrom_desc.FadeVisible = false;
                }
                if (Dialog.Has(DialogID + "__Description")) {
                    TextMenuExt.EaseInSubHeaderExt _desc = CreateDescription(menu, DialogID + "__Description");
                    option.OnEnter += () => _desc.FadeVisible = selected == config.SkinName;
                    option.OnLeave += () => _desc.FadeVisible = false;

                    descriptions.Add((config.SkinName, _desc));
                    options.Add(_desc);
                }
            }
            ChangeUnselectedColor(option, 3);
            if (playermenu_initindex == playermenu_index && (selected != (option.Values.Count > 0 ? option.Values[option.PreviousIndex].Item2 : DEFAULT))) {
                options_lists.SetInitialSelection(playermenu_initindex = 1);
            } else if (playermenu_initindex == 0 && !inAll && option.Values.Count > 0 && (selected == option.Values[option.PreviousIndex].Item2)) {
                options_lists.SetInitialSelection(playermenu_initindex = 1);
            }

            option.OnValueChange += (skinId) => {
                UpdateOtherSelfSkin(selected = skinId, inGame);
                conf.FadeVisible = true;
                foreach (var d in descriptions)
                    d.Item2.FadeVisible = selected == d.Item1;
            };
            if (inAll) {
                option.OnValueChange += (skinId) => {
                    string mod2 = skinId == DEFAULT ? null : skinConfigs[skinId].Mod;
                    foreach (var d in modfrom_descs)
                        d.Value.FadeVisible = mod2 == d.Key;
                };
            }
            option.OnLeave += () => conf.FadeVisible = false;

            lists_OnPressed[playermenu_index - 1] += () => {
                string skin = option.Values.Count > 0 ? option.Values[option.Index].Item2 : DEFAULT;
                if (Settings.SelectedOtherSelfSkin != skin) {
                    UpdateOtherSelfSkin(selected = skin, inGame);
                    conf.FadeVisible = true;
                }
            };
        }
        #endregion 

        #region // General Skins Options       
        public void BuildExSkinsMenu(TextMenu menu, bool inGame) {
            TextMenuExt.OptionSubMenu options_lists = new(Dialog.Clean("SkinModHelper_Settings_Otherskin")) { ItemIndent = 25f };
            options_lists.Add(Dialog.Clean("SkinModHelper_Settings_Otherskin_null"), new());

            if (OptionsDisabled) {
                options_lists.Disabled = true;
                menu.Add(options_lists);
                return;
            }
            Dictionary<string, List<SkinModHelperConfig>> mods = new();
            foreach (var config in OtherskinConfigs.Values) {
                if (!config.General_List)
                    continue;
                if (!mods.ContainsKey(config.Mod))
                    mods.Add(config.Mod, new());
                mods[config.Mod].Add(config);
            }
            foreach (var configs in mods) {
                List<TextMenu.Item> options = new();

                foreach (var config in configs.Value) {
                    bool OnOff = false;

                    string DialogID = !string.IsNullOrEmpty(config.SkinDialogKey) ? config.SkinDialogKey : ("SkinModHelper_ExSprite__" + config.SkinName);
                    string Text = Dialog.Clean(DialogID);

                    if (!Settings.ExtraXmlList.ContainsKey(config.SkinName))
                        Settings.ExtraXmlList.Add(config.SkinName, false);
                    else
                        OnOff = Settings.ExtraXmlList[config.SkinName];

                    TextMenu.OnOff option = new TextMenu.OnOff(Text, OnOff);
                    bool doSessionHint = Engine.Scene is Level && smh_Session != null && smh_Session.ExtraXmlList.ContainsKey(config.SkinName);
                    var sessionHint = CreateDescription(menu, "SkinModHelper_SessionHint_Alt", Color.SteelBlue, 0f, doSessionHint);

                    var conf_on = CreateDescription(menu, "SkinModHelper_GeneralSkin_TurnedIntoOn", Color.Goldenrod);
                    var conf_off = CreateDescription(menu, "SkinModHelper_GeneralSkin_TurnedIntoOff", Color.Goldenrod);
                    option.OnLeave += delegate {
                        conf_on.FadeVisible = false;
                        conf_off.FadeVisible = false;
                    };

                    options.Add(option);

                    options.Add(sessionHint);
                    options.Add(conf_off);
                    options.Add(conf_on);

                    option.Change(OnOff => {
                        UpdateGeneralSkin(config.SkinName, OnOff, inGame);
                        sessionHint.FadeVisible = false;
                        conf_on.FadeVisible = OnOff;
                        conf_off.FadeVisible = !OnOff;
                    });
                    if (Dialog.Has(DialogID + "__Description")) {
                        TextMenuExt.EaseInSubHeaderExt _text = CreateDescription(menu, DialogID + "__Description");
                        option.OnEnter += delegate {
                            _text.FadeVisible = true;
                        };
                        option.OnLeave += delegate {
                            _text.FadeVisible = false;
                        };
                        options.Add(_text);
                    }
                    ChangeUnselectedColor(option, 3);
                }
                options_lists.Add(Dialog.Has(configs.Key) ? Dialog.Clean(configs.Key) : configs.Key, options);
            }
            menu.Add(options_lists);

            var changeHint = CreateDescription(menu, "SkinModHelper_Settings_Otherskin_onoffHint", Color.Gray, 0f, false);
            options_lists.OnValueChange += delegate {
                // everest will call the OnEnter of first-option of currentmenu before entering there... i hate it.
                foreach (var item in options_lists.CurrentMenu)
                    if (item is TextMenuExt.EaseInSubHeaderExt item2 && item2.TextColor == Color.Gray)
                        item2.FadeVisible = false;
                changeHint.FadeVisible = options_lists.MenuIndex == 0;
            };
            options_lists.OnEnter += delegate {
                if (options_lists.MenuIndex == 0)
                    changeHint.FadeVisible = true;
            };
            options_lists.OnLeave += delegate {
                changeHint.FadeVisible = false;
            };
            if (inGame)
                menu.Insert(menu.IndexOf(options_lists) + 1, changeHint);
        }
        #endregion

        #region // Advanced Options Menu
        public TextMenuExt.SubMenu BuildMoreOptionsMenu(TextMenu menu, bool inGame, bool includeCategorySubmenus, Action submenuBackAction) {
            return new TextMenuExt.SubMenu(Dialog.Clean("SkinModHelper_MORE_OPTIONS"), false).Apply(subMenu => {
                if (OptionsDisabled) {
                    subMenu.Disabled = true;
                    return;
                }
                TextMenuButtonExt SpriteSubmenu;
                subMenu.Add(SpriteSubmenu = AbstractSubmenu.BuildOpenMenuButton<OuiCategorySubmenu>(menu, inGame, submenuBackAction, new object[] { NewMenuCategory.SkinFreeConfig }));                // ---
                                                                                                                                                                                                       // ---

                var bp = CreateBackpackEntry(inGame);
                subMenu.Add(bp);
                bp.AddDescription(subMenu, menu, Dialog.Clean("SkinModHelper_Desc_Backpack"));
                // ---

                TextMenu.OnOff bhgm = new TextMenu.OnOff(Dialog.Clean("SkinModHelper_Settings_BetterHairGrowsMotion"), Settings.BetterHairMotionOnGrows);
                bhgm.Change(OnOff => {
                    Settings.BetterHairMotionOnGrows = OnOff;
                });
                subMenu.Add(bhgm);
                bhgm.AddDescription(subMenu, menu, Dialog.Clean("SkinModHelper_Desc_BetterHairGrowsMotion"));
                // ---

                TextMenu.OnOff sosv = new TextMenu.OnOff(Dialog.Clean("SkinModHelper_Settings_ShowOtherSelfVariants"), Settings.ShowOtherSelfVariants);
                sosv.Change(OnOff => {
                    Settings.ShowOtherSelfVariants = OnOff;
                });
                // ---

                subMenu.Add(sosv);
                sosv.AddDescription(subMenu, menu, Dialog.Clean("SkinModHelper_Desc_ShowOtherSelfVariants"));
                TextMenuExt.EaseInSubHeaderExt needReloadMenu_desc = CreateDescription(menu, "SkinModHelper_NeedReloadMenu", Color.OrangeRed);
                subMenu.Add(needReloadMenu_desc);
                sosv.OnEnter += () => needReloadMenu_desc.FadeVisible = true;
                sosv.OnLeave += () => needReloadMenu_desc.FadeVisible = false;
                // ---

                TextMenu.OnOff sssiac = new TextMenu.OnOff(Dialog.Clean("SkinModHelper_Settings_ShowSkinSourceInAllCategory"), Settings.ShowSkinSourceInAllCategory);
                sssiac.Change(OnOff => {
                    Settings.ShowSkinSourceInAllCategory = OnOff;
                });
                subMenu.Add(sssiac);
                sssiac.AddDescription(subMenu, menu, Dialog.Clean("SkinModHelper_Desc_ShowSkinSourceInAllCategory"));
                TextMenuExt.EaseInSubHeaderExt needReloadMenu_desc2 = CreateDescription(menu, "SkinModHelper_NeedReloadMenu", Color.OrangeRed);
                subMenu.Add(needReloadMenu_desc2);
                sssiac.OnEnter += () => needReloadMenu_desc2.FadeVisible = true;
                sssiac.OnLeave += () => needReloadMenu_desc2.FadeVisible = false;
            });
        }
        private TextMenu.Item CreateBackpackEntry(bool inGame) {
            Array enumValues = Enum.GetValues(typeof(BackpackMode));
            Array.Sort((int[])enumValues);
            TextMenu.Item item = new TextMenu.Slider("SkinModHelper_Settings_Backpack".DialogClean(),
                    i => {
                        string enumName = enumValues.GetValue(i).ToString();
                        return $"SkinModHelper_Settings_Backpack_{enumName}".DialogClean();
                    }, 0, enumValues.Length - 1, (int)Settings.Backpack)
                .Change(value => Settings.Backpack = (BackpackMode)value);
            return item;
        }
        #endregion

        #region // Precisely Skin Choose
        public void Build_SkinFreeConfig_NewMenu(TextMenu menu, bool inGame) {
            List<TextMenu.Option<string>> allOptions = new();
            TextMenu.OnOff SkinFreeConfig_OnOff = new TextMenu.OnOff(Dialog.Clean("SkinModHelper_SkinFreeConfig_OnOff"), Settings.FreeCollocations_OffOn);

            SkinFreeConfig_OnOff.Change(OnOff => {
                RefreshSkinValues(OnOff, inGame);
                foreach (var options in allOptions) {
                    options.Disabled = !OnOff;
                }
            });
            menu.Add(SkinFreeConfig_OnOff);
            menu.Add(CreateDescription(menu, "SkinModHelper_SkinFreeConfig_Warning", Color.Gray, 0f, true));

            #region
            foreach (var respriteBank in RespriteBankModule.ManagedInstance()) {
                if (respriteBank.Settings == null || respriteBank.SkinsRecords.Count == 0)
                    continue;
                menu.Add(buildHeading(menu, respriteBank.O_SubMenuName));
                string prefix = $"SkinModHelper_{respriteBank.O_DescriptionPrefix}__";

                foreach (KeyValuePair<string, List<string>> recordID in DictionarySort(respriteBank.SkinsRecords)) {
                    string SpriteId = recordID.Key;

                    string SpriteText = respriteBank is nonBankReskin ? Dialog.Clean(prefix + SpriteId) : SpriteId;
                    string TextDescription = "";

                    if (SpriteText.Length > 18) {
                        int index;
                        for (index = 18; index < SpriteText.Length - 3; index++)
                            if (char.IsUpper(SpriteText, index) || SpriteText[index] == '_' || index > 25) { break; }

                        if (index < SpriteText.Length - 3) {
                            TextDescription = "..." + SpriteText.Substring(index) + " ";
                            SpriteText = SpriteText.Remove(index) + "...";
                        }
                    }
                    if (Dialog.Has(prefix + SpriteId) && respriteBank is not nonBankReskin)
                        TextDescription = TextDescription + "(" + Dialog.Clean(prefix + SpriteId) + ")";

                    if (!respriteBank.Settings.ContainsKey(SpriteId))
                        respriteBank.Settings[SpriteId] = DEFAULT;
                    TextMenu.Option<string> skinSelectMenu = new(SpriteText);

                    allOptions.Add(skinSelectMenu);
                    string actually = respriteBank[SpriteId];

                    skinSelectMenu.Change(skinId => {
                        actually = respriteBank.SetSettings(SpriteId, skinId);

                        if (actually == ORIGINAL)
                            ChangeUnselectedColor(skinSelectMenu, 3);
                        else if (actually == null)
                            ChangeUnselectedColor(skinSelectMenu, 1);
                        else if (actually == (GetPlayerSkinName() + playercipher) && (skinSelectMenu.Index == 1 || skinSelectMenu.Index == 2))
                            ChangeUnselectedColor(skinSelectMenu, 2);
                        else
                            ChangeUnselectedColor(skinSelectMenu, 0);
                    });
                    string selected = respriteBank.Settings[SpriteId];
                    skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Original"), ORIGINAL, true);
                    skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_Default"), DEFAULT, selected == DEFAULT);
                    skinSelectMenu.Add(Dialog.Clean("SkinModHelper_anyXmls_LockedToPlayer"), LockedToPlayer, selected == LockedToPlayer);

                    foreach (string SkinName in recordID.Value) {
                        string SkinText;
                        if (Dialog.Has($"{prefix}{SpriteId}__{SkinName}"))
                            SkinText = Dialog.Clean($"{prefix}{SpriteId}__{SkinName}");
                        else if (!string.IsNullOrEmpty(OtherskinConfigs[SkinName].SkinDialogKey))
                            SkinText = Dialog.Clean(OtherskinConfigs[SkinName].SkinDialogKey);
                        else
                            SkinText = Dialog.Clean($"SkinModHelper_anySprite__{SkinName}");

                        skinSelectMenu.Add(SkinText, SkinName, (SkinName == selected));
                    }
                    if (selected == ORIGINAL)
                        ChangeUnselectedColor(skinSelectMenu, 3);
                    else if (actually == null && skinSelectMenu.Index < 3)
                        ChangeUnselectedColor(skinSelectMenu, 1);
                    else if (actually == (GetPlayerSkinName() + playercipher) && (skinSelectMenu.Index == 1 || skinSelectMenu.Index == 2))
                        ChangeUnselectedColor(skinSelectMenu, 2);
                    else {
                        ChangeUnselectedColor(skinSelectMenu, 0);
                    }

                    menu.Add(skinSelectMenu);
                    skinSelectMenu.AddDescription(menu, TextDescription);
                }
            }
            #endregion

            foreach (var options in allOptions)
                options.Disabled = !Settings.FreeCollocations_OffOn;
        }
        #endregion

        #region Method

        public static bool Disabled(bool inGame) {
            if (inGame) {
                Player player = Engine.Scene?.Tracker.GetEntity<Player>();
                if (player != null && player.StateMachine.State == Player.StIntroWakeUp) {
                    return true;
                }
            }
            return false;
        }
        /// <summary> 0 - White(default) / 1 - DimGray(false setting) / 2 - Goldenrod(special settings) / 3 - DarkGray(blocking skins/sub options)</summary>
        private static void ChangeUnselectedColor<T>(TextMenu.Option<T> options, int index) {
            options.UnselectedColor = GetColor(index);
        }
        /// <returns> 0 - White / 1 - DimGray / 2 - Goldenrod / 3 - DarkGray / 4 - SteelBlue</returns>
        private static Color GetColor(int index) {
            switch (index) {
                case 1:
                    return Color.DimGray;
                case 2:
                    return Color.Goldenrod;
                case 3:
                    return Color.DarkGray;
                case 4:
                    return Color.SteelBlue;
            }
            return Color.White;
        }

        private static string GetModName(string key) {
            if (Dialog.Has(key))
                return Dialog.Clean(key);
            if (Dialog.Has("modname_" + key))
                return Dialog.Clean("modname_" + key);
            return key;
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
        private TextMenuExt.EaseInSubHeaderExt CreateDescription(TextMenu menu, string dialog,
            Color? textColor = null, float heightExtra = 0f, bool initVisible = false) {
            if (textColor == null) {
                textColor = Color.Gray;
            }
            return new(Dialog.Clean(dialog), initVisible, menu) {
                TextColor = textColor.Value,
                HeightExtra = heightExtra,
                IncludeWidthInMeasurement = false,
            };
        }
        private TextMenuExt.EaseInSubHeaderExt CreateDescriptionWithoutClean(TextMenu menu, string dialog,
            Color? textColor = null, float heightExtra = 0f, bool initVisible = false) {

            return new(dialog, initVisible, menu) {
                TextColor = textColor ?? Color.Gray,
                HeightExtra = heightExtra,
                IncludeWidthInMeasurement = false,
            };
        }
        private TextMenuExt.EaseInSubHeaderExt CreateDescriptionWithoutClean(TextMenu menu, string dialog, Vector2 offset,
            Color? textColor = null, float heightExtra = 0f, bool initVisible = false) {

            return new(dialog, initVisible, menu) {
                Offset = offset,
                TextColor = textColor ?? Color.Gray,
                HeightExtra = heightExtra,
                IncludeWidthInMeasurement = false,
            };
        }

        private TextMenu.SubHeader buildHeading(TextMenu menu, string headingNameResource) {
            return new TextMenu.SubHeader(Dialog.Clean($"SkinModHelper_NewSubMenu_{headingNameResource}"));
        }
        #endregion

        #region SearchBox Reimplement (Reference from EverestCore)
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

    #region Submenu System (from ExtendedVariant)
    public static class CommonExtensions
    {
        internal static TextMenuExt.SubMenu Apply(this TextMenuExt.SubMenu obj, Action<TextMenuExt.SubMenu> action)
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

    #region Search Button UI
    public class InputSearchUI : Entity {
        #region Hooks
        public static void Load() {
            On.Celeste.Overworld.ctor += onOverworldConstruct;
        }
        public static void Unload() {
            On.Celeste.Overworld.ctor -= onOverworldConstruct;
        }
        private static void onOverworldConstruct(On.Celeste.Overworld.orig_ctor orig, Overworld self, OverworldLoader loader) {
            orig(self, loader);
            Instance = new InputSearchUI(self);
        }
        #endregion

        private static VirtualButton key => Input.QuickRestart;
        public static InputSearchUI Instance;
        public InputSearchUI(Overworld overworld) {
            Instance = this;

            Tag = Tags.HUD | Tags.PauseUpdate;
            Depth = -10000;
            Add(wiggler);
            this.overworld = overworld;
        }
        private bool showSearchUI;
        private float wiggleDelay;
        private readonly Wiggler wiggler = Wiggler.Create(0.4f, 4f);
        private float inputEase;
        private Overworld overworld;

        public override void Update() {
            if (key.Pressed && wiggleDelay <= 0f) {
                wiggler.Start();
                wiggleDelay = 0.5f;
            }
            wiggleDelay -= Engine.DeltaTime;
            inputEase = Calc.Approach(inputEase, (showSearchUI ? 1 : 0), Engine.DeltaTime * 4f);
            base.Update();
        }
        public override void Render() {
            if (inputEase > 0f) {
                float num = 0.5f;
                float num2 = overworld?.inputEase > 0f ? 48f : 0f;
                string label = Dialog.Clean("MAPLIST_SEARCH");
                float num3 = ButtonUI.Width(label, key);

                Vector2 position = new Vector2(1880f, 1024f - num2);
                position.X += (40f + num3 * num + 32f) * (1f - Ease.CubeOut(inputEase));
                ButtonUI.Render(position, label, key, num, 1f, wiggler.Value * 0.05f, 1f);
            }
        }
        public void RegisterMenuEvents(TextMenu menu, bool showSearchUI) {
            this.showSearchUI = showSearchUI;
            if (!showSearchUI)
                return;

            Overworld overworld = Engine.Scene as Overworld;
            // make sure the button is part of the current scene (Level or Overworld)
            if (Scene != Engine.Scene) {
                Engine.Scene.Add(this);
                this.overworld = overworld;
            }
            Action startSearching = SkinModHelperUI.AddSearchBox(menu, overworld);
            //menu.Remove(menu.Items[menu.Items.Count - 1]);

            menu.OnClose += () => this.showSearchUI = false;
            menu.OnUpdate += () => {
                if (key.Pressed)
                    startSearching.Invoke();
            };
        }
    }
    #endregion
}