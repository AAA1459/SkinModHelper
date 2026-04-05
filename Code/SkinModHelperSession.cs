﻿using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

using static Celeste.Mod.SkinModHelper.SkinsSystem;
using static Celeste.Mod.SkinModHelper.SkinModHelperModule;

namespace Celeste.Mod.SkinModHelper {

    public class SkinModHelperSession : EverestModuleSession {
        public string SelectedPlayerSkin { get; set; }

        public string SelectedSilhouetteSkin { get; set; }

        public string SelectedOtherSelfSkin { get; set; }

        public Dictionary<string, bool> ExtraXmlList {
            get => _ExtraXmlList;
            // When reading the save it loses the comparator... so create the new with the comparator.
            set => _ExtraXmlList = new(value, StringComparer.OrdinalIgnoreCase);
        }
        [YamlIgnore]
        private Dictionary<string, bool> _ExtraXmlList = new(StringComparer.OrdinalIgnoreCase);

        public string Player_animPrefixAddOn { get; set; }



        [YamlIgnore]
        public Dictionary<string, string> SpriteSkin_record = new(StringComparer.OrdinalIgnoreCase);
        [YamlIgnore]
        public Dictionary<string, string> PortraitsSkin_record = new(StringComparer.OrdinalIgnoreCase);
        [YamlIgnore]
        public Dictionary<string, string> OtherSkin_record = new(StringComparer.OrdinalIgnoreCase);


        public void SetPlayerSkin(string newSkinId) {
            if (smh_Session == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"Cannot set session because it is null");
                return;
            }
            if (newSkinId != null && GetPlayerSkin(null, newSkinId) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"PlayerSkin '{newSkinId}' does not exist!");
            }
            SelectedPlayerSkin = newSkinId;
            RefreshSkins(false);
        }
        public void SetSilhouetteSkin(string newSkinId) {
            if (newSkinId != null && GetPlayerSkin(null, newSkinId) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"PlayerSkin '{newSkinId}' does not exist!");
            }
            SelectedSilhouetteSkin = newSkinId;
            RefreshSkins(false);
        }
        public void SetOtherSelfSkin(string newSkinId) {
            if (newSkinId != null && GetPlayerSkin(null, newSkinId) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"PlayerSkin '{newSkinId}' does not exist!");
            }
            SelectedOtherSelfSkin = newSkinId;
            RefreshSkins(false);
        }

        public void SetGeneralSkin(string newSkin, bool? OnOff) {
            if (GetGeneralSkin(newSkin) == null) {
                Logger.Log(LogLevel.Warn, "SkinModHelper", $"GeneralSkin '{newSkin}' does not exist!");
            }

            if (OnOff == null) {
                ExtraXmlList.Remove(newSkin);
            } else if (OnOff != null) {
                ExtraXmlList[newSkin] = OnOff.Value;
            }
            RefreshSkins(false);
        }



        public int? Last_Player_Skinid_verify { get; set; }




    }
}