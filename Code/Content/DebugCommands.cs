﻿using FMOD.Studio;
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

        public const string HelpInfo = "SubCommands list: player(p), spriteidpath, sip";

        [Command("skinmodhelper", HelpInfo)]
        #region Process
        public static void Process(string subCommand, string name, string subname) {
            subCommand = subCommand?.ToLower();
            if (string.IsNullOrWhiteSpace(subCommand) || subCommand == "help") { Send(HelpInfo); return; }

            #region // subCommand == "player"
            if (subCommand == "player" || subCommand == "p") {
                name = name?.ToLower();
                if (string.IsNullOrWhiteSpace(name) || name == "help") {
                    Send("SubSubCommands list: id, path, colorgrade(cg), hairpath, mode");
                    return;
                }

                Player player = (Engine.Scene as Level)?.Tracker?.GetEntity<Player>();
                PlayerSprite sprite = player?.Sprite;
                if (sprite != null) {
                    if (name == "colorgrade" || name == "cg") {
                        DynamicData spriteData = DynamicData.For(sprite);

                        Atlas atlas = spriteData.Get<Atlas>("ColorGrade_Atlas") ?? GFX.Game;
                        string path = spriteData.Get<string>("ColorGrade_Path");

                        if (path != null && atlas.Has(path))
                            Send($"The current colorgrade: {atlas.DataPath}/{path}");
                        else
                            Send($"The current colorgrade: {atlas.DataPath}/{path}, but it doesn't exist");
                        return;
                    }
                    if (name == "id") {
                        Send($"The player spriteID: {DynamicData.For(sprite).Get<string>("spriteName")}");
                        return;
                    }
                    if (name == "mode") {
                        Send($"The player mode: {(int)sprite.Mode} : (smh){Player_Skinid_verify} : {GetPlayerSkinName((int)sprite.Mode)}");
                        return;
                    }
                    if (name == "path") {
                        string path = getAnimationRootPath(sprite);
                        Send($"The player sprite's rootpath: {path}");
                        return;
                    }
                    if (name == "hairpath") {
                        subname = subname?.ToLower();
                        if (string.IsNullOrWhiteSpace(subname) || subname == "help") {
                            Send("Outputs hair path, usage:");
                            Send($" skinmodhelper player {name} [segment]");
                            return;
                        }
                        if (int.TryParse(subname, out int index)) {
                            if (index >= 0 & index < sprite.HairCount)
                                Send($"The current hair no.{index} segment path: {player.Hair.GetHairTexture(index)}");
                            else
                                Send($"Does not exist no.{index} segment of hair");
                            return;
                        }
                    }
                    return;
                }
                Send("Can't find the player entity, If we are in maps?");
                return;
            }
            #endregion

            #region // subCommand == "spriteidpath"
            if (subCommand == "spriteidpath" || subCommand == "sip") {
                if (string.IsNullOrWhiteSpace(name) || name.ToLower() == "help") {
                    Send("Outputs the ID or its current skin's the root path.  ID is ID from Sprites.xml");
                    Send($" skinmodhelper {subCommand} [id] [optional anim-id]");
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
