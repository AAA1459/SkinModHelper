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

namespace Celeste.Mod.SkinModHelper {
    public static class DebugCommands {

        public const string HelpInfo = "SubCommands list: player, spriteidpath(sip), loglevel, settings";

        [Command("skinmodhelper", HelpInfo)]
        #region Process
        public static void Process(string command, string name, string subname) {
            command = command?.ToLower();
            bool help = string.IsNullOrWhiteSpace(command) || command == "help";
            if (help) {
                Send(HelpInfo);
                return;
            }
            name = name?.ToLower();
            subname = subname?.ToLower();
            bool help2 = string.IsNullOrWhiteSpace(name) || name == "help";
            bool help3 = string.IsNullOrWhiteSpace(subname) || subname == "help";

            #region // command == "settings"
            if (command == "settings") {
                if (help2) {
                    Send("Quick changes SkinModHelper setting. and available subcommands are backpack, closehaircolor, closehairlength, playerskinxmlgreatestpriority(psgp)");
                    return;
                }
                switch (name) {
                    #region // subcommand == "backpack"
                    case "backpack":
                        if (help3) {
                            Send("Quick switch SkinModHelper backpack setting to... default, invert, on, off, or 0~3");
                            return;
                        }
                        switch (subname) {
                            case "default":
                                smh_Settings.Backpack = SkinModHelperSettings.BackpackMode.Default;
                                break;
                            case "invert":
                                smh_Settings.Backpack = SkinModHelperSettings.BackpackMode.Invert;
                                break;
                            case "on":
                                smh_Settings.Backpack = SkinModHelperSettings.BackpackMode.On;
                                break;
                            case "off":
                                smh_Settings.Backpack = SkinModHelperSettings.BackpackMode.Off;
                                break;
                            default:
                                if (int.TryParse(name, out int index) && index >= 0 && index < 4) {
                                    smh_Settings.Backpack = (SkinModHelperSettings.BackpackMode)index;
                                    break;
                                }
                                Send("error commands");
                                return;
                        }
                        Send($"Changed SkinModHelper's backpack setting to '{smh_Settings.Backpack}'");
                        break;
                    #endregion
                    #region // subcommand == "closehaircolor"
                    case "closehaircolor":
                    case "pshcd":
                    case "playerskinhaircolorsdisabled":
                        if (help3) {
                            Send("Quick switch hidden settings of SkinModHelper... onoff for PlayerSkinHairColorsDisabled, available subcommands are on, true, off, false");
                            return;
                        }
                        switch (subname) {
                            case "on":
                            case "true":
                                smh_Settings.PlayerSkinHairColorsDisabled = true;
                                break;
                            case "off":
                            case "false":
                                smh_Settings.PlayerSkinHairColorsDisabled = false;
                                break;
                            default:
                                Send("error commands");
                                return;
                        }
                        Send($"Changed SkinModHelper's PlayerSkinHairColorsDisabled setting to '{smh_Settings.PlayerSkinHairColorsDisabled}'");
                        break;
                    #endregion
                    #region // subcommand == "closehairlength"
                    case "closehairlength":
                    case "pshld":
                    case "playerskinhairlengthsdisabled":
                        if (help3) {
                            Send("Quick switch hidden settings of SkinModHelper... onoff for PlayerSkinHairLengthsDisabled, available subcommands are on, true, off, false");
                            return;
                        }
                        switch (subname) {
                            case "on":
                            case "true":
                                smh_Settings.PlayerSkinHairLengthsDisabled = true;
                                break;
                            case "off":
                            case "false":
                                smh_Settings.PlayerSkinHairLengthsDisabled = false;
                                break;
                            default:
                                Send("error commands");
                                return;
                        }
                        Send($"Changed SkinModHelper's PlayerSkinHairLengthsDisabled setting to '{smh_Settings.PlayerSkinHairLengthsDisabled}'");
                        break;
                    #endregion
                    #region // subcommand == "psgp"
                    case "psgp":
                    case "playerskinxmlgreatestpriority":
                        if (help3) {
                            Send("Quick switch hidden settings of SkinModHelper... onoff for PlayerSkinXmlGreatestPriority, available subcommands are on, true, off, false");
                            return;
                        }
                        switch (subname) {
                            case "on":
                            case "true":
                                smh_Settings.PlayerSkinGreatestPriority = true;
                                break;
                            case "off":
                            case "false":
                                smh_Settings.PlayerSkinGreatestPriority = false;
                                break;
                            default:
                                Send("error commands");
                                return;
                        }
                        Send($"Changed SkinModHelper's PlayerSkinXmlGreatestPriority setting to '{smh_Settings.PlayerSkinGreatestPriority}'");
                        break;
                    #endregion
                    default:
                        Send("error commands");
                        return;
                }
                Instance.SaveSettings();
                return;
            }
            #endregion

            #region // command == "loglevel"
            if (command == "loglevel") {
                if (help2) {
                    Send("Quick changes SkinModHelper loglevel. and available subcommands are verbose, debug, info, warn, error, or any-number, or now, last, current");
                    return;
                }
                switch (name) {
                    case "now":
                    case "current":
                    case "last":
                        Send($"current SkinModHelper loglevel is '{Logger.GetLogLevel("SkinModHelper")}'");
                        return;
                    case "verbose":
                        Logger.SetLogLevel("SkinModHelper", LogLevel.Verbose);
                        break;
                    case "debug":
                        Logger.SetLogLevel("SkinModHelper", LogLevel.Debug);
                        break;
                    case "info":
                        Logger.SetLogLevel("SkinModHelper", LogLevel.Info);
                        break;
                    case "warn":
                        Logger.SetLogLevel("SkinModHelper", LogLevel.Warn);
                        break;
                    case "error":
                        Logger.SetLogLevel("SkinModHelper", LogLevel.Error);
                        break;
                    default:
                        if (int.TryParse(name, out int index) && index >= 0) {
                            Logger.SetLogLevel("SkinModHelper", (LogLevel)index);
                            break;
                        }
                        Send("error commands");
                        return;
                }
                Send($"Changed SkinModHelper loglevel to '{Logger.GetLogLevel("SkinModHelper")}'");
                return;
            }
            #endregion

            #region // command == "player"
            if (command == "player" || command == "p") {
                if (help2) {
                    Send("SubSubCommands list: id, path, colorgrade(cg), hairpath, mode");
                    return;
                }

                Player player = (Engine.Scene as Level)?.Tracker?.GetEntity<Player>();
                PlayerSprite sprite = player?.Sprite;
                if (sprite != null) {
                    switch (name) {
                        case "cg":
                        case "colorgrade":
                            DynamicData spriteData = DynamicData.For(sprite);
                            Atlas atlas = spriteData.Get<Atlas>("ColorGrade_Atlas") ?? GFX.Game;
                            string path = spriteData.Get<string>("ColorGrade_Path");
                            if (path != null && atlas.Has(path))
                                Send($"The current colorgrade: {atlas.DataPath}/{path}");
                            else
                                Send($"The current colorgrade: {atlas.DataPath}/{path}, but it doesn't exist");
                            break;
                        case "id":
                            Send($"The player spriteID: {sprite.spriteName}");
                            break;
                        case "mode":
                            Send($"The player mode: {(int)sprite.Mode} : (smh){Player_Skinid_verify} : {GetPlayerSkinName((int)sprite.Mode)}");
                            break;
                        case "path":
                            Send($"The player sprite's rootpath: {getAnimationRootPath(sprite)}");
                            break;
                        case "hairpath":
                            if (help3) {
                                Send("Outputs hair path,\n  usage: skinmodhelper player {name} [segment]");
                                break;
                            }
                            if (int.TryParse(subname, out int index)) {
                                if (index >= 0 & index < sprite.HairCount)
                                    Send($"The current hair no.{index} segment path: {player.Hair.GetHairTexture(index)}");
                                else
                                    Send($"Does not exist no.{index} segment of hair");
                                break;
                            }
                            break;
                    }
                } else {
                    Send("Can't find the player entity, If we are in maps?");
                }
                return;
            }
            #endregion

            #region // command == "spriteidpath"
            if (command == "spriteidpath" || command == "sip") {
                if (help2) {
                    Send("Outputs the ID or its current skin's the root path.  ID is ID from Sprites.xml");
                    Send($" skinmodhelper {command} [id] [optional anim-id]");
                    return;
                }
                if (GFX.SpriteBank.Has(name)) {
                    string skin = Reskin_SpriteBank[name];
                    Sprite sprite = GFX.SpriteBank.SpriteData[skin].Sprite;
                    skin = skin.Substring(name.Length).Replace(playercipher, "(player)");

                    if (subname == null) {
                        string path = getAnimationRootPath(sprite);
                        Send($"'{name}'--'{skin}' rootpath: {path}");
                        return;
                    }
                    if (sprite.Has(subname)) {
                        string path = getAnimationRootPath(sprite, subname);
                        Send($"'{name}'--'{skin}'--'{subname}' rootpath: {path}");
                        return;
                    }

                    Send($"Does not exist anim '{subname}' in '{name}'--'{skin}'");
                    return;
                }
                Send($"Does not exist '{name}' in Sprites.xml");
                return;
            }
            #endregion

            Send("error commands");
        }
        #endregion

        public static readonly int pagelength = 360;
        public static void Send(string text, int? page = null) {
            if (page == null) {
                Engine.Commands.Log(text);
                return;
            }
            int pages = Math.Max(text.Length / pagelength, 0);
            page = Calc.Clamp((int)page, 0, pages);

            if (text.Length < ((int)page * pagelength))
                text = text.Substring((int)page * pagelength);

            if (text.Length >= ((int)page + 1) * pagelength)
                text = text.Remove(((int)page + 1) * pagelength);

            Engine.Commands.Log(text);
            if (pages > 0)
                Engine.Commands.Log($"  current page is {page}/{pages}");
        }
    }
}
