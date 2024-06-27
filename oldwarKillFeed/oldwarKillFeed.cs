using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace oldwar
{
    public class oldwarKillFeed : RocketPlugin<Config>
    {
        public static oldwarKillFeed Instance;

        private static readonly ConcurrentDictionary<CSteamID, PlayerKillData> playersKillData = new ConcurrentDictionary<CSteamID, PlayerKillData>();

        private class PlayerKillData
        {
            public List<(string message, short key, float startTime)> KillFeedData { get; } = new List<(string, short, float)>();
            public SemaphoreSlim KillFeedSemaphore { get; } = new SemaphoreSlim(1, 1);
        }

        protected override void Load()
        {
            Instance = this;
            UnturnedPlayerEvents.OnPlayerDeath += OnPlayerDeath;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;

            Rocket.Core.Logging.Logger.Log("oldwarKillFeed loaded! Created by SyetaG");
        }

        protected override void Unload()
        {
            UnturnedPlayerEvents.OnPlayerDeath -= OnPlayerDeath;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            Instance = null;
        }

        private static PlayerKillData GetData(UnturnedPlayer player)
        {
            if (player == null)
            {
                return null;
            }

            try
            {
                return playersKillData.GetOrAdd(player.CSteamID, _ => new PlayerKillData());
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarKillFeed] oldwarKillFeed.GetData");
                return null;
            }
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            try
            {
                if (playersKillData.TryRemove(player.CSteamID, out var killData))
                {
                    killData.KillFeedSemaphore.Dispose();
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, $"[oldwarKillFeed] oldwarKillFeed.OnPlayerDisconnected");
            }
        }

        public override TranslationList DefaultTranslations => new TranslationList()
        {
            { "UnknownPlayer", "Unknown Player" },
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
                string playerName = $"<color=#{Configuration.Instance.PlayerNameColor}>[{player.DisplayName}]</color>";
                string murdererName = "";
                string weaponName = "";
                string weaponColor = "#FFFFFF";

                if (cause == EDeathCause.GUN || cause == EDeathCause.MELEE || cause == EDeathCause.PUNCH)
                {
                    UnturnedPlayer murdererPlayer = UnturnedPlayer.FromCSteamID(murderer);
                    murdererName = murdererPlayer == null || murdererPlayer.DisplayName == null
                        ? $"<color=#{Configuration.Instance.PlayerNameColor}>[{Translate("UnknownPlayer", player.CSteamID)}]</color>"
                        : $"<color=#{Configuration.Instance.PlayerNameColor}>[{murdererPlayer.DisplayName}]</color>";

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

                string deathColor = "#FFFFFF";
                string killMessage = GenerateKillMessage(playerName, murdererName, deathColor, cause, weaponName, weaponColor, player);

                foreach (var client in Provider.clients)
                {
                    var targetPlayer = UnturnedPlayer.FromSteamPlayer(client);
                    SendKillMessage(targetPlayer, killMessage);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, $"[oldwarKillFeed] oldwarKillFeed.OnPlayerDeath");
            }
        }

        private void SendKillMessage(UnturnedPlayer player, string killMessage)
        {
            if (player == null)
            {
                return;
            }

            var killData = GetData(player);
            if (killData == null)
            {
                return;
            }

            try
            {
                if (!killData.KillFeedSemaphore.Wait(8000))
                {
                    Rocket.Core.Logging.Logger.LogError($"[oldwarKillFeed] Timeout while waiting for semaphore for player {player.DisplayName} ({player.CSteamID}). Possible deadlock in oldwarKillFeed.SendKillMessage.");
                    return;
                }

                short uniqueEffectKey = GenerateUniqueEffectKey();
                int currentIndex = killData.KillFeedData.Count;

                if (currentIndex >= 5)
                {
                    (string message, short key, float startTime) = killData.KillFeedData[0];
                    HideTextElement(player, key, 0);
                    killData.KillFeedData.RemoveAt(0);
                    currentIndex--;

                    for (int i = 0; i < currentIndex; ++i)
                    {
                        killData.KillFeedData[i] = (killData.KillFeedData[i].message, killData.KillFeedData[i].key, killData.KillFeedData[i].startTime);
                        MoveTextElementUp(player, killData.KillFeedData[i].key, i + 1, i, killData.KillFeedData[i].startTime);
                    }
                }

                killData.KillFeedData.Add((killMessage, uniqueEffectKey, Time.time));
                CreateUI(player, uniqueEffectKey, currentIndex, killMessage);
                StartCoroutine(HideMessageAfterDelay(player, uniqueEffectKey, currentIndex, 5f));
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarKillFeed] oldwarKillFeed.SendKillMessage");
            }
            finally
            {
                killData = GetData(player);
                if (killData != null)
                {
                    killData.KillFeedSemaphore.Release();
                }
            }
        }

        private string GenerateKillMessage(string playerName, string murdererName, string deathColor, EDeathCause cause, string weaponName, string weaponColor, UnturnedPlayer player)
        {
            switch (cause)
            {
                case EDeathCause.BLEEDING:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Bleeding", player.CSteamID)}</color></b>";
                case EDeathCause.BONES:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Bones", player.CSteamID)}</color></b>";
                case EDeathCause.FREEZING:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Freezing", player.CSteamID)}</color></b>";
                case EDeathCause.BURNING:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Burning", player.CSteamID)}</color></b>";
                case EDeathCause.FOOD:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Food", player.CSteamID)}</color></b>";
                case EDeathCause.WATER:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Water", player.CSteamID)}</color></b>";
                case EDeathCause.GUN:
                    return $"<b>{murdererName} <color={deathColor}>{Translate("DeathCause_Gun_1", player.CSteamID)}</color> {playerName} <color=#FFFFFF>{Translate("DeathCause_Gun_2", player.CSteamID)}</color> <color={weaponColor}>\"{weaponName}\"!</color></b>";
                case EDeathCause.MELEE:
                    return $"<b>{murdererName} <color={deathColor}>{Translate("DeathCause_Melee_1", player.CSteamID)}</color> {playerName} <color=#FFFFFF>{Translate("DeathCause_Melee_2", player.CSteamID)}</color> <color={weaponColor}>\"{weaponName}\"!</color></b>";
                case EDeathCause.ZOMBIE:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Zombie", player.CSteamID)}</color></b>";
                case EDeathCause.ANIMAL:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Animal", player.CSteamID)}</color></b>";
                case EDeathCause.SUICIDE:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Suicide", player.CSteamID)}</color></b>";
                case EDeathCause.INFECTION:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Infection", player.CSteamID)}</color></b>";
                case EDeathCause.PUNCH:
                    return $"<b>{murdererName} <color={deathColor}>{Translate("DeathCause_Punch", player.CSteamID)}</color> {playerName}!</b>";
                case EDeathCause.BREATH:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Breath", player.CSteamID)}</color></b>";
                case EDeathCause.VEHICLE:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Vehicle", player.CSteamID)}</color></b>";
                case EDeathCause.ROADKILL:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Roadkill", player.CSteamID)}</color></b>";
                case EDeathCause.GRENADE:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Grenade", player.CSteamID)}</color></b>";
                case EDeathCause.SHRED:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Shred", player.CSteamID)}</color></b>";
                case EDeathCause.LANDMINE:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Landmine", player.CSteamID)}</color></b>";
                case EDeathCause.ARENA:
                    return $"<b>{murdererName} <color={deathColor}>{Translate("DeathCause_Arena_1", player.CSteamID)}</color> {playerName} <color={deathColor}>{Translate("DeathCause_Arena_2", player.CSteamID)}</color></b>";
                case EDeathCause.MISSILE:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Missile", player.CSteamID)}</color></b>";
                case EDeathCause.CHARGE:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Charge", player.CSteamID)}</color></b>";
                case EDeathCause.SPLASH:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Splash", player.CSteamID)}</color></b>";
                case EDeathCause.SENTRY:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Sentry", player.CSteamID)}</color></b>";
                case EDeathCause.ACID:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Acid", player.CSteamID)}</color></b>";
                case EDeathCause.BOULDER:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Boulder", player.CSteamID)}</color></b>";
                case EDeathCause.BURNER:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Burner", player.CSteamID)}</color></b>";
                case EDeathCause.SPIT:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Spit", player.CSteamID)}</color></b>";
                default:
                    return $"<b>{playerName} <color={deathColor}>{Translate("DeathCause_Default", player.CSteamID)}</color></b>";
            }
        }

        private IEnumerator HideMessageAfterDelay(UnturnedPlayer player, short key, int index, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (player == null)
            {
                yield break;
            }

            var killData = GetData(player);
            if (killData == null)
            {
                yield break;
            }

            try
            {
                HideTextElement(player, key, index);

                int targetIndex = killData.KillFeedData.FindIndex(data => data.key == key);
                if (targetIndex >= 0 && targetIndex < killData.KillFeedData.Count)
                {
                    killData.KillFeedData.RemoveAt(targetIndex);

                    for (int i = targetIndex; i < killData.KillFeedData.Count; ++i)
                    {
                        (string message, short key1, float startTime) = killData.KillFeedData[i];
                        killData.KillFeedData[i] = (message, key1, startTime);
                        MoveTextElementUp(player, key1, i + 1, i, startTime);
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarKillFeed] oldwarKillFeed.HideMessageAfterDelay");
            }
        }

        private void MoveTextElementUp(UnturnedPlayer player, short key, int currentIndex, int targetIndex, float startTime)
        {
            if (player == null)
            {
                return;
            }

            var killData = GetData(player);
            if (killData == null)
            {
                return;
            }

            try
            {

                EffectManager.sendUIEffectVisibility(key, player.Player.channel.owner.transportConnection, true, $"Text{currentIndex}", false);

                string message = killData.KillFeedData[targetIndex].message;
                EffectManager.sendUIEffectText(key, player.Player.channel.owner.transportConnection, true, $"Text{targetIndex}", message);
                EffectManager.sendUIEffectVisibility(key, player.Player.channel.owner.transportConnection, true, $"Text{targetIndex}", true);

                killData.KillFeedData[targetIndex] = (killData.KillFeedData[targetIndex].message, killData.KillFeedData[targetIndex].key, killData.KillFeedData[targetIndex].startTime);
                float delay = 5f - (Time.time - startTime);
                if (delay < 0f)
                    delay = 0f;

                StopCoroutine(HideMessageAfterDelay(player, key, currentIndex, 5f));
                StartCoroutine(HideMessageAfterDelay(player, key, targetIndex, delay));
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarKillFeed] oldwarKillFeed.MoveTextElementUp");
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
                        EffectManager.sendUIEffectVisibility(key, player.Player.channel.owner.transportConnection, true, $"Text{i}", false);
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarKillFeed] oldwarKillFeed.CreateUI");
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
                Rocket.Core.Logging.Logger.LogException(ex, "[oldwarKillFeed] oldwarKillFeed.HideTextElement");
            }
        }

        public short GenerateUniqueEffectKey()
        {
            HashSet<short> usedKeys = new HashSet<short>();

            while (true)
            {
                try
                {
                    short key = (short)UnityEngine.Random.Range(short.MinValue, short.MaxValue);

                    if (usedKeys.Add(key) && (key < Configuration.Instance.MinEffectKey || key > Configuration.Instance.MaxEffectKey))
                    {
                        return key;
                    }
                }
                catch (Exception ex)
                {
                    Rocket.Core.Logging.Logger.LogException(ex, "[oldwarKillFeed] oldwarKillFeed.GenerateUniqueEffectKey.");
                    return -1;
                }
            }
        }
    }
}