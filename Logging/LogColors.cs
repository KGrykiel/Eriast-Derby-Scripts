namespace Assets.Scripts.Logging
{
    /// <summary>
    /// Centralised colour palette for all log message formatting.
    /// All log managers reference this; only CombatFormatter may additionally
    /// use the damage-type helper.
    ///
    /// </summary>
    public static class LogColors
    {
        // =====================================================================
        // COLOUR CONSTANTS
        // Raw hex strings. Set any of these to null at runtime to opt a slot out
        // of custom colouring; the corresponding format method will then return
        // plain text, inheriting whatever colour wraps it in the feed.
        // =====================================================================

        // ==================== COMBAT / ROLL ====================
        public const string SuccessColor  = "#44FF44";
        public const string FailureColor  = "#FF4444";
        public const string DamageColor   = "#FFA500";
        public const string EnergyColor   = "#88DDFF";
        public const string HealthColor   = "#44FF44";
        public const string SkillColor    = "#AADDFF";
        public const string NumberColor   = "#FFDD88";
        public const string DCColor       = "#FF8866";
        public const string DurationColor = "#AAAAAA";

        // ==================== ENTITY / ABILITY NAMES ====================
        // Bolding is part of the contract for VehicleName — always apply via
        // Vehicle(), Component(), Ability() rather than writing tags manually.
        public const string VehicleName   = "#AAAAAA";
        public const string ComponentName = "#FFFFFF";
        public const string AbilityName   = "#DDAAFF";

        // ==================== IMPORTANCE ====================
        public const string ImportanceCritical = "#FF4444";
        public const string ImportanceHigh     = "#FFAA44";
        public const string ImportanceMedium   = "#FFFFFF";
        public const string ImportanceLow      = "#AAAAAA";
        public const string ImportanceDefault  = "#888888";

        // ==================== EVENT FEED UI ====================
        public const string FeedLocation  = "#888888";
        public const string FeedTimestamp = "#666666";

        // ==================== EVENT TYPE ICONS ====================
        public const string IconCombat      = "#FF5555";
        public const string IconMovement    = "#55AAFF";
        public const string IconStageHazard = "#FFAA00";
        public const string IconCondition   = "#FFAA00";
        public const string IconDestruction = "#FF3333";
        public const string IconFinishLine  = "#FFD700";
        public const string IconSystem      = "#888888";
        public const string IconResource    = "#55FFAA";
        public const string IconEventCard   = "#AADDFF";
        public const string IconAI          = "#8888FF";
        public const string IconDefault     = "#666666";
        public const string IconUnknown     = "#6688FF";

        // ==================== INSPECTOR UI ====================
        public const string Available          = "#66FF66";
        public const string Warning            = "#FF6666";
        public const string InspectorHeader    = "#5599FF";
        public const string InspectorSeparator = "#334466";
        public const string InspectorNoAction  = "#FF8888";

        // ==================== DAMAGE TYPES ====================
        // Chosen to not clash with the importance palette
        // (#FF4444 Critical, #FFAA44 High, #FFFFFF Medium, #AAAAAA Low).
        public static string GetDamageTypeColor(Combat.Damage.DamageType damageType)
        {
            return damageType switch
            {
                Combat.Damage.DamageType.Physical    => "#CCCCCC",
                Combat.Damage.DamageType.Bludgeoning => "#BBAA99",
                Combat.Damage.DamageType.Piercing    => "#AACCDD",
                Combat.Damage.DamageType.Slashing    => "#AAAACC",
                Combat.Damage.DamageType.Fire        => "#FF6622",
                Combat.Damage.DamageType.Cold        => "#88CCFF",
                Combat.Damage.DamageType.Lightning   => "#DDFF44",
                Combat.Damage.DamageType.Acid        => "#AADD44",
                Combat.Damage.DamageType.Force       => "#CC88FF",
                Combat.Damage.DamageType.Psychic     => "#FF88CC",
                Combat.Damage.DamageType.Necrotic    => "#AA55FF",
                Combat.Damage.DamageType.Radiant     => "#FFF5AA",
                _                                    => "#FFA500",
            };
        }

        // =====================================================================
        // FORMAT METHODS
        // =====================================================================

        // ==================== PRIMITIVES ====================
        // Not for direct use at call sites — prefer the semantic methods below.
        private static string Color(string hex, string text) => $"<color={hex}>{text}</color>";
        private static string Bold(string hex, string text)  => $"<b><color={hex}>{text}</color></b>";
        private static string Raw(string text)               => text;

        // ==================== SEMANTIC WRAPPERS ====================
        // Each method checks its backing constant — if null, returns plain text.
        public static string Success(string text)  => Color(SuccessColor,  text);
        public static string Failure(string text)  => Color(FailureColor,  text);
        public static string Damage(string text)   => Color(DamageColor,   text);
        public static string Energy(string text)   => Color(EnergyColor,   text);
        public static string Health(string text)   => Color(HealthColor,   text);
        public static string Skill(string text)    => Color(SkillColor,    text);
        public static string Number(string text)   => Raw(text);
        public static string DC(string text)       => Color(DCColor,       text);
        public static string Duration(string text) => Color(DurationColor, text);

        // Condition name — colour reflects buff (green) or debuff (red).
        public static string Condition(string text, bool isBuff) => isBuff ? Success(text) : Failure(text);

        // Damage type — delegates to GetDamageTypeColor.
        public static string DamageType(Combat.Damage.DamageType type, string text)
            => Color(GetDamageTypeColor(type), text);

        // Name format methods — Vehicle is bold, Component and Ability are plain colour.
        public static string Vehicle(string text)   => Bold(VehicleName,   text);
        public static string Component(string text) => Color(ComponentName, text);
        public static string Ability(string text)   => Color(AbilityName,   text);

        // Explicitly opt out of colouring a span — returns plain text.
        // Use this to keep call sites consistent with the format-method pattern.
        public static string Plain(string text) => Raw(text);

        // ==================== IMPORTANCE FORMATTING ====================
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
    }
}
