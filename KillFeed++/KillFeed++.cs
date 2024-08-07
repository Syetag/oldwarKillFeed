using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KillFeedPlusPlus
{
    public class KillFeedPlusPlus : RocketPlugin<Config>
    {
        public static KillFeedPlusPlus Instance;

        private static readonly Dictionary<CSteamID, PlayerKillData> playersKillData = new Dictionary<CSteamID, PlayerKillData>();

        private class PlayerKillData
        {
            public List<(string message, short key, DateTime startTime)> KillFeedData { get; } = new List<(string, short, DateTime)>();
            public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
        }

        protected override void Load()
        {
            Instance = this;
            UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;

            Rocket.Core.Logging.Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} loaded! Created by iche");
        }

        protected override void Unload()
        {
            UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;

            foreach (var playerData in playersKillData.Values)
            {
                foreach (var (message, key, startTime) in playerData.KillFeedData)
                {
                    foreach (var client in Provider.clients)
                    {
                        var player = UnturnedPlayer.FromSteamPlayer(client);
                        if (player == null)
                        {
                            continue;
                        }

                        HideTextElement(player, key, playerData.KillFeedData.IndexOf((message, key, startTime)));
                    }
                }

                playerData.CancellationTokenSource.Cancel();
            }

            playersKillData.Clear();

            Instance = null;
            Rocket.Core.Logging.Logger.Log($"{Name} {Assembly.GetName().Version.ToString(3)} unloaded! Created by iche");
        }

        private static PlayerKillData GetData(UnturnedPlayer player)
        {
            if (player == null)
            {
                return null;
            }

            try
            {
                if (!playersKillData.TryGetValue(player.CSteamID, out var data))
                {
                    data = new PlayerKillData();
                    playersKillData[player.CSteamID] = data;
                }

                return data;
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "KillFeedPlusPlus.GetData");
                return null;
            }
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            try
            {
                if (playersKillData.ContainsKey(player.CSteamID))
                {
                    playersKillData[player.CSteamID].CancellationTokenSource.Cancel();
                    playersKillData.Remove(player.CSteamID);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, $"KillFeedPlusPlus.OnPlayerDisconnected");
            }
        }

        public override TranslationList DefaultTranslations => new TranslationList()
        {
            { "UnknownPlayer", "Unknown" },
            { "UnknownMurderer", "Unknown" },
            { "UnknownWeapon", "Unknown" },
            { "DeathCause_Bleeding", "bled out!" },
            { "DeathCause_Bones", "broke their bones!" },
            { "DeathCause_Freezing", "froze to death!" },
            { "DeathCause_Burning", "burned to death!" },
            { "DeathCause_Food", "starved to death!" },
            { "DeathCause_Water", "died of thirst!" },
            { "DeathCause_Gun_1", "killed" },
            { "DeathCause_Gun_2", "with" },
            { "DeathCause_Melee_1", "killed" },
            { "DeathCause_Melee_2", "with" },
            { "DeathCause_Zombie", "was killed by a zombie!" },
            { "DeathCause_Animal", "was killed by an animal!" },
            { "DeathCause_Suicide", "commited suicide!" },
            { "DeathCause_Infection", "died from infection!" },
            { "DeathCause_Punch", "punched" },
            { "DeathCause_Breath", "suffocated!" },
            { "DeathCause_Vehicle", "was hit by a vehicle!" },
            { "DeathCause_Roadkill", "died in a car accident!" },
            { "DeathCause_Grenade", "blew themselves up with a grenade!" },
            { "DeathCause_Shred", "was shredded!" },
            { "DeathCause_Landmine", "stepped on a landmine!" },
            { "DeathCause_Arena_1", "defeated" },
            { "DeathCause_Arena_2", "in the arena!" },
            { "DeathCause_Missile", "was hit by a missile!" },
            { "DeathCause_Charge", "was electrocuted!" },
            { "DeathCause_Splash", "died from an explosion!" },
            { "DeathCause_Sentry", "was killed by a sentry!" },
            { "DeathCause_Acid", "dissolved in acid!" },
            { "DeathCause_Boulder", "was crushed!" },
            { "DeathCause_Burner", "burned alive!" },
            { "DeathCause_Spit", "was poisoned!" },
            { "DeathCause_Default", "died!" }
        };

        private void OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                string playerName = "";
                string murdererName = "";
                string weaponName = Translate("UnknownWeapon");
                string weaponColor = "#FFFFFF";
                string deathColor = "#FFFFFF";

                if (player != null)
                {
                    playerName = player == null || player.CharacterName == null ? $"<color=#{Configuration.Instance.PlayerNameColor}>[{Translate("UnknownPlayer")}]</color>" : $"<color=#{Configuration.Instance.PlayerNameColor}>[{player.CharacterName}]</color>";
                }

                if (cause == EDeathCause.GUN || cause == EDeathCause.MELEE || cause == EDeathCause.PUNCH)
                {
                    var murdererPlayer = UnturnedPlayer.FromCSteamID(murderer);
                    murdererName = murdererPlayer == null || murdererPlayer.CharacterName == null ? $"<color=#{Configuration.Instance.MurdererNameColor}>[{Translate("UnknownMurderer")}]</color>" : $"<color=#{Configuration.Instance.MurdererNameColor}>[{murdererPlayer.CharacterName}]</color>";

                    if (murdererPlayer != null)
                    {
                        if (murdererPlayer.Player.equipment.asset is ItemGunAsset gunAsset)
                        {
                            weaponName = gunAsset.itemName;
                            weaponColor = Palette.hex(ItemTool.getRarityColorUI(gunAsset.rarity));
                        }
                        else if (murdererPlayer.Player.equipment.asset is ItemMeleeAsset meleeAsset)
                        {
                            weaponName = meleeAsset.itemName;
                            weaponColor = Palette.hex(ItemTool.getRarityColorUI(meleeAsset.rarity));
                        }
                    }
                }

                string killMessage = GenerateKillMessage(playerName, murdererName, deathColor, cause, weaponName, weaponColor);

                foreach (var client in Provider.clients)
                {
                    var targetPlayer = UnturnedPlayer.FromSteamPlayer(client);
                    SendKillMessage(targetPlayer, killMessage);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, $"KillFeedPlusPlus.OnPlayerDeath");
            }
        }

        private void SendKillMessage(UnturnedPlayer player, string killMessage)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                var data = GetData(player);
                if (data == null)
                {
                    return;
                }

                short uniqueEffectKey = GenerateUniqueEffectKey();
                int currentIndex = data.KillFeedData.Count;

                if (currentIndex >= 5)
                {
                    (string message, short key, DateTime startTime) = data.KillFeedData[0];
                    HideTextElement(player, key, 0);
                    data.KillFeedData.RemoveAt(0);
                    currentIndex--;

                    for (int i = 0; i < currentIndex; ++i)
                    {
                        data.KillFeedData[i] = (data.KillFeedData[i].message, data.KillFeedData[i].key, data.KillFeedData[i].startTime);
                        MoveTextElementUp(player, data.KillFeedData[i].key, i + 1, i, data.KillFeedData[i].startTime);
                    }
                }

                data.KillFeedData.Add((killMessage, uniqueEffectKey, DateTime.Now));
                CreateUI(player, uniqueEffectKey, currentIndex, killMessage);

                _ = HideMessageAfterDelayAsync(player, uniqueEffectKey, currentIndex, 5f, data.CancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "KillFeedPlusPlus.SendKillMessage");
            }
        }

        private async Task HideMessageAfterDelayAsync(UnturnedPlayer player, short key, int index, float delay, CancellationToken cancellationToken)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

                if (player == null)
                {
                    return;
                }

                var killData = GetData(player);
                if (killData == null)
                {
                    return;
                }

                HideTextElement(player, key, index);

                int targetIndex = killData.KillFeedData.FindIndex(data => data.key == key);
                if (targetIndex >= 0 && targetIndex < killData.KillFeedData.Count)
                {
                    killData.KillFeedData.RemoveAt(targetIndex);

                    for (int i = targetIndex; i < killData.KillFeedData.Count; ++i)
                    {
                        (string message, short key1, DateTime startTime) = killData.KillFeedData[i];
                        killData.KillFeedData[i] = (message, key1, startTime);
                        MoveTextElementUp(player, key1, i + 1, i, startTime);
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "KillFeedPlusPlus.HideMessageAfterDelayAsync");
            }
        }

        private string GenerateKillMessage(string playerName, string murdererName, string deathColor, EDeathCause cause, string weaponName, string weaponColor)
        {
            try
            {
                switch (cause)
                {
                    case EDeathCause.BLEEDING:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Bleeding")}</color></b>";
                    case EDeathCause.BONES:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Bones")}</color></b>";
                    case EDeathCause.FREEZING:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Freezing")}</color></b>";
                    case EDeathCause.BURNING:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Burning")}</color></b>";
                    case EDeathCause.FOOD:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Food")}</color></b>";
                    case EDeathCause.WATER:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Water")}</color></b>";
                    case EDeathCause.GUN:
                        return $"<b>{murdererName} <color={deathColor}>{Translate("DeathCause_Gun_1")}</color> {playerName} <color=#FFFFFF>{Translate("DeathCause_Gun_2")}</color> <color={weaponColor}>\"{weaponName}\"!</color></b>";
                    case EDeathCause.MELEE:
                        return $"<b>{murdererName} <color={deathColor}>{Translate("DeathCause_Melee_1")}</color> {playerName} <color=#FFFFFF>{Translate("DeathCause_Melee_2")}</color> <color={weaponColor}>\"{weaponName}\"!</color></b>";
                    case EDeathCause.ZOMBIE:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Zombie")}</color></b>";
                    case EDeathCause.ANIMAL:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Animal")}</color></b>";
                    case EDeathCause.SUICIDE:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Suicide")}</color></b>";
                    case EDeathCause.INFECTION:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Infection")}</color></b>";
                    case EDeathCause.PUNCH:
                        return $"<b>{murdererName} <color={deathColor}>{Translate("DeathCause_Punch")}</color> {playerName}!</b>";
                    case EDeathCause.BREATH:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Breath")}</color></b>";
                    case EDeathCause.VEHICLE:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Vehicle")}</color></b>";
                    case EDeathCause.ROADKILL:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Roadkill")}</color></b>";
                    case EDeathCause.GRENADE:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Grenade")}</color></b>";
                    case EDeathCause.SHRED:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Shred")}</color></b>";
                    case EDeathCause.LANDMINE:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Landmine")}</color></b>";
                    case EDeathCause.ARENA:
                        return $"<b>{murdererName} <color={deathColor}>{Translate("DeathCause_Arena_1")}</color> {playerName} <color={deathColor}>{Translate("DeathCause_Arena_2")}</color></b>";
                    case EDeathCause.MISSILE:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Missile")}</color></b>";
                    case EDeathCause.CHARGE:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Charge")}</color></b>";
                    case EDeathCause.SPLASH:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Splash")}</color></b>";
                    case EDeathCause.SENTRY:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Sentry")}</color></b>";
                    case EDeathCause.ACID:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Acid")}</color></b>";
                    case EDeathCause.BOULDER:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Boulder")}</color></b>";
                    case EDeathCause.BURNER:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Burner")}</color></b>";
                    case EDeathCause.SPIT:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Spit")}</color></b>";
                    default:
                        return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Default")}</color></b>";
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "KillFeedPlusPlus.GenerateKillMessage");
                return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Default")}</color></b>";
            }
        }

        private void MoveTextElementUp(UnturnedPlayer player, short key, int currentIndex, int targetIndex, DateTime startTime)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                var data = GetData(player);
                if (data == null)
                {
                    return;
                }

                EffectManager.sendUIEffectVisibility(key, player.Player.channel.owner.transportConnection, true, $"Text{currentIndex}", false);

                string message = data.KillFeedData[targetIndex].message;
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true, $"Text{targetIndex}", message);
                EffectManager.sendUIEffectVisibility(key, player.Player.channel.owner.transportConnection, true, $"Text{targetIndex}", true);

                data.KillFeedData[targetIndex] = (data.KillFeedData[targetIndex].message, data.KillFeedData[targetIndex].key, data.KillFeedData[targetIndex].startTime);
                float delay = 5f - (float)(DateTime.Now - startTime).TotalSeconds;

                if (delay < 0f)
                {
                    delay = 0f;
                }

                _ = HideMessageAfterDelayAsync(player, key, currentIndex, delay, data.CancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "KillFeedPlusPlus.MoveTextElementUp");
            }
        }

        public void CreateUI(UnturnedPlayer player, short key, int index, string message)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                EffectManager.sendUIEffect(Configuration.Instance.EffectID, key, player.Player.channel.owner.transportConnection, true);
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true, $"Text{index}", message);
                EffectManager.sendUIEffectVisibility(key, player.Player.channel.owner.transportConnection, true, $"Text{index}", true);

                for (int i = 0; i < 5; ++i)
                {
                    if (i != index)
                    {
                        EffectManager.sendUIEffectVisibility(key, player.Player.channel.owner.transportConnection, true, $"Text{i}", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "KillFeedPlusPlus.CreateUI");
            }
        }

        private void HideTextElement(UnturnedPlayer player, short key, int index)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                EffectManager.sendUIEffectVisibility(key, player.Player.channel.owner.transportConnection, true, $"Text{index}", false);
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "KillFeedPlusPlus.HideTextElement");
            }
        }

        public short GenerateUniqueEffectKey()
        {
            try
            {
                HashSet<short> usedKeys = new HashSet<short>();

                while (true)
                {
                    short key = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue);

                    if (usedKeys.Add(key) && (key < Configuration.Instance.MinEffectKey || key > Configuration.Instance.MaxEffectKey))
                    {
                        return key;
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "KillFeedPlusPlus.GenerateUniqueEffectKey.");
                return -1;
            }
        }
    }
}