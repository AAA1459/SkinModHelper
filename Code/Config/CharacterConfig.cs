using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using YamlDotNet.Serialization;
using System.Text.RegularExpressions;
using FMOD.Studio;
using System;
using MonoMod.Utils;
using System.Linq;
using System.IO;
using System.Reflection;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.PlayerSkinSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public class CharacterConfig {
        #region Ctor / Initialization
        public CharacterConfig() {
        }

        public static CharacterConfig For(Image target) {
            DynamicData selfData = DynamicData.For(target);
            CharacterConfig config = selfData.Get<CharacterConfig>("smh_characterConfig");

            string rootPath = getAnimationRootPath(target);

            if (config == null || config.SourcePath != rootPath) {
                ModAsset asset = GetAssetOnSprite<AssetTypeYaml>(target, "skinConfig/CharacterConfig");
                config = AssetIntoConfig<CharacterConfig>(asset) ?? new();
                config.Source = asset;
                config.attached = target;
                config.SourcePath = rootPath;

                if (target is PlayerSprite playerSprite)
                    config.ModeInitialize(playerSprite.Mode);

                selfData.Set("smh_characterConfig", config);
            }
            if (target.Entity != config.lastEntity) {
                config.lastEntity = target.Entity;
                if (config.EntityTweaks != null && target is Sprite)
                    // Avoid multiple EntityTweaks works, make sure this target is the first of its entity. 
                    if (target == target.Entity?.Get<Sprite>())
                        config.ValuesTweak(target.Entity, config.EntityTweaks, config.TweaksTEST);
            }
            return config;
        }
        public void ModeInitialize(PlayerSpriteMode mode) {
            BadelineMode ??= mode == (PlayerSpriteMode)2 || mode == (PlayerSpriteMode)3;
            SilhouetteMode ??= mode == (PlayerSpriteMode)4;
        }
        #endregion

        #region Values 
        private Image attached;
        private Entity lastEntity;
        private ModAsset Source;
        private string SourcePath;
        #endregion

        #region Configurable values
        public bool? BadelineMode { get; set; }
        public bool? SilhouetteMode { get; set; }

        public string LowStaminaFlashColor { get; set; }
        public bool LowStaminaFlashHair { get; set; }
        public bool HoldableFacingFlipable { get; set; }

        public string TrailsColor { get; set; }
        public string DeathParticleColor { get; set; }


        public bool TweaksTEST;
        public List<Tweak> EntityTweaks { get; set; }
        public class Tweak {
            public string Name { get; set; }
            public string Value { get; set; }
            public string LimitOnType { get; set; }

            public bool subTEST;
            public List<Tweak> subTweaks { get; set; }
        }
        #endregion

        #region EntityTweaks Method
        private static List<Type> NotCloneList = new List<Type>() {
            typeof(Image)
        };
        public void ValuesTweak(object obj, List<Tweak> tweaks, bool TEST = false) {
            if (obj == null) {
                return;
            }
            Type type = obj.GetType();
            if (TEST) {
                string log = $"{SourcePath}skinConfig/CharacterConfig TEST on {type}:";
                Type type2 = type;
                while (type2 != null) {
                    FieldInfo[] fs = type2.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fs != null) {
                        for (int i = 0; i < fs.Length; i++) {
                            FieldInfo f = fs[i];
                            log += ("\n" + f);
                            if (f.FieldType.IsEnum) {
                                log += ": ";
                                foreach (string str in f.FieldType.GetEnumNames()) {
                                    log += (str + " ");
                                }
                            }
                        }
                    }
                    type2 = type2.BaseType;
                }
                Logger.Log(LogLevel.Info, "SkinModHelper", log);
            }

            for (int i = 0; i < tweaks.Count; i++) {
                Tweak t = tweaks[i];
                if (t.LimitOnType != null) {
                    bool match = true;
                    Type type2 = type;
                    while (t.LimitOnType != type2.FullName) {
                        if (match = t.LimitOnType == type2?.FullName)
                            break;
                        if ((type2 = type2.BaseType) == null)
                            break;
                    }
                    if (!match)
                        continue;
                }
                FieldInfo f = GetFieldPlus(type, t.Name);
                if (f == null) {
                    Logger.Log(TEST ? LogLevel.Warn : LogLevel.Info, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n Not found the Instance Field: {type}.{t.Name}");
                    continue;
                }

                object v = f.GetValue(obj);
                if (t.subTweaks != null) {
                    // Clone it first before modify e.g ParticleType.
                    if (!NotCloneList.Contains(f.FieldType)) {
                        v = CloneMethod.Invoke(v, null);
                    }

                    ValuesTweak(v, t.subTweaks, t.subTEST);
                    f.SetValue(obj, v);
                    continue;
                }

                if (t.Value != null) {
                    object v2 = v;
                    try {
                        // Check field type instead of value, for works even field's value is null.
                        if (f.FieldType == typeof(Sprite)) {
                            GFX.SpriteBank.CreateOn(v as Sprite, t.Value);
                            continue;
                        }
                        if (f.FieldType == typeof(Image)) {
                            if (GetTextureOnSprite(attached, t.Value, out var texture))
                                (v as Image).Texture = texture;
                            else
                                Logger.Log(LogLevel.Warn, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n texture {SourcePath}{t.Value} does not exist");
                            continue;
                        }

                        if (f.FieldType == typeof(List<MTexture>)) {
                            if (GetTexturesOnSprite(attached, t.Value, out var texture2)) {
                                v = texture2;
                            } else {
                                Logger.Log(LogLevel.Warn, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n textures {SourcePath}{t.Value} does not exist");
                            }
                        } else if (f.FieldType == typeof(MTexture)) {
                            if (GetTextureOnSprite(attached, t.Value, out var texture2)) {
                                v = texture2;
                            } else {
                                Logger.Log(LogLevel.Warn, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n texture {SourcePath}{t.Value} does not exist");
                            }
                        } else if (f.FieldType == typeof(Color)) {
                            v = Calc.HexToColorWithAlpha(t.Value);
                        } else if (f.FieldType.IsEnum) {
                            if (Enum.TryParse(f.FieldType, t.Value, true, out object _enum)) {
                                v = _enum;
                            } else if (int.TryParse(t.Value, out int v3)) {
                                v = v3;
                            } else {
                                Logger.Log(LogLevel.Error, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n '{f.FieldType} {type}.{t.Name}' IsEnum, but its new value is not number");
                            }
                        } else {
                            v = Convert.ChangeType(t.Value, f.FieldType);
                        }
                        f.SetValue(obj, v);
                    } catch (Exception e) {
                        Logger.Log(LogLevel.Error, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n '{f.FieldType} {type}.{t.Name}': \n   {e.Message}");
                        v = v2;
                        f.SetValue(obj, v);
                    }
                }
            }
        }
        #endregion
    }
}
