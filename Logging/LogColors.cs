using Assets.Scripts.Combat.Damage;

namespace Assets.Scripts.Logging
{
    /// <summary>
    /// Centralised colour palette for all log message formatting.
    /// All log managers reference this; only CombatFormatter may additionally
    /// use the damage-type helper.
    /// </summary>
    public static class LogColors
    {
        // ==================== COMBAT / ROLL COLOURS ====================
        public const string Success     = "#44FF44";
        public const string Failure     = "#FF4444";
        public const string Damage      = "#FFA500";
        public const string Energy      = "#88DDFF";
        public const string Health      = "#44FF44";
        public const string Skill       = "#AADDFF";
        public const string Number      = "#FFDD88";
        public const string DC          = "#FF8866";
        public const string Duration    = "#AAAAAA";

        // Bold name colours — bolding is part of the style contract for these three.
        // Always apply via the Format methods below, never write the tags manually.
        public const string VehicleName   = "#AAAAAA";
        public const string ComponentName = "#FFFFFF";
        public const string AbilityName   = "#DDAAFF";

        // ==================== FORMAT HELPERS ====================
        // Plain colour — no bold.
        private static string Color(string hex, string text)    => $"<color={hex}>{text}</color>";
        // Bold + colour — used for names.
        private static string Bold(string hex, string text)     => $"<b><color={hex}>{text}</color></b>";

        public static string Vehicle(string text)   => Bold(VehicleName,   text);
        public static string Component(string text) => Color(ComponentName, text);
        public static string Ability(string text)   => Color(AbilityName,   text);

        // ==================== IMPORTANCE COLOURS ====================
        public const string ImportanceCritical = "#FF4444";
        public const string ImportanceHigh     = "#FFAA44";
        public const string ImportanceMedium   = "#FFFFFF";
        public const string ImportanceLow      = "#AAAAAA";
        public const string ImportanceDefault  = "#888888";

        public static string FormatImportanceText(EventImportance importance, string text)
        {
            string color = importance switch
            {
                EventImportance.Critical => ImportanceCritical,
                EventImportance.High     => ImportanceHigh,
                EventImportance.Medium   => ImportanceMedium,
                EventImportance.Low      => ImportanceLow,
                _                        => ImportanceDefault
            };

            return $"<color={color}>{text}</color>";
        }

        // ==================== EVENT FEED UI COLOURS ====================
        public const string FeedLocation  = "#888888";
        public const string FeedTimestamp = "#666666";

        // ==================== EVENT TYPE ICON COLOURS ====================
        public const string IconCombat       = "#FF5555";
        public const string IconMovement     = "#55AAFF";
        public const string IconStageHazard  = "#FFAA00";
        public const string IconModifier     = "#AA88FF";
        public const string IconCondition    = "#FFAA00";
        public const string IconSkillUse     = "#55DDAA";
        public const string IconDestruction  = "#FF3333";
        public const string IconFinishLine   = "#FFD700";
        public const string IconRivalry      = "#FF55FF";
        public const string IconHeroicMoment = "#FFD700";
        public const string IconTragicMoment = "#AA4444";
        public const string IconSystem       = "#888888";
        public const string IconResource     = "#55FFAA";
        public const string IconEventCard    = "#AADDFF";
        public const string IconAI           = "#8888FF";
        public const string IconDefault      = "#666666";
        public const string IconUnknown      = "#6688FF";

        // ==================== INSPECTOR UI COLOURS ====================
        public const string Available          = "#66FF66";
        public const string Warning            = "#FF6666";
        public const string InspectorHeader    = "#5599FF";
        public const string InspectorSeparator = "#334466";
        public const string InspectorNoAction  = "#FF8888";

        // ==================== DAMAGE TYPE COLOURS ====================
        // Chosen to not clash with importance palette
        // (#FF4444 Critical, #FFAA44 High, #FFFFFF Medium, #AAAAAA Low)
        public static string GetDamageTypeColor(DamageType type)
        {
            return type switch
            {
                DamageType.Physical    => "#CCCCCC",
                DamageType.Bludgeoning => "#BBAA99",
                DamageType.Piercing    => "#AACCDD",
                DamageType.Slashing    => "#AAAACC",
                DamageType.Fire        => "#FF6622",
                DamageType.Cold        => "#88CCFF",
                DamageType.Lightning   => "#DDFF44",
                DamageType.Acid        => "#AADD44",
                DamageType.Force       => "#CC88FF",
                DamageType.Psychic     => "#FF88CC",
                DamageType.Necrotic    => "#AA55FF",
                DamageType.Radiant     => "#FFF5AA",
                _                      => "#FFA500",
            };
        }
    }
}
