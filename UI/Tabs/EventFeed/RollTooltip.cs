using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Assets.Scripts.Combat;
using Assets.Scripts.Combat.Attacks;
using Assets.Scripts.Combat.Damage;

/// <summary>
/// Tooltip panel that displays detailed roll/damage breakdowns on hover.
/// Singleton that positions itself near the hovered UI element (not cursor).
/// 
/// Uses CombatLogManager for all formatting (single source of truth).
/// </summary>
public class RollTooltip : MonoBehaviour
{
    private static RollTooltip instance;
    public static RollTooltip Instance => instance;

    [Header("UI References")]
    [Tooltip("The panel that contains the tooltip content")]
    public RectTransform tooltipPanel;

    [Tooltip("Text component for the tooltip content")]
    public TextMeshProUGUI tooltipText;

    [Header("Settings")]
    [Tooltip("Offset from target element")]
    public Vector2 elementOffset = new Vector2(0f, 20f);

    [Tooltip("Padding from screen edges")]
    public float edgePadding = 10f;

    [Tooltip("Maximum tooltip width")]
    public float maxWidth = 350f;

    [Tooltip("Delay before showing tooltip (seconds)")]
    public float showDelay = 0f;

    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private bool isShowing = false;
    private float showTimer = 0f;
    private string pendingContent = null;
    private RectTransform targetElement = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }
        
        if (tooltipPanel != null)
        {
            var canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tooltipPanel.gameObject.AddComponent<CanvasGroup>();
            }
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        Hide();
    }

    void Update()
    {
        if (pendingContent != null && !isShowing)
        {
            showTimer += Time.unscaledDeltaTime;
            if (showTimer >= showDelay)
            {
                ShowImmediate(pendingContent);
                pendingContent = null;
            }
        }

        if (isShowing && targetElement != null)
        {
            UpdatePosition();
        }
    }

    /// <summary>
    /// Shows tooltip with delay at the position of the target element.
    /// </summary>
    public static void Show(string content, RectTransform target = null)
    {
        if (Instance == null || string.IsNullOrEmpty(content)) return;

        Instance.pendingContent = content;
        Instance.targetElement = target;
        Instance.showTimer = 0f;
    }

    /// <summary>
    /// Shows tooltip immediately without delay at the position of the target element.
    /// </summary>
    public static void ShowNow(string content, RectTransform target = null)
    {
        if (Instance == null) return;
        Instance.targetElement = target;
        Instance.ShowImmediate(content);
    }

    private void ShowImmediate(string content)
    {
        if (string.IsNullOrEmpty(content)) return;

        if (tooltipText != null)
        {
            tooltipText.text = content;
        }

        if (tooltipPanel != null)
        {
            // Move tooltip to end of sibling list (renders last = on top)
            tooltipPanel.SetAsLastSibling();
            
            tooltipPanel.gameObject.SetActive(true);

            // Force layout rebuild to get correct size
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        }

        isShowing = true;
        pendingContent = null;
        UpdatePosition();
    }

    /// <summary>
    /// Hides the tooltip.
    /// </summary>
    public static void Hide()
    {
        if (Instance == null) return;
        Instance.HideInternal();
    }

    private void HideInternal()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }

        isShowing = false;
        pendingContent = null;
        showTimer = 0f;
        targetElement = null;
    }

    /// <summary>
    /// Updates tooltip position to align with target element while staying on screen.
    /// </summary>
    private void UpdatePosition()
    {
        if (tooltipPanel == null || canvasRect == null) return;

        Vector2 targetPos;

        if (targetElement != null)
        {
            // Position relative to the target element (centered on it)
            targetPos = targetElement.position;
            targetPos += elementOffset;
        }
        else
        {
            // Fallback to cursor position if no target
            Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            targetPos = mousePos + elementOffset;
        }

        // Convert to canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetPos,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
            out Vector2 localPoint
        );

        // Get tooltip size
        Vector2 tooltipSize = tooltipPanel.sizeDelta;

        // Clamp to screen bounds
        float minX = -canvasRect.sizeDelta.x / 2 + edgePadding;
        float maxX = canvasRect.sizeDelta.x / 2 - tooltipSize.x - edgePadding;
        float minY = -canvasRect.sizeDelta.y / 2 + tooltipSize.y + edgePadding;
        float maxY = canvasRect.sizeDelta.y / 2 - edgePadding;

        localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

        tooltipPanel.anchoredPosition = localPoint;
    }

    /// <summary>
    /// Shows an attack result tooltip.
    /// </summary>
    public static void ShowAttackResult(AttackResult result, RectTransform target = null)
    {
        if (result == null) return;
        Show(CombatLogManager.FormatAttackDetailed(result), target);
    }

    /// <summary>
    /// Shows a damage result tooltip.
    /// </summary>
    public static void ShowDamageResult(DamageResult result, RectTransform target = null)
    {
        if (result == null) return;
        Show(CombatLogManager.FormatDamageDetailed(result), target);
    }

    /// <summary>
    /// Shows combined attack and damage result.
    /// </summary>
    public static void ShowCombinedResult(AttackResult attack, DamageResult damage, RectTransform target = null)
    {
        Show(CombatLogManager.FormatCombinedAttackAndDamage(attack, damage), target);
    }
}
