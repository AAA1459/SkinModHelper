using FMOD.Studio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using Celeste.Mod.UI;
using System.Xml;
using System.Linq;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {
    public class RespriteBankModule {
        #region Method
        public static List<RespriteBankModule> InstanceList = new();
        private static Dictionary<string, RespriteBankModule> cache = new();
        public static bool SearchInstance(SpriteBank bank, out RespriteBankModule bank2) {
            if (cache.TryGetValue(bank.XMLPath, out bank2))
                return bank2 != null;

            return (cache[bank.XMLPath] = bank2 = InstanceList.Find(bank3 => bank3.Basebank == bank)) != null;
        }
        public static List<RespriteBankModule> ManagedInstance() {
            return InstanceList.FindAll(bank3 => bank3.runhosting);
        }
        #endregion 

        #region Ctor / Values
        public RespriteBankModule(string xml_name, bool runhosting) {
            XML_name = xml_name;
            this.runhosting = runhosting;

            if (InstanceList.Remove(InstanceList.Find(bank => bank.GetType() == this.GetType())))
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Warnning!  RespriteBank '{this.GetType()}' be created twice!");
            cache?.Clear();
            InstanceList.Add(this);
        }


        public string XML_name;
        public virtual SpriteBank Basebank { get; }
        public virtual Dictionary<string, string> Settings { get; }

        public string O_SubMenuName;
        public string O_DescriptionPrefix;

        public Dictionary<string, string> CurrentSkins = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> SkinsRecords = new(StringComparer.OrdinalIgnoreCase);

        public virtual bool SettingsActive {
            get => smh_Settings.FreeCollocations_OffOn;
        }
        public bool Active = true;
        public bool runhosting { get; private set; }
        #endregion

        #region Record / Combine
        public virtual void DoRecord(string skinId, string directory, string cipher = "") {
            if (string.IsNullOrEmpty(skinId) || Basebank == null)
                return;
            SpriteBank newBank = BuildBank(Basebank, skinId, "Graphics/" + directory + "/" + XML_name);
            if (newBank == null)
                return;
            foreach (KeyValuePair<string, SpriteData> spriteDataEntry in newBank.SpriteData) {
                string spriteId = spriteDataEntry.Key;
                if (Basebank.SpriteData.ContainsKey(spriteId)) {

                    if (!SkinsRecords.ContainsKey(spriteId))
                        SkinsRecords.Add(spriteId, new());
                    if (cipher != playercipher && !SkinsRecords[spriteId].Contains(skinId + cipher))
                        SkinsRecords[spriteId].Add(skinId + cipher);
                }
            }
        }
        public virtual void ClearRecord() {
            SkinsRecords.Clear();
        }
        // Combine skin mod XML with a vanilla sprite bank
        public virtual void DoCombine(string skinId, string directory, string cipher = "") {
            if (string.IsNullOrEmpty(skinId) || Basebank == null)
                return;
            SpriteBank newBank = BuildBank(Basebank, skinId, "Graphics/" + directory + "/" + XML_name);
            if (newBank == null)
                return;
            foreach (KeyValuePair<string, SpriteData> spriteDataEntry in newBank.SpriteData) {
                string spriteId = spriteDataEntry.Key;
                if (Basebank.SpriteData.TryGetValue(spriteId, out SpriteData origSpriteData)) {
                    SpriteData newSpriteData = spriteDataEntry.Value;
                    PatchSprite(origSpriteData.Sprite, newSpriteData.Sprite);

                    string newSpriteId = spriteId + skinId + cipher;

                    foreach (SpriteDataSource source in origSpriteData.Sources)
                        newSpriteData.Sources.Add(source);
                    Basebank.SpriteData[newSpriteId] = newSpriteData;

                    OnCombine(newSpriteId, newSpriteData);
                }
            }
        }
        public virtual void OnCombine(string n_name, SpriteData n_data) { }

        public virtual void DoRefresh(bool inGame) {
            foreach (string spriteId in SkinsRecords.Keys) {
                SetCurrentSkin(spriteId, SettingsActive && Settings.TryGetValue(spriteId, out var value) ? value ?? DEFAULT : DEFAULT);
            }
        }

        public virtual string SetSettings(string spriteId, string skinID) {
            if (Settings == null)
                return null;
            Settings[spriteId] = skinID;
            if (SettingsActive)
                return SetCurrentSkin(spriteId, skinID);
            return skinID;
        }
        #endregion

        #region Method #2
        public virtual string this[string SpriteID] {
            get {
                return Active && CurrentSkins.TryGetValue(SpriteID, out var value) ? value : null;
            }
            set => SetCurrentSkin(SpriteID, value);
        }

        public virtual string GetCurrentSkin(string SpriteID) {
            if (Active && CurrentSkins.TryGetValue(SpriteID, out var value)) {
                return SpriteID + value;
            }
            return SpriteID;
        }

        public virtual string SetCurrentSkin(string SpriteID, string SkinID) {
            if (SkinID == DEFAULT || SkinID == LockedToPlayer) {
                return CurrentSkins[SpriteID] = GetDefaultSkin(SpriteID, SkinID);
            }

            return CurrentSkins[SpriteID] = SkinID;
        }
        public virtual string GetDefaultSkin(string SpriteID, string cipher) {
            string SkinID = null;
            string playerSkinName = GetPlayerSkinName(Player_Skinid_verify);

            if (playerSkinName != null && Basebank.Has(SpriteID + playerSkinName + playercipher)) {
                SkinID = playerSkinName + playercipher;
                if (smh_Settings.PlayerSkinGreatestPriority)
                    return SkinID;
            }
            if (cipher == LockedToPlayer)
                return SkinID;
            foreach (SkinModHelperConfig config in GetEnabledGeneralSkins())
                if (Basebank.Has(SpriteID + config.SkinName))
                    SkinID = config.SkinName;
            return SkinID;
        }
        #endregion
    }
    #region RespriteBank
    public class RespriteBank : RespriteBankModule {
        public RespriteBank(string xml_name, string menu_name, string prefix) : base(xml_name, true) {
            O_SubMenuName = menu_name;
            O_DescriptionPrefix = prefix;
        }
        public override SpriteBank Basebank { get => GFX.SpriteBank; }
        public override Dictionary<string, string> Settings { get => smh_Settings.FreeCollocations_Sprites; }
        public override void OnCombine(string n_name, SpriteData n_data) {

            if (n_data.Sources[0].XML["Metadata"] != null)
                PlayerSprite.CreateFramesMetadata(n_name);  // Check if skin have metadata... no matter what skin it is.
        }
    }
    #endregion

    #region ReportraitsBank
    public class ReportraitsBank : RespriteBankModule {
        public ReportraitsBank(string xml_name, string menu_name, string prefix) : base(xml_name, true) {
            O_SubMenuName = menu_name;
            O_DescriptionPrefix = prefix;
        }
        public override SpriteBank Basebank { get => GFX.PortraitsSpriteBank; }
        public override Dictionary<string, string> Settings { get => smh_Settings.FreeCollocations_Portraits; }
    }
    #endregion

    #region nonBankReskin / Other Sprite
    public class nonBankReskin : RespriteBankModule {
        #region Ctor / Values
        public nonBankReskin(string menu_name, string prefix) : base(null, true) {
            O_SubMenuName = menu_name;
            O_DescriptionPrefix = prefix;
        }
        public override Dictionary<string, string> Settings { get => smh_Settings.FreeCollocations_OtherExtra; }

        public Dictionary<string, Tuple<Atlas, string, string>> PathSpriteId = new();
        public Dictionary<string, Tuple<Atlas, string, string>> PathStaticSpriteId = new();

        public Dictionary<string, string> SkinIdPath = new(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Record / Combine
        public void AddSpriteInfo(string storageId, Atlas atlas, string orig_path, bool isStatic = true) {
            if (isStatic)
                PathStaticSpriteId[atlas.RelativeDataPath + orig_path] = new(atlas, orig_path, storageId);
            else
                PathSpriteId[atlas.RelativeDataPath + orig_path] = new(atlas, orig_path, storageId);
        }

        public override void ClearRecord() {
            SkinsRecords.Clear();
            SkinIdPath.Clear();
            PathSpriteId.Clear();
            PathStaticSpriteId.Clear();

            // evil...
            AddSpriteInfo("death_particle", GFX.Game, "death_particle");
            AddSpriteInfo("dreamblock_particles", GFX.Game, "objects/dreamblock/particles");
            AddSpriteInfo("feather_particles", GFX.Game, "particles/feather");

            AddSpriteInfo("Mountain_marker", MTN.Mountain, "marker/runBackpack", false);
            AddSpriteInfo("Mountain_marker", MTN.Mountain, "marker/runNoBackpack", false);
            AddSpriteInfo("Mountain_marker", MTN.Mountain, "marker/Fall", false);

            AddSpriteInfo("Interact_icons", GFX.Gui, "hover/idle");
            AddSpriteInfo("Interact_icons", GFX.Gui, "hover/highlight");
        }
        public override void DoRecord(string skinId, string directory, string cipher) {
            if (string.IsNullOrEmpty(skinId))
                return;
            directory = directory + "/";

            foreach (var kvp in PathSpriteId) {
                if (kvp.Value.Item1.HasAtlasSubtextures(directory + kvp.Value.Item2)) {

                    string spriteId = kvp.Value.Item3;
                    if (!SkinsRecords.ContainsKey(spriteId))
                        SkinsRecords.Add(spriteId, new());
                    if (cipher != playercipher && !SkinsRecords[spriteId].Contains(skinId + cipher))
                        SkinsRecords[spriteId].Add(skinId + cipher);

                    SkinIdPath[spriteId + skinId + cipher] = directory;
                }
            }
            foreach (var kvp in PathStaticSpriteId) {
                if (kvp.Value.Item1.Has(directory + kvp.Value.Item2)) {

                    string spriteId = kvp.Value.Item3;
                    if (!SkinsRecords.ContainsKey(spriteId))
                        SkinsRecords.Add(spriteId, new());
                    if (cipher != playercipher && !SkinsRecords[spriteId].Contains(skinId + cipher))
                        SkinsRecords[spriteId].Add(skinId + cipher);

                    SkinIdPath[spriteId + skinId + cipher] = directory;
                }
            }
        }
        public override void DoCombine(string skinId, string directory, string cipher) {}
        #endregion

        #region Method
        public override string GetDefaultSkin(string SpriteID, string cipher) {
            if (!SkinsRecords.TryGetValue(SpriteID, out var value))
                return null;
            string SkinID = null;
            string playerSkinName = GetPlayerSkinName(Player_Skinid_verify);

            if (playerSkinName != null && SkinIdPath.ContainsKey(SpriteID + playerSkinName + playercipher)) {
                SkinID = playerSkinName + playercipher;
                if (smh_Settings.PlayerSkinGreatestPriority)
                    return SkinID;
            }
            if (cipher == LockedToPlayer)
                return SkinID;
            foreach (SkinModHelperConfig config in GetEnabledGeneralSkins())
                if (value.Contains(config.SkinName))
                    SkinID = config.SkinName;
            return SkinID;
        }
        public string GetSkinWithPath(Atlas atlas, string orig_path, bool numberSet = false) {
            if (atlas == null || !Active)
                return orig_path;

            if ((numberSet ? PathSpriteId : PathStaticSpriteId).TryGetValue(atlas.RelativeDataPath + orig_path, out var tuple)) {

                string skinId = GetCurrentSkin(tuple.Item3);
                if (SkinIdPath.TryGetValue(skinId, out string path2))
                    if (numberSet ? atlas.HasAtlasSubtextures(path2 + orig_path) : atlas.Has(path2 + orig_path))
                        return path2 + orig_path;
            }
            return orig_path;
        }
        #endregion
    }
    #endregion
}