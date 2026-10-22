using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MultiplayerTemplate.Core.Completion;
using MultiplayerTemplate.Progression;

namespace SkillModify
{
    /// <summary>
    /// Changes the value of the last level of two skills: carrying capacity ("Strong Arms", game: 20) and bricks per
    /// magnet pull ("Brick Magnet", game: 10). Every level overrides the stat with a higher priority than the one
    /// before, so the new value applies from the last level on; the game rebuilds bought levels from these definitions
    /// when a world loads, so it also applies to worlds that already have the level. The game does not notice the
    /// change itself, so while a value differs from the game's, every world opened is marked as practice (no Steam
    /// leaderboard score), as the game does for its own development grants.
    /// </summary>
    [BepInPlugin(Guid, "SkillModify", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "michel.brickbybrick.skillmodify";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<int> CarryCapacity;
        internal static ConfigEntry<int> MagnetAmount;

        private void Awake()
        {
            Log = Logger;
            CarryCapacity = Config.Bind("Last level", "CarryCapacity", 50,
                new ConfigDescription("Carrying capacity at the last \"Strong Arms\" level (game: 20).",
                    new AcceptableValueRange<int>(1, 500)));
            MagnetAmount = Config.Bind("Last level", "MagnetAmount", 50,
                new ConfigDescription("Bricks per pull at the last \"Brick Magnet\" level (game: 10). The magnet pulls at " +
                    "most as many bricks as there is free carrying capacity.", new AcceptableValueRange<int>(1, 500)));
            new Harmony(Guid).PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo($"SkillModify: last level carry capacity {CarryCapacity.Value}, magnet {MagnetAmount.Value}.");
        }
    }

    /// <summary>Sets the last level's values in the skill definitions before the game indexes them.</summary>
    [HarmonyPatch(typeof(ProgressionService), MethodType.Constructor, typeof(ProgressionDefinitionCatalog), typeof(IProgressionAuthority))]
    internal static class ProgressionServicePatch
    {
        private static readonly FieldInfo ModifierValue = AccessTools.Field(typeof(ProgressionRewardDefinition), "modifierValue");

        /// <summary>The game's values, read before the first change (the definitions stay changed for the session).</summary>
        private static readonly Dictionary<string, float> GameValues = new Dictionary<string, float>();

        /// <summary>True once a value differs from the game's.</summary>
        internal static bool Changed;

        private static void Prefix(ProgressionDefinitionCatalog __0)
        {
            if (__0 == null)
            {
                return;
            }
            foreach (ProgressionSkillDefinition skill in __0.Skills)
            {
                if (skill.Id == "skill.carry-capacity")
                {
                    SetLastLevel(skill, "stat.carry-capacity", Plugin.CarryCapacity.Value);
                }
                else if (skill.Id == "skill.brick-magnet")
                {
                    SetLastLevel(skill, "stat.brick-magnet-amount", Plugin.MagnetAmount.Value);
                }
            }
        }

        private static void SetLastLevel(ProgressionSkillDefinition skill, string statId, int value)
        {
            if (skill.Levels.Count == 0)
            {
                return;
            }
            foreach (ProgressionRewardDefinition reward in skill.Levels[skill.Levels.Count - 1].Rewards)
            {
                if (reward.Kind != ProgressionRewardKind.StatModifier || reward.TargetId != statId)
                {
                    continue;
                }
                if (!GameValues.TryGetValue(statId, out float game))
                {
                    game = reward.ModifierValue;
                    GameValues[statId] = game;
                }
                if (reward.ModifierValue != value)
                {
                    ModifierValue.SetValue(reward, (float)value);
                    Plugin.Log.LogInfo($"{skill.Id} level {skill.Levels.Count}: {statId} {game} -> {value}.");
                }
                Changed |= value != game;
            }
        }
    }

    /// <summary>Marks the run as practice while a skill value differs from the game's.</summary>
    [HarmonyPatch(typeof(CompletionRun), nameof(CompletionRun.Open))]
    internal static class CompletionRunPatch
    {
        private static void Postfix(CompletionRun __instance)
        {
            if (ProgressionServicePatch.Changed && __instance.IsEligible)
            {
                __instance.Invalidate("SkillModify mod: changed skill values.");
                Plugin.Log.LogInfo("World marked as practice (no leaderboard score): skill values changed.");
            }
        }
    }
}
