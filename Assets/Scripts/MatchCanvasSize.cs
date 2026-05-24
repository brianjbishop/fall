// MatchCanvasSize.cs
// ─────────────────────────────────────────────────────────────────────────────
// Attach to the parent object of the indicators.
// Every frame it stretches this RectTransform to exactly cover the Canvas,
// so any children anchored to its edges will track the true screen edges.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class MatchCanvasSize : MonoBehaviour
{
    private RectTransform rt;
    private Canvas rootCanvas;

    private void Awake() => Refresh();
    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();

    private void Refresh()
    {
        if (rt == null) rt = GetComponent<RectTransform>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas == null) return;

        RectTransform canvasRt = rootCanvas.GetComponent<RectTransform>();

        // Pin all four corners to the canvas corners
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;   // left / bottom offset
        rt.offsetMax        = Vector2.zero;   // right / top offset
    }
}
