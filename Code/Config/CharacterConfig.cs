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
using Celeste.Mod.Helpers;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.PlayerSkinSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;
using System.Runtime.CompilerServices;
using static Celeste.Mod.SkinModHelper.CharacterConfig;

namespace Celeste.Mod.SkinModHelper {
    public class CharacterConfig {
        #region Ctor / Initialization
        internal const string _ConfigName = "skinConfig/CharacterConfig";

        internal static ConditionalWeakTable<Image, CharacterConfig> _Instance = new();

        public enum MaskModes {
            Red = 0, Green = 1, Blue = 2, Grayscale = 3
        }

        public CharacterConfig() {
        }

        public static CharacterConfig For(Image target) {
            string rootPath = getAnimationRootPath(target);
            if (!_Instance.TryGetValue(target, out CharacterConfig config) || config.SourcePath != rootPath) {

                ModAsset asset = GetAssetOnSprite<AssetTypeYaml>(target, _ConfigName);
                config = AssetIntoConfig<CharacterConfig>(asset) ?? new();
                config.Source = asset;
                config.attached = target;
                config.SourcePath = rootPath;

                if (target is PlayerSprite playerSprite)
                    config.ModeInitialize(playerSprite.Mode);

                // SilhouetteMode and TintGrayscaleWithHair are the almost same and conflicting. only the latter work when
                if (config.TintMaskWithHair) {
                    config.SilhouetteMode = false;

                } else if (config.SilhouetteMode == true) {
                    config.LowStaminaFlashHair = true;
                }
                config.ParticleModifierInit();

                _Instance.AddOrUpdate(target, config);
            }
            if (target.Entity != config.lastEntity) {
                config.lastEntity = target.Entity;
                // Avoid multiple EntityTweaks works, make sure this target is the first of its entity. 
                if (target is Sprite sprite && sprite == target.Entity?.Get<Sprite>())
                    config.ParticleModifyRefresh();

                if (config.EntityTweaks != null)
                    config.ValuesTweak(target.Entity, config.EntityTweaks, config.TweaksTEST);
            }
            return config;
        }
        public void ModeInitialize(PlayerSpriteMode mode) {
            BadelineMode ??= mode == (PlayerSpriteMode)2 || mode == (PlayerSpriteMode)3;
            SilhouetteMode ??= mode == (PlayerSpriteMode)4;
        }

        public void RefreshConflict() {
            if (TintMaskWithHair) {
                SilhouetteMode = false;

            } else if (SilhouetteMode == true) {
                LowStaminaFlashHair = true;
            }
        }
        #endregion

        #region Values 
        private Image attached;
        private Entity lastEntity;
        private ModAsset Source;
        private string SourcePath;

        /// <summary> uses when TintMaskWithHair is true </summary>
        internal Color effect_hairColor = Color.White;
        public string ColorGrade_Path;
        public Atlas ColorGrade_Atlas;

        public bool? HoldableFacingIsFront;

        [YamlIgnore]
        public Chooser<string> IdleColdOptions;
        [YamlIgnore]
        public Chooser<string> IdleWarmOptions;
        #endregion

        #region Configurable values
        public bool? BadelineMode { get; set; }

        /// <summary> Always false when TintMaskWithHair </summary>
        public bool? SilhouetteMode { get; set; }
        public bool TintMaskWithHair { get; set; }
        public MaskModes MaskMode { get; set; }
        public int _MaskMode => (int)MaskMode;


        /// <summary> Always true when SilhouetteMode </summary>
        public bool LowStaminaFlashHair { get; set; }
        public string LowStaminaFlashColor { get; set; }

        public bool HoldableFacingFlipable { get; set; }

        public string TrailsColor { get; set; }
        public string DeathParticleColor { get; set; }


        public float? IdleAnimationChance;

        [YamlMember(Alias = "IdleColdOptions")]
        public List<string> _IdleColdOptions {
            get => null; set {
                IdleColdOptions = new Chooser<string>();
                foreach (string option in value) {
                    string[] array = option.Split(',', 2, StringSplitOptions.TrimEntries);
                    float.TryParse((array.Length == 2 ? array[1] : "3"), out float f);
                    IdleColdOptions.Add(array[0].StartsWith("idle") ? array[0] : "idle" + array[0], Math.Max(0, f));
                }
            }
        }
        [YamlMember(Alias = "IdleWarmOptions")]
        public List<string> _IdleWarmOptions {
            get => null; set {
                IdleWarmOptions = new Chooser<string>();
                foreach (string option in value) {
                    string[] array = option.Split(',', 2, StringSplitOptions.TrimEntries);
                    float.TryParse((array.Length == 2 ? array[1] : "3"), out float f);
                    IdleWarmOptions.Add(array[0].StartsWith("idle") ? array[0] : "idle" + array[0], Math.Max(0, f));
                }
            }
        }

        public bool ColorGradingAfterColored { get; set; }
        #endregion

        #region ParticleModify
        public List<particleModifier> ParticleModify { get; set; }

        public class particleModifier {
            [YamlIgnore]
            public CharacterConfig Parent;
            [YamlIgnore]
            public ParticleType BaseParticle;
            [YamlIgnore]
            public FieldInfo BaseField;

            [YamlIgnore]
            public ParticleType NewParticle {
                get => newParticle ??= NewParticleInit(BaseParticle);
            }
            private ParticleType newParticle;

            public string TargetFullName { get; set; }
            public bool IsStatic = true;

            public string Source; // MTexture
            public List<string> SourceChooser; // Chooser<MTexture>
            public string Color;
            public string Color2;
            public string ColorMode; // ParticleType.ColorModes
            public string FadeMode; // ParticleType.FadeModes
            public float? SpeedMin;
            public float? SpeedMax;
            public float? SpeedMultiplier;
            public string Acceleration;
            public float? Friction;
            public float? Direction;
            public float? DirectionRange;
            public float? LifeMin;
            public float? LifeMax;
            public float? Size;
            public float? SizeRange;
            public float? SpinMin;
            public float? SpinMax;
            public bool? SpinFlippedChance;
            public string RotationMode; // ParticleType.RotationModes
            public bool? ScaleOut;
            public bool? UseActualDeltaTime;

            public ParticleType NewParticleInit(ParticleType baseParticle) {
                ParticleType particle = new ParticleType(baseParticle);

                if (Source != null) {
                    if (GetTextureOnSprite(Parent.attached, Source, out MTexture texture)) {
                        particle.SourceChooser = null;
                        particle.Source = texture;
                    } else {
                        Log(LogLevel.Error, $"{Parent.SourcePath}{_ConfigName} ParticleModify:\n   The texture {Parent.SourcePath}{Source} does not exist");
                    }
                } else if (SourceChooser != null) {
                    if (SourceChooser.Count == 0 || SourceChooser[0] == "null") {
                        particle.SourceChooser = null;
                    } else {
                        Chooser<MTexture> chooser = new Chooser<MTexture>();
                        foreach (string source in SourceChooser) {
                            if (GetTextureOnSprite(Parent.attached, source, out MTexture texture)) {
                                chooser.Add(texture, 1f);
                            } else {
                                Log(LogLevel.Error, $"{Parent.SourcePath}{_ConfigName} ParticleModify:\n   The texture {Parent.SourcePath}{Source} does not exist");
                            }
                        }
                        particle.SourceChooser = chooser;
                    }
                }

                if (RGBA_IsMatch(Color)) {
                    particle.Color = Calc.HexToColorWithAlpha(Color);
                }
                if (RGBA_IsMatch(Color2)) {
                    particle.Color2 = Calc.HexToColorWithAlpha(Color2);
                }
                if (ColorMode != null) {
                    if (Enum.TryParse(ColorMode, true, out ParticleType.ColorModes result)) {
                        particle.ColorMode = result;
                    } else if (int.TryParse(ColorMode, out int result2)) {
                        particle.ColorMode = (ParticleType.ColorModes)result2;
                    } else {
                        Log(LogLevel.Error, $"{Parent.SourcePath}{_ConfigName} ParticleModify:\n   The '{ColorMode}' is invalid ColorMode value.");
                    }
                }
                if (FadeMode != null) {
                    if (Enum.TryParse(FadeMode, true, out ParticleType.FadeModes result)) {
                        particle.FadeMode = result;
                    } else if (int.TryParse(FadeMode, out int result2)) {
                        particle.FadeMode = (ParticleType.FadeModes)result2;
                    } else {
                        Log(LogLevel.Error, $"{Parent.SourcePath}{_ConfigName} ParticleModify:\n   The '{FadeMode}' is invalid FadeMode value.");
                    }
                }
                if (SpeedMin != null) {
                    particle.SpeedMin = SpeedMin.Value;
                }
                if (SpeedMax != null) {
                    particle.SpeedMax = SpeedMax.Value;
                }
                if (SpeedMultiplier != null) {
                    particle.SpeedMultiplier = SpeedMultiplier.Value;
                }
                if (Acceleration != null) {
                    string[] array = Acceleration.Split(',', 2, StringSplitOptions.TrimEntries);
                    if (array.Length == 2 && float.TryParse(array[0], out float f) && float.TryParse(array[1], out float f2)) {
                        particle.Acceleration = new Vector2(f, f2);
                    } else {
                        Log(LogLevel.Error, $"{Parent.SourcePath}{_ConfigName} ParticleModify:\n   The Acceleration value should be an array with two floats than '{Acceleration}'.");
                    }
                }
                if (Friction != null) {
                    particle.Friction = Friction.Value;
                }
                if (Direction != null) {
                    particle.Direction = Direction.Value;
                }
                if (DirectionRange != null) {
                    particle.DirectionRange = DirectionRange.Value;
                }
                if (LifeMin != null) {
                    particle.LifeMin = LifeMin.Value;
                }
                if (LifeMax != null) {
                    particle.LifeMax = LifeMax.Value;
                }
                if (Size != null) {
                    particle.Size = Size.Value;
                }
                if (SizeRange != null) {
                    particle.SizeRange = SizeRange.Value;
                }
                if (SpinMin != null) {
                    particle.SpinMin = SpinMin.Value;
                }
                if (SpinMax != null) {
                    particle.SpinMax = SpinMax.Value;
                }
                if (SpinFlippedChance != null) {
                    particle.SpinFlippedChance = SpinFlippedChance.Value;
                }
                if (RotationMode != null) {
                    if (Enum.TryParse(RotationMode, true, out ParticleType.RotationModes result)) {
                        particle.RotationMode = result;
                    } else if (int.TryParse(RotationMode, out int result2)) {
                        particle.RotationMode = (ParticleType.RotationModes)result2;
                    } else {
                        Log(LogLevel.Error, $"{Parent.SourcePath}{_ConfigName} ParticleModify:\n   The '{RotationMode}' is invalid RotationMode value.");
                    }
                }
                if (ScaleOut != null) {
                    particle.ScaleOut = ScaleOut.Value;
                }
                if (UseActualDeltaTime != null) {
                    particle.UseActualDeltaTime = UseActualDeltaTime.Value;
                }
                return particle;
            }
        }
        private void ParticleModifierInit() {
            if (ParticleModify != null) {
                foreach (var m in ParticleModify) {
                    if (m.TargetFullName == null) {
                        Log(LogLevel.Error, $"{SourcePath}{_ConfigName} ParticleModify:\n   An unset TargetFullName value was found");
                        continue;
                    }
                    m.Parent = this;

                    // the value ends with ':' char are not supported by Yaml Deserialization... well that doesn't affect this.
                    int i = m.TargetFullName.LastIndexOf("::");
                    string ClassName = i < 0 ? m.TargetFullName : m.TargetFullName.Remove(i);

                    Type type;
                    if (string.IsNullOrEmpty(ClassName) || (type = Extensions.GetTypeFrom(ClassName)) == null) {
                        Log(LogLevel.Error, $"{SourcePath}{_ConfigName} ParticleModify:\n   Invalid type '{ClassName}'");
                        continue;
                    }
                    string FieldName = m.TargetFullName.Substring(Math.Min(i + 2, m.TargetFullName.Length));

                    FieldInfo field = m.IsStatic ? Extensions.GetStaticField(type, FieldName) : Extensions.GetField(type, FieldName);
                    if (field?.FieldType != typeof(ParticleType)) {
                        Log(LogLevel.Error, $"{SourcePath}{_ConfigName} ParticleModify:\n   Invalid field '{FieldName}' in '{ClassName}'");
                        continue;
                    }
                    m.BaseField = field;
                    if (m.IsStatic) {
                        m.BaseParticle = (ParticleType)field.GetValue(null);
                    }
                }
            }
        }
        private void ParticleModifyRefresh() {
            if (ParticleModify != null) {
                foreach (var m in ParticleModify) {
                    if (m.IsStatic || m.BaseField == null) {
                        continue;
                    }
                    try {
                        m.BaseField.SetValue(lastEntity, m.NewParticleInit((ParticleType)m.BaseField.GetValue(lastEntity)));
                    } catch (Exception e) {
                        Logger.Log(LogLevel.Error, "SkinModHelper", $"{SourcePath}{_ConfigName} ParticleModify:\n   {e.Message}");
                    }
                }
            }
        }
        #endregion

        #region EntityTweaks
        public bool TweaksTEST;
        public List<Tweak> EntityTweaks { get; set; }
        public class Tweak {
            public string Name { get => name; set {
                    string[] array = value.Split(',', 2, StringSplitOptions.TrimEntries);
                    if (array.Length == 2) {
                        Value = array[1];
                    }
                    name = array[0];
                }
            }
            private string name;
            public string Value { get; set; }
            public string LimitOnType { get; set; }

            public bool subTEST;
            public List<Tweak> subTweaks { get; set; }
        }

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
                FieldInfo f = Extensions.GetField(type, t.Name);
                if (f == null) {
                    Logger.Log(TEST ? LogLevel.Warn : LogLevel.Info, "SkinModHelper", $"{SourcePath}skinConfig/CharacterConfig Tweaks error: \n Invalid field '{t.Name}' in '{type}'");
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
