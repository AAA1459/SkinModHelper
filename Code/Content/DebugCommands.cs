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

        public const string HelpInfo = "SubCommands list: player, p, spriteidpath, sip";

        [Command("skinmodhelper", HelpInfo)]
        #region
        public static void Process(string subCommand, string name, string subname) {
            subCommand = subCommand?.ToLower();
            if (string.IsNullOrWhiteSpace(subCommand) || subCommand == "help") { Send(HelpInfo); return; }

            if (subCommand == "player" || subCommand == "p") {
                #region
                name = name?.ToLower();
                if (string.IsNullOrWhiteSpace(name) || name == "help") {
                    Send("SubSubCommands list: id, path, colorgrade, cg, hairpath");
                } else {
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
                        } else if (name == "id") {
                            string id = DynamicData.For(sprite).Get<string>("spriteName");
                            Send($"The player spriteID: {id}");
                        } else if (name == "path") {
                            string path = getAnimationRootPath(sprite);
                            Send($"The player sprite's rootpath: {path}");
                        } else if (name == "hairpath") {
                            subname = subname?.ToLower();
                            if (string.IsNullOrWhiteSpace(subname) || subname == "help") {
                                Send("Outputs hair path, usage:");
                                Send($" skinmodhelper player {name} [segment]");
                            } else if (int.TryParse(subname, out int index)) {
                                if (index >= 0 & index < sprite.HairCount)
                                    Send($"The current hair no.{index} segment path: {player.Hair.GetHairTexture(index)}");
                                else
                                    Send($"Does not exist no.{index} segment of hair");
                            }
                        }
                    } else {
                        Send("Can't find the player entity, If we are in maps?");
                    }
                }
                #endregion
            } else if (subCommand == "spriteidpath" || subCommand == "sip") {
                #region
                if (string.IsNullOrWhiteSpace(name) || name.ToLower() == "help") {
                    Send("Outputs the ID or its current skin's the root path.  ID is ID from Sprites.xml");
                    Send($" skinmodhelper {subCommand} [id] [optional anim-id]");
                } else if (GFX.SpriteBank.Has(name)) {
                    string skin = GetSpriteBankIDSkin(name);
                    Sprite sprite = GFX.SpriteBank.SpriteData[skin].Sprite;
                    skin = skin.Substring(name.Length).Replace(playercipher, "(player)");

                    if (subname == null) {
                        string path = getAnimationRootPath(sprite);
                        Send($"'{name}'--'{skin}' rootpath: {path}");
                    } else if (sprite.Has(subname)) {
                        string path = getAnimationRootPath(sprite, subname);
                        Send($"'{name}'--'{skin}'--'{subname}' rootpath: {path}");
                    } else {
                        Send($"Does not exist anim '{subname}' in '{name}'--'{skin}'");
                    }
                } else {
                    Send($"Does not exist '{name}' in Sprites.xml");
                }
                #endregion
            }
        }
        #endregion

        public static void Send(string text) {
            Engine.Commands.Log(text);
        }
    }
}
