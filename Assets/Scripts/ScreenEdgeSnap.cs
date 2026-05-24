// ScreenEdgeSnap.cs
// ─────────────────────────────────────────────────────────────────────────────
// Attach to a UI RectTransform. Each frame it snaps the element to the chosen
// screen edge, centered on that edge, regardless of resolution or aspect ratio.
//
// SETUP
//   1. Attach this script to each indicator Image.
//   2. Set Edge in the Inspector:
//        Yellow → Top
//        Green  → Bottom
//        Red    → Left
//        Blue   → Right
//   3. Use Inset to push the element away from the edge (0 = flush with edge).
//
// HOW IT WORKS
//   It sets the RectTransform anchor to the chosen edge each LateUpdate so the
//   position is always expressed relative to that edge — no hard-coded pixels.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;

[ExecuteAlways]   // preview in the Editor without entering Play mode
public class ScreenEdgeSnap : MonoBehaviour
{
    public enum Edge { Top, Bottom, Left, Right }

    [Tooltip("Which screen edge to snap to.\n" +
             "Yellow = Top  |  Green = Bottom  |  Red = Left  |  Blue = Right")]
    [SerializeField] private Edge edge = Edge.Top;

    [Tooltip("Distance (in canvas units) between the element and the screen edge.")]
    [SerializeField] private float inset = 0f;

    // ── Private ───────────────────────────────────────────────────────────────
    private RectTransform rt;

    private void Awake()  => rt = GetComponent<RectTransform>();
    private void OnEnable() => rt = GetComponent<RectTransform>();

    // LateUpdate so it runs after any other script that might move the element.
    private void LateUpdate() => Snap();

    private void Snap()
    {
        if (rt == null) return;

        float halfW = rt.rect.width  * 0.5f;
        float halfH = rt.rect.height * 0.5f;

        switch (edge)
        {
            case Edge.Top:
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -halfH - inset);
                break;

            case Edge.Bottom:
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, halfH + inset);
                break;

            case Edge.Left:
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(halfW + inset, 0f);
                break;

            case Edge.Right:
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-halfW - inset, 0f);
                break;
        }
    }
}
