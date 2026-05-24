using UnityEngine;
using UnityEngine.UI;

public class DirectionalHighlight : MonoBehaviour
{
    [SerializeField] private Player player;

    [Header("Highlight Images")]
    [SerializeField] private Image forwardHighlight;
    [SerializeField] private Image backwardHighlight;
    [SerializeField] private Image leftHighlight;
    [SerializeField] private Image rightHighlight;

    [Header("Colors")]
    [SerializeField] private Color forwardColor  = Color.white;
    [SerializeField] private Color backwardColor = Color.white;
    [SerializeField] private Color leftColor     = Color.white;
    [SerializeField] private Color rightColor    = Color.white;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.3f;

    private Image[] highlights;
    private Color[] colors;
    private float[] timers;

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<Player>();

        highlights = new Image[] { forwardHighlight, backwardHighlight, leftHighlight, rightHighlight };
        colors     = new Color[] { forwardColor, backwardColor, leftColor, rightColor };
        timers     = new float[4];

        foreach (var img in highlights)
            if (img != null) img.color = Color.clear;
    }

    void Update()
    {
        if (player == null) return;

        if (player.IsForwardKeyDown())  timers[0] = fadeDuration;
        if (player.IsBackwardKeyDown()) timers[1] = fadeDuration;
        if (player.IsLeftKeyDown())     timers[2] = fadeDuration;
        if (player.IsRightKeyDown())    timers[3] = fadeDuration;

        for (int i = 0; i < 4; i++)
        {
            if (highlights[i] == null) continue;

            timers[i] = Mathf.Max(timers[i] - Time.deltaTime, 0f);
            Color c = colors[i];
            c.a = timers[i] / fadeDuration;
            highlights[i].color = c;
        }
    }
}
