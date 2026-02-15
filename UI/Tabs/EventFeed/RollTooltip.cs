using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

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
    public Vector2 elementOffset = new(10f, -10f);

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

    public static void Show(string content, RectTransform target = null)
    {
        if (Instance == null || string.IsNullOrEmpty(content)) return;

        Instance.pendingContent = content;
        Instance.targetElement = target;
        Instance.showTimer = 0f;
    }

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
            tooltipPanel.SetAsLastSibling();
            tooltipPanel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        }

        isShowing = true;
        pendingContent = null;
        UpdatePosition();
    }

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

    private void UpdatePosition()
    {
        if (tooltipPanel == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);

        Vector2 tooltipSize = tooltipPanel.rect.size;
        Vector2 screenSize = new(Screen.width, Screen.height);
        float scaleFactor = parentCanvas != null ? parentCanvas.scaleFactor : 1f;
        Vector2 scaledTooltipSize = tooltipSize * scaleFactor;
        
        Camera cam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) 
            ? null 
            : (parentCanvas != null ? parentCanvas.worldCamera : null);
        
        Vector2 targetCenter;
        Vector2 targetSize = Vector2.zero;

        if (targetElement != null)
        {
            Vector3[] worldCorners = new Vector3[4];
            targetElement.GetWorldCorners(worldCorners);

            Vector2 minScreen = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[0]);
            Vector2 maxScreen = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[2]);

            targetCenter = (minScreen + maxScreen) / 2f;
            targetSize = new Vector2(Mathf.Abs(maxScreen.x - minScreen.x), Mathf.Abs(maxScreen.y - minScreen.y));
        }
        else
        {
            targetCenter = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        Vector2 finalScreenPos;

        // Horizontal positioning
        float rightX = targetCenter.x + targetSize.x / 2f + Mathf.Abs(elementOffset.x);
        float leftX = targetCenter.x - targetSize.x / 2f - scaledTooltipSize.x - Mathf.Abs(elementOffset.x);
        
        bool canFitRight = (rightX + scaledTooltipSize.x + edgePadding) <= screenSize.x;
        bool canFitLeft = (leftX - edgePadding) >= 0;
        
        float finalX;
        if (canFitRight)
            finalX = rightX;
        else if (canFitLeft)
            finalX = leftX;
        else
            finalX = Mathf.Clamp(rightX, edgePadding, screenSize.x - scaledTooltipSize.x - edgePadding);

        float belowY = targetCenter.y - targetSize.y / 2f + elementOffset.y;
        float aboveY = targetCenter.y + targetSize.y / 2f + scaledTooltipSize.y - elementOffset.y;

        bool canFitBelow = (belowY - scaledTooltipSize.y - edgePadding) >= 0;
        bool canFitAbove = (aboveY + edgePadding) <= screenSize.y;

        float finalY;
        if (canFitBelow)
            finalY = belowY;
        else if (canFitAbove)
            finalY = aboveY;
        else
            finalY = Mathf.Clamp(belowY, scaledTooltipSize.y + edgePadding, screenSize.y - edgePadding);

        finalScreenPos = new Vector2(finalX, finalY);

        if (canvasRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                finalScreenPos,
                cam,
                out Vector2 localPoint
            );

            Vector2 pivotOffset = new(
                tooltipPanel.pivot.x * tooltipSize.x,
                (tooltipPanel.pivot.y - 1f) * tooltipSize.y
            );

            tooltipPanel.anchoredPosition = localPoint + pivotOffset;
        }
    }

    }
