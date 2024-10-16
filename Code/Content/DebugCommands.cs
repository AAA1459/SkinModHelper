using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Reflection;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;
using System.ComponentModel.Design;

namespace Celeste.Mod.SkinModHelper {
    public static class DebugCommands {

        public const string HelpInfo = "SubCommands list: player(p), spriteidpath(sip), loglevel, settings, session";
        private const string Error = "Error";

        [Command("skinmodhelper", HelpInfo)]
        #region Process
#pragma warning disable CS0618
        public static void Process(string command, string command2, string command3, string command4) {
            if (string.IsNullOrWhiteSpace(command) || (command = command.ToLower()) == "help") {
                Send(HelpInfo);
                return;
            }
            bool help2 = string.IsNullOrWhiteSpace(command2) || (command2 = command2.ToLower()) == "help";
            bool help3 = string.IsNullOrWhiteSpace(command3) || (command3 = command3.ToLower()) == "help";
            bool help4 = string.IsNullOrWhiteSpace(command4) || (command4 = command4.ToLower()) == "help";
            string message = Error;

            switch (command) {
                case "settings":
                    if (help2) {
                        message = "Quick changes SkinModHelper setting. and available subcommands are \n  saving, backpack, disablehaircolor(dhc), disablehairlength(dhl), playerskinxmlgreatestpriority(psgp)";
                    } else {
                        #region
                        switch (command2) {
                            case "saving":
                                message = "Saved settings";
                                break;
                            case "backpack":
                                if (help3) {
                                    message = "Quick switch SkinModHelper backpack setting to... default, invert, on, off, or 0~3";
                                } else if (Enum.TryParse(command2, true, out SkinModHelperSettings.BackpackMode result)) {
                                    message = $"Changed SkinModHelper's backpack setting to '{smh_Settings.Backpack = result}'";
                                } else if (int.TryParse(command2, out int i) && i >= 0 && i < 4) {
                                    message = $"Changed SkinModHelper's backpack setting to '{smh_Settings.Backpack = (SkinModHelperSettings.BackpackMode)i}'";
                                }
                                break;

                            case "disablehaircolor" or "dhc" or "pshcd" or "playerskinhaircolorsdisabled":
                                if (help3) {
                                    message = "Quick switch hidden settings of SkinModHelper... onoff for PlayerSkinHairColorsDisabled, available subcommands are on, true, off, false";
                                } else if (TryParseToBoolen(command3, out bool boolen)) {
                                    message = $"Changed SkinModHelper's PlayerSkinHairColorsDisabled setting to '{smh_Settings.PlayerSkinHairColorsDisabled = boolen}'";
                                }
                                break;

                            case "disablehairlength" or "dhl" or "pshld" or "playerskinhairlengthsdisabled":
                                if (help3) {
                                    message = "Quick switch hidden settings of SkinModHelper... onoff for PlayerSkinHairLengthsDisabled, available subcommands are on, true, off, false";
                                } else if (TryParseToBoolen(command3, out bool boolen)) {
                                    message = $"Changed SkinModHelper's PlayerSkinHairLengthsDisabled setting to '{smh_Settings.PlayerSkinHairLengthsDisabled = boolen}'";
                                }
                                break;

                            case "psgp" or "playerskinxmlgreatestpriority":
                                if (help3) {
                                    message = "Quick switch hidden settings of SkinModHelper... onoff for PlayerSkinXmlGreatestPriority, available subcommands are on, true, off, false";
                                } else if (TryParseToBoolen(command3, out bool boolen)) {
                                    message = $"Changed SkinModHelper's PlayerSkinHairLengthsDisabled setting to '{smh_Settings.PlayerSkinGreatestPriority = boolen}'";
                                }
                                break;
                        }
                        #endregion
                        Instance.SaveSettings();
                    }
                    break;
                case "loglevel":
                    #region
                    if (help2) {
                        message = "Quick changes SkinModHelper loglevel. and available subcommands are verbose, debug, info, warn, error, or any-number, or now, last, current";
                    } else if (command == "now" || command == "last" || command == "current") {
                        message = $"Current SkinModHelper loglevel is '{Logger.GetLogLevel("SkinModHelper")}'";

                    } else if (!Enum.TryParse(command2, true, out LogLevel result)) {
                        Logger.SetLogLevel("SkinModHelper", result);
                        message = $"Changed SkinModHelper loglevel to '{result}'";

                    } else if (int.TryParse(command2, out int i) && i >= 0 && i < 5) {
                        Logger.SetLogLevel("SkinModHelper", (LogLevel)i);
                        message = $"Changed SkinModHelper loglevel to '{(LogLevel)i}'";
                    }
                    #endregion
                    break;
                case "player" or "p":
                    Player player = _Player;
                    PlayerSprite sprite = player?.Sprite;
                    if (sprite == null) {
                        message = "Can't find the player entity, If we are in maps?";
                    } else if (help2) {
                        message = "SubSubCommands list: id, path, colorgrade(cg), hairpath, mode";
                    } else {
                        #region
                        switch (command2) {
                            case "cg" or "colorgrade":
                                DynamicData spriteData = DynamicData.For(sprite);
                                Atlas atlas = spriteData.Get<Atlas>("ColorGrade_Atlas") ?? GFX.Game;
                                string path = spriteData.Get<string>("ColorGrade_Path");

                                message = $"The current colorgrade: {atlas.RelativeDataPath}{path}";
                                if (path == null || !atlas.Has(path))
                                    message = message + ", but it doesn't exist";
                                break;
                            case "id":
                                message = "The player spriteID: {sprite.spriteName}";
                                break;
                            case "mode":
                                message = $"The player mode: {(int)sprite.Mode} : (smh){Player_Skinid_verify} : {GetPlayerSkinName((int)sprite.Mode)}";
                                break;
                            case "path":
                                message = $"The player sprite's rootpath: {getAnimationRootPath(sprite)}";
                                break;
                            case "hairpath":
                                if (help3) {
                                    message = $"Outputs hair path,\n  usage: skinmodhelper player {command2} [segment]";
                                } else if (int.TryParse(command3, out int index)) {
                                    if (index >= 0 & index < sprite.HairCount)
                                        message = $"The current hair no.{index} segment path: {player.Hair.GetHairTexture(index)}";
                                    else
                                        message = $"Does not exist no.{index} segment of hair";
                                }
                                break;
                        }
                        #endregion
                    }
                    break;
                case "spriteidpath" or "sip":
                    if (help2) {
                        message = "Outputs the ID or its current skin's the root path.  ID is ID from Sprites.xml \n  skinmodhelper {command} [id] [optional anim-id]";
                    } else if (!GFX.SpriteBank.Has(command2)) {
                        message = $"Does not exist '{command2}' in Sprites.xml";
                    } else {
                        #region
                        string skin = Reskin_SpriteBank[command2];
                        Sprite sprite2 = GFX.SpriteBank.SpriteData[skin].Sprite;
                        skin = skin.Substring(command2.Length).Replace(playercipher, "(player)");

                        if (command3 == null) {
                            string path = getAnimationRootPath(sprite2);
                            message = $"'{command2}'--'{skin}' rootpath: {path}";
                        } else if (sprite2.Has(command3)) {
                            string path = getAnimationRootPath(sprite2, command3);
                            message = $"'{command2}'--'{skin}'--'{command3}' rootpath: {path}";
                        } else {
                            message = $"Does not exist anim '{command3}' in '{command2}'--'{skin}'";
                        }
                        #endregion
                    }
                    break;
                case "session":
                    if (smh_Session == null) {
                        message = "SkinModHelper session is null... cannot do commands";
                    } else if (help2) {
                        message = "Quick changes SkinModHelper session. and available subcommands are \n  saving, playerskin, generalskin, playeranimPrefixAddOn(p_animpref)";
                    } else {
                        int slot = SaveData.Instance.FileSlot;
                        #region
                        switch (command2) {
                            case "saving":
                                message = "Saved settings";
                                break;
                            case "playerskin":
                                if (help3) {
                                    message = $"Used set the session's player skin. \n  usage: skinmodhelper {command} {command2} [any-skin/null]";
                                } else if (command3 == "null" || command3 == "nil") {
                                    smh_Session.SelectedPlayerSkin = null;
                                    message = $"Changed session{slot}'s player skin to null";
                                    PlayerSkinSystem.RefreshPlayerSpriteMode();
                                } else {
                                    smh_Session.SelectedPlayerSkin = command3;
                                    message = $"Changed session{slot}'s player skin to '{command3}'";
                                    if (GetPlayerSkin(command3) == null)
                                        message = message + "...although it does not exist";
                                    PlayerSkinSystem.RefreshPlayerSpriteMode();
                                }
                                break;
                            case "playeranimprefixaddon" or "p_animpref":
                                if (help3) {
                                    message = "Used set the session's p_animpref that change anims e.g. 'idle' to '[p_animpref]idle' just them existing." +
                                        $"\n  usage: skinmodhelper {command} {command2} [any-value/null]";
                                } else if (command3 == "null" || command3 == "nil") {
                                    smh_Session.Player_animPrefixAddOn = null;
                                    message = $"Changed session{slot}'s p_animpref to null";
                                } else {
                                    message = $"Changed session{slot}'s p_animpref to '{smh_Session.Player_animPrefixAddOn = command3}'";
                                }
                                break;
                            case "generalskin":
                                if (help3 || help4) {
                                    message = $"Set the enabled status of general skin in session. \n  usage: skinmodhelper {command} {command2} [any-skin] [true/false/null]";
                                } else if (command4 == "null" || command4 == "nil") {
                                    SessionSet_GeneralSkin(command3, null);
                                    message = $"Changed session{slot}'s general skin '{command3}' to null";
                                    if (GetGeneralSkin(command3) == null)
                                        message = message + "...although it does not exist";

                                } else if (TryParseToBoolen(command4, out bool boolen)) {
                                    SessionSet_GeneralSkin(command3, boolen);
                                    message = $"Changed session{slot}'s general skin '{command3}' to {boolen}";
                                    if (GetGeneralSkin(command3) == null)
                                        message = message + "...although it does not exist";
                                }
                                break;
                        }
                        #endregion
                        Instance.WriteSession(slot, Instance.SerializeSession(slot));
                    }
                    break;
            }

            Send(message);
        }
#pragma warning restore CS0618
        public static bool TryParseToBoolen(string str, out bool boolen) {
            switch (str) {
                case "on" or "true" or "1":
                    boolen = true;
                    return true;
                case "off" or "false" or "0":
                    boolen = false;
                    return true;
            }
            boolen = false;
            return false;
        }
        #endregion

        #region send message
        public static void Send(string text) {
            Engine.Commands.Log(text);
        }
        #endregion
    }
}
