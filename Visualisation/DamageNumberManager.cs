using System.Collections;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Restoration;
using Assets.Scripts.Logging;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Visualisation
{
    /// <summary>
    /// Spawns and animates floating damage numbers in world space when damage is dealt.
    /// Also shows a "Miss" indicator on failed attack rolls and a ghost segment on the
    /// HP bar to show the magnitude of each HP change.
    /// Place on any persistent scene GameObject (e.g. TrackVisualizationManager).
    /// </summary>
    public class DamageNumberManager : MonoBehaviour
    {
        [Header("Animation")]
        [Tooltip("How far upward the number floats before fading out.")]
        [SerializeField] private float floatDistance = 1.5f;

        [Tooltip("Total duration of the float and fade animation in seconds.")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("Height offset above the target's origin where the number spawns.")]
        [SerializeField] private float spawnHeightOffset = 0.5f;

        [Header("Text")]
        [Tooltip("Font size for normal hits.")]
        [SerializeField] private float fontSize = 4f;

        [Tooltip("Font size for critical hits.")]
        [SerializeField] private float critFontSize = 6f;

        [Tooltip("Font size for Miss indicators.")]
        [SerializeField] private float missFontSize = 3f;

        private void OnEnable()
        {
            CombatEventBus.OnDamage       += HandleDamage;
            CombatEventBus.OnRestoration  += HandleRestoration;
            CombatEventBus.OnAttackRoll   += HandleAttackRoll;
        }

        private void OnDisable()
        {
            CombatEventBus.OnDamage       -= HandleDamage;
            CombatEventBus.OnRestoration  -= HandleRestoration;
            CombatEventBus.OnAttackRoll   -= HandleAttackRoll;
        }

        private void HandleDamage(DamageEvent evt)
        {
            if (evt.Target == null)
                return;

            Vector3 origin = evt.Target.transform.position + Vector3.up * spawnHeightOffset;
            string colorHex = LogColors.GetDamageTypeColor(evt.Result.DamageType);
            string text = evt.Result.IsCritical
                ? $"<b>{evt.Result.FinalDamage}!</b>"
                : evt.Result.FinalDamage.ToString();
            float size = evt.Result.IsCritical ? critFontSize : fontSize;

            StartCoroutine(SpawnNumber(origin, text, colorHex, size));
        }

        private void HandleRestoration(RestorationEvent evt)
        {
            if (evt.Target == null)
                return;

            if (evt.Result.ResourceType != ResourceType.Health)
                return;

            Vector3 origin = evt.Target.transform.position + Vector3.up * spawnHeightOffset;
            string text = evt.Result.ActualChange >= 0
                ? $"+{evt.Result.ActualChange}"
                : evt.Result.ActualChange.ToString();

            StartCoroutine(SpawnNumber(origin, text, LogColors.HealthColor, fontSize));
        }

        private void HandleAttackRoll(AttackRollEvent evt)
        {
            if (evt.Roll.Success || evt.Target == null)
                return;

            Vector3 origin = evt.Target.transform.position + Vector3.up * spawnHeightOffset;
            StartCoroutine(SpawnNumber(origin, "Miss", LogColors.FailureColor, missFontSize));
        }

        private IEnumerator SpawnNumber(Vector3 origin, string text, string colorHex, float size)
        {
            GameObject go = new GameObject("DamageNumber");
            go.transform.position = origin;

            TextMeshPro tmp = go.AddComponent<TextMeshPro>();
            tmp.text         = text;
            tmp.fontSize     = size;
            tmp.color        = HexToColor(colorHex);
            tmp.alignment    = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;

                go.transform.position = origin + Vector3.up * (floatDistance * t);

                Camera cam = Camera.main;
                if (cam != null)
                    go.transform.rotation = cam.transform.rotation;

                // Fade out in the second half
                float alpha = t < 0.5f ? 1f : 1f - ((t - 0.5f) / 0.5f);
                Color c = tmp.color;
                c.a = alpha;
                tmp.color = c;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(go);
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                return c;

            return Color.white;
        }
    }
}
