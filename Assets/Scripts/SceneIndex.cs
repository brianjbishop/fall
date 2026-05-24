// SceneIndex.cs
// ─────────────────────────────────────────────────────────────────────────────
// Central lookup for every scene's build index.
// Add scenes here as you create them so no other script ever uses a raw number.
//
// To add a new level:
//   1. Create the scene in Unity (File → New Scene).
//   2. Add it to Build Settings (File → Build Settings → drag it in).
//   3. Note its build index and add a constant below.
//   4. Add that index to GameManager.levelSceneIndices in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────

public static class SceneIndex
{
    // The opening scene — camera drop intro, no gameplay yet.
    public const int Start = 0;

    // Countdown scene — shows 3, 2, 1, GO! then loads a random level.
    public const int Countdown = 1;

    // Level scenes start at index 2 and go up.
    // They are stored as an array on GameManager (levelSceneIndices)
    // so you can add levels without changing any code.

    // Game over screen — shows score and waits for restart input.
    // Kept at a high index so inserting new levels never shifts it.
    public const int GameOver = 9;
}
