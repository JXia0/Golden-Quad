#if UNITY_EDITOR
using System.Collections;
using System.IO;
using LetGo;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class JourneyPlaythroughDriver
{
    private const float MemoryReviewFloor = -2.72f;
    private string memoryReviewCapturePath;

    private IEnumerator ReviewFinalMemoryPresentation()
    {
        phase = "the real stage remains drawn beyond its right curtain";
        SceneManager.LoadScene("03_Stage");
        yield return Keys(1.4f);
        var presentation = FindAnyObjectByType<SceneArtPresentation>();
        if (!Check(presentation != null, "the real stage has its runtime scene presentation")) yield break;
        var serializedPresentation = new UnityEditor.SerializedObject(presentation);
        var limits = serializedPresentation.FindProperty("backdropLimits").vector2Value;
        var rightCurtain = GameObject.Find("Curtain Right");
        var ground = GameObject.Find("Ground")?.GetComponent<Collider2D>();
        if (!Check(rightCurtain != null && ground != null,
            "the stage review uses its actual right curtain and walkable floor")) yield break;
        var rightEnd = Mathf.Min(limits.y - 0.2f, ground.bounds.max.x - 0.2f);
        var exit = GameObject.Find("Back Curtain");
        var exitHidden = exit != null && exit.GetComponent<StoryDoor>() != null && exit.GetComponent<Collider2D>() != null;
        if (exit != null)
            foreach (var renderer in exit.GetComponentsInChildren<SpriteRenderer>(true))
                exitHidden &= !renderer.enabled;
        if (!Check(exitHidden, "the stage exit keeps its door interaction without a translucent prototype rectangle")) yield break;
        var stagePositions = new[] { 0f, 10f, rightCurtain.transform.position.x - 1.8f,
            rightCurtain.transform.position.x + 1.8f, rightEnd };
        for (var i = 0; i < stagePositions.Length; i++)
        {
            phase = "stage picture coverage at x=" + stagePositions[i].ToString("F2");
            yield return Walk(stagePositions[i]);
            yield return Keys(0.4f);
            yield return SaveMemoryReviewCapture("03-right-curtain-coverage-" + i);
            if (finished) yield break;
            if (!Check(Player.ControlsEnabled && Mathf.Abs(Player.transform.position.x - stagePositions[i]) < 0.3f,
                "the performer can reach the curtain review position " + i)) yield break;
            if (!CheckRenderedMemoryFloor(false, "stage position " + i)) yield break;
        }

        phase = "the memory walk preserves grounded objects instead of retired floating cards";
        // These are prior-chapter choices, not invented renderer positions or relay results.
        JourneyChoices.RememberWorkshop("lamp", "crate", false);
        SceneManager.LoadScene("06_FinalWalk");
        yield return Keys(1.4f);
        var centers = FinalPassageLayout.Centers;
        var firstCurtain = GameObject.Find("Memory Black Passage 1")?.GetComponent<SpriteRenderer>();
        if (!Check(firstCurtain != null, "the memory review reads the actual walking curtain width")) yield break;
        var halfCurtain = firstCurtain.bounds.extents.x;
        var stageStart = centers[0] + halfCurtain + 1f;
        var stageEnd = centers[1] - halfCurtain - 0.2f;
        var researchStart = centers[1] + halfCurtain + 1f;
        var researchEnd = centers[2] - halfCurtain - 0.2f;
        yield return Walk(centers[0] - 0.5f);
        yield return Keys(0.3f);
        if (!Check(Player.CharacterRenderer.sprite.name.StartsWith("char_child") && PassageAlpha() >= 0.99f,
            "the child enters the first full black passage before the age changes")) yield break;
        yield return Walk(centers[0] + 0.5f);
        yield return Keys(0.3f);
        if (!Check(Player.CharacterRenderer.sprite.name.StartsWith("char_teen") && PassageAlpha() >= 0.99f,
            "the teenager appears while the first passage still completely covers the change")) yield break;

        var stageMemoryPositions = new[] { stageStart, (centers[0] + centers[1]) * 0.5f,
            stageEnd - 0.8f, stageEnd };
        for (var i = 0; i < stageMemoryPositions.Length; i++)
        {
            yield return ReviewMemoryPictureAt(stageMemoryPositions[i], "char_teen", "06-stage-memory-" + i);
            if (finished) yield break;
        }
        yield return Walk(centers[1] + 0.5f);
        yield return Keys(0.3f);
        if (!Check(Player.CharacterRenderer.sprite.name.StartsWith("char_adult") && PassageAlpha() >= 0.99f,
            "the research age changes inside the next world curtain")) yield break;
        var researchMemoryPositions = new[] { researchStart, (centers[1] + centers[2]) * 0.5f,
            researchEnd - 0.8f, researchEnd };
        for (var i = 0; i < researchMemoryPositions.Length; i++)
        {
            yield return ReviewMemoryPictureAt(researchMemoryPositions[i], "char_adult", "06-research-memory-" + i);
            if (finished) yield break;
            if (i == 1 && !CheckResearchMemoryStillLife()) yield break;
        }

        phase = "the revised memory rooms retain reversible ages";
        yield return Walk(centers[1] - 0.5f);
        yield return Keys(0.3f);
        if (!Check(PassageAlpha() >= 0.99f && Player.CharacterRenderer.sprite.name.StartsWith("char_teen"),
            "walking back from research restores the teenager while fully hidden")) yield break;
        yield return ReviewMemoryPictureAt(stageEnd - 0.8f, "char_teen", "06-stage-memory-return");
        if (finished) yield break;
        yield return Walk(centers[0] - 0.5f);
        yield return Keys(0.3f);
        if (!Check(PassageAlpha() >= 0.99f && Player.CharacterRenderer.sprite.name.StartsWith("char_child"),
            "walking back across the first memory boundary restores the child inside black")) yield break;
        yield return Walk(centers[0] - halfCurtain - 1f);
        yield return Keys(0.4f);
        yield return SaveMemoryReviewCapture("06-child-memory-return");
        if (finished) yield break;
        if (!Check(Player.CharacterRenderer.sprite.name.StartsWith("char_child") && Player.ControlsEnabled,
            "the child is still playable after returning through both revised rooms")) yield break;
        yield return ReviewMemoryPictureAt(stageStart, "char_teen", "06-stage-memory-reentered");
        if (finished) yield break;
        yield return ReviewMemoryPictureAt((centers[1] + centers[2]) * 0.5f, "char_adult", "06-research-memory-reentered");
        if (finished) yield break;
        var repeated = true;
        var repeatedRegions = 0;
        foreach (var age in FindObjectsByType<FinalAgeTransition>())
            if (age.ArtSlotId == "char_teen" || age.ArtSlotId == "char_adult")
            {
                repeated &= age.Applications >= 2;
                repeatedRegions++;
            }
        if (!Check(repeated && repeatedRegions == 2, "both age transitions remain repeatable after the presentation changes")) yield break;
        CheckResearchMemoryStillLife();
    }

    private IEnumerator ReviewMemoryPictureAt(float x, string age, string capture)
    {
        phase = capture + " at x=" + x.ToString("F2");
        yield return Walk(x);
        yield return Keys(0.4f);
        yield return SaveMemoryReviewCapture(capture);
        if (finished) yield break;
        if (!Check(Player.ControlsEnabled && Player.CharacterRenderer.sprite.name.StartsWith(age) &&
            Mathf.Abs(Player.transform.position.x - x) < 0.3f,
            "the player reaches " + capture + " in the correct playable age")) yield break;
        if (!CheckRetiredMemoryCards()) yield break;
        CheckRenderedMemoryFloor(true, capture);
    }

    private bool CheckRetiredMemoryCards()
    {
        var legacyNames = new[] { "Remembered Question", "Remembered Photo Evidence", "Remembered Data Evidence",
            "Remembered Conclusion", "Remembered Steady Route", "Remembered Forward Route", "Memory Spotlight" };
        foreach (var transform in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        foreach (var legacy in legacyNames)
        {
            if (transform.name != legacy) continue;
            foreach (var renderer in transform.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.color.a > 0.01f)
                    return Check(false, "retired floating memory artwork is visible again: " + legacy);
        }
        return Check(true, "the old question, evidence, conclusion and abstract route sprites stay hidden at this walking position");
    }

    private bool CheckResearchMemoryStillLife()
    {
        var desk = GameObject.Find("Remembered Workbench")?.GetComponent<SpriteRenderer>();
        var report = GameObject.Find("Remembered Finished Report")?.GetComponent<SpriteRenderer>();
        var crate = GameObject.Find("Remembered Crossing Tool")?.GetComponent<SpriteRenderer>();
        var lamp = GameObject.Find("Remembered Lamp")?.GetComponent<SpriteRenderer>();
        if (!Check(MemoryPropVisible(desk) && MemoryPropVisible(report) && MemoryPropVisible(crate) && MemoryPropVisible(lamp),
            "the delivered workbench, completed report, chosen crate and lamp are visible together in the research memory")) return false;
        if (!Check(Mathf.Abs(desk.bounds.min.y - MemoryReviewFloor) < 0.08f &&
            Mathf.Abs(crate.bounds.min.y - MemoryReviewFloor) < 0.08f &&
            Mathf.Abs(lamp.bounds.min.y - MemoryReviewFloor) < 0.08f,
            "the remembered desk, crossing tool and lamp visibly meet the floor")) return false;
        return Check(report.bounds.min.y >= desk.bounds.max.y - 0.08f &&
            report.bounds.min.y <= desk.bounds.max.y + 0.02f &&
            report.bounds.size.y < 0.25f && report.bounds.size.x > 0.65f &&
            report.bounds.min.x >= desk.bounds.min.x && report.bounds.max.x <= desk.bounds.max.x &&
            UnityEditor.AssetDatabase.GetAssetPath(report.sprite.texture) == "Assets/Sprites/prop_research_report.png" &&
            UnityEditor.AssetDatabase.GetAssetPath(crate.sprite.texture) == "Assets/Sprites/prop_research_crate.png",
            "the completed report lies flat against the painted tabletop, and the crossing tool matches the prior crate choice");
    }

    private static bool MemoryPropVisible(SpriteRenderer renderer)
    {
        return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy &&
            renderer.sprite != null && renderer.color.a > 0.5f &&
            GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(Camera.main), renderer.bounds);
    }

    private IEnumerator SaveMemoryReviewCapture(string name)
    {
        memoryReviewCapturePath = null;
        var path = Path.Combine("Logs", "GameplayCaptures", name + ".png");
        var previousWrite = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : System.DateTime.MinValue;
        var requestedAt = System.DateTime.UtcNow;
        var deadline = Time.realtimeSinceStartup + 8f;
        Capture(name);
        // Keep the character still while ScreenCapture saves a real game frame. EndOfFrame
        // coroutines are not reliable in batchmode, and the current framebuffer may be an editor view.
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return Keys(0.2f);
            if (!File.Exists(path)) continue;
            var writtenAt = File.GetLastWriteTimeUtc(path);
            if (writtenAt < requestedAt || writtenAt == previousWrite) continue;
            byte[] data = null;
            try { data = File.ReadAllBytes(path); }
            catch (IOException) { }
            // A fresh timestamp alone can belong to a PNG still being written. Its IEND chunk
            // must be present before any pixels are decoded and used as evidence.
            if (data == null || data.Length < 20 || data[data.Length - 8] != 73 ||
                data[data.Length - 7] != 69 || data[data.Length - 6] != 78 || data[data.Length - 5] != 68) continue;
            memoryReviewCapturePath = path;
            Check(true, "a newly saved complete game screenshot is available for " + name);
            yield break;
        }
        Check(false, "the requested game screenshot was not saved within eight seconds: " + name);
    }

    private bool CheckRenderedMemoryFloor(bool excludeCurtains, string moment)
    {
        // Inspect the rendered frame, not only sprite bounds: opaque black pixels inside a large
        // panorama used to pass geometry-only coverage checks while leaving a visible empty strip.
        const int columns = 160;
        var view = Camera.main;
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (string.IsNullOrEmpty(memoryReviewCapturePath) ||
            !texture.LoadImage(File.ReadAllBytes(memoryReviewCapturePath)))
        {
            Destroy(texture);
            return Check(false, "the newly saved game screenshot could not be decoded for " + moment);
        }
        var imageWidth = texture.width;
        var imageHeight = texture.height;
        var pixels = texture.GetPixels32();
        Destroy(texture);
        var halfWidth = view.orthographicSize * view.aspect;
        var worldStep = halfWidth * 2f / columns;
        var longest = 0f;
        var run = 0f;
        var architectureLongest = 0f;
        var architectureRun = 0f;
        var inspectArchitecture = SceneManager.GetActiveScene().name == "03_Stage";
        var inspected = 0;
        var rendered = FindObjectsByType<SpriteRenderer>();
        for (var column = 1; column < columns - 1; column++)
        {
            var worldX = view.transform.position.x - halfWidth + (column + 0.5f) * worldStep;
            var covered = false;
            if (excludeCurtains)
                foreach (var renderer in rendered)
                    if (renderer.name.StartsWith("Memory Black Passage ") && renderer.enabled && renderer.color.a >= 0.99f &&
                        worldX >= renderer.bounds.min.x - 0.04f && worldX <= renderer.bounds.max.x + 0.04f) covered = true;
            if (covered) { run = architectureRun = 0f; continue; }
            var dark = true;
            var samples = 0;
            foreach (var height in new[] { 0.10f, 0.36f, 0.72f })
            {
                var point = view.WorldToViewportPoint(new Vector3(worldX, MemoryReviewFloor + height, 0f));
                var px = Mathf.RoundToInt(point.x * imageWidth);
                var py = Mathf.RoundToInt(point.y * imageHeight);
                if (px < 0 || px >= imageWidth || py < 0 || py >= imageHeight) continue;
                var pixel = pixels[py * imageWidth + px];
                dark &= pixel.r <= 5 && pixel.g <= 5 && pixel.b <= 5;
                samples++;
            }
            if (samples == 0) continue;
            inspected++;
            run = dark ? run + worldStep : 0f;
            longest = Mathf.Max(longest, run);
            if (inspectArchitecture)
            {
                var architectureDark = true;
                foreach (var worldY in new[] { -0.45f, 0.65f, 1.8f })
                {
                    var point = view.WorldToViewportPoint(new Vector3(worldX, worldY, 0f));
                    var px = Mathf.Clamp(Mathf.RoundToInt(point.x * imageWidth), 0, imageWidth - 1);
                    var py = Mathf.Clamp(Mathf.RoundToInt(point.y * imageHeight), 0, imageHeight - 1);
                    var pixel = pixels[py * imageWidth + px];
                    architectureDark &= pixel.r <= 5 && pixel.g <= 5 && pixel.b <= 5;
                }
                architectureRun = architectureDark ? architectureRun + worldStep : 0f;
                architectureLongest = Mathf.Max(architectureLongest, architectureRun);
            }
        }
        return Check(inspected >= 25 && longest < 1.6f && (!inspectArchitecture || architectureLongest < 2f),
            "the rendered walking picture has no long pure-black floor gap outside its deliberate curtains at " + moment +
            " (floor=" + longest.ToString("F2") + "m, auditorium=" + architectureLongest.ToString("F2") +
            "m, sampled=" + inspected + ")");
    }
}
#endif
