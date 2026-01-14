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
    public Vector2 elementOffset = new Vector2(10f, -10f);

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
    /// Handles edge cases: flips horizontally if too close to left/right, flips vertically if too close to top/bottom.
    /// </summary>
    private void UpdatePosition()
    {
        if (tooltipPanel == null) return;

        // Force layout rebuild first to get accurate size
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        
        Vector2 tooltipSize = tooltipPanel.rect.size;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);
        float scaleFactor = parentCanvas != null ? parentCanvas.scaleFactor : 1f;
        Vector2 scaledTooltipSize = tooltipSize * scaleFactor;
        
        Camera cam = parentCanvas?.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas?.worldCamera;
        
        // Get target element bounds in screen space
        Vector2 targetCenter;
        Vector2 targetSize = Vector2.zero;
        
        if (targetElement != null)
        {
            Vector3[] worldCorners = new Vector3[4];
            targetElement.GetWorldCorners(worldCorners);
            
            // Convert corners to screen space
            Vector2 minScreen = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[0]);
            Vector2 maxScreen = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[2]);
            
            targetCenter = (minScreen + maxScreen) / 2f;
            targetSize = new Vector2(Mathf.Abs(maxScreen.x - minScreen.x), Mathf.Abs(maxScreen.y - minScreen.y));
        }
        else
        {
            // Fallback to cursor position
            targetCenter = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }
        
        // Determine best position: try right of target first, then left
        // Try below target first, then above
        
        Vector2 finalScreenPos;
        
        // Horizontal positioning
        float rightX = targetCenter.x + targetSize.x / 2f + Mathf.Abs(elementOffset.x);
        float leftX = targetCenter.x - targetSize.x / 2f - scaledTooltipSize.x - Mathf.Abs(elementOffset.x);
        
        bool canFitRight = (rightX + scaledTooltipSize.x + edgePadding) <= screenSize.x;
        bool canFitLeft = (leftX - edgePadding) >= 0;
        
        float finalX;
        if (canFitRight)
        {
            // Position to the right of target
            finalX = rightX;
        }
        else if (canFitLeft)
        {
            // Position to the left of target
            finalX = leftX;
        }
        else
        {
            // Can't fit on either side, clamp to screen
            finalX = Mathf.Clamp(rightX, edgePadding, screenSize.x - scaledTooltipSize.x - edgePadding);
        }
        
        // Vertical positioning
        float belowY = targetCenter.y - targetSize.y / 2f + elementOffset.y;
        float aboveY = targetCenter.y + targetSize.y / 2f + scaledTooltipSize.y - elementOffset.y;
        
        bool canFitBelow = (belowY - scaledTooltipSize.y - edgePadding) >= 0;
        bool canFitAbove = (aboveY + edgePadding) <= screenSize.y;
        
        float finalY;
        if (canFitBelow)
        {
            // Position below target (tooltip top-left corner at this Y)
            finalY = belowY;
        }
        else if (canFitAbove)
        {
            // Position above target
            finalY = aboveY;
        }
        else
        {
            // Can't fit above or below, clamp to screen
            finalY = Mathf.Clamp(belowY, scaledTooltipSize.y + edgePadding, screenSize.y - edgePadding);
        }
        
        finalScreenPos = new Vector2(finalX, finalY);
        
        // Convert screen position to local canvas space
        if (canvasRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                finalScreenPos,
                cam,
                out Vector2 localPoint
            );
            
            // Set position - assumes pivot is (0, 1) = top-left
            // If pivot is different, adjust accordingly
            Vector2 pivotOffset = new Vector2(
                tooltipPanel.pivot.x * tooltipSize.x,
                (tooltipPanel.pivot.y - 1f) * tooltipSize.y
            );
            
            tooltipPanel.anchoredPosition = localPoint + pivotOffset;
        }
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
