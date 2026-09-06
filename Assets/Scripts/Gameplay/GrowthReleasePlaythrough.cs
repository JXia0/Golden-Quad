#if UNITY_EDITOR
using System.Collections;
using LetGo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public sealed partial class JourneyPlaythroughDriver
{
    private IEnumerator ReviewDeliveredVoice()
    {
        phase = "the user's recorded narration plays only after Start and respects pause";
        var clip = Resources.Load<AudioClip>("Voice/vo_prologue");
        var flashbackVoice = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Voice/vo_flashback.wav");
        if (!Check(clip != null && clip.length > 6.5f && flashbackVoice != null && flashbackVoice.length > 15f,
            "both delivered user recordings import as real audio clips (opening=" + clip?.length.ToString("F2") +
            "s, flashback=" + flashbackVoice?.length.ToString("F2") + "s)")) yield break;
        SceneManager.LoadScene("00_Prologue");
        yield return Keys(1.5f);
        var sceneAudio = FindAnyObjectByType<SceneAudio>();
        var serializedAudio = new UnityEditor.SerializedObject(sceneAudio);
        var music = (AudioSource)serializedAudio.FindProperty("musicSource").objectReferenceValue;
        var ambience = (AudioSource)serializedAudio.FindProperty("ambienceSource").objectReferenceValue;
        var musicBefore = music.volume;
        var ambienceBefore = ambience.volume;
        if (!Check(GameObject.Find("Opening Voice") == null && FindAnyObjectByType<MainMenuController>().Visible,
            "the real opening recording cannot start behind the title")) yield break;
        yield return Keys(0.15f, Key.Enter);
        yield return Keys(1.7f);
        var voice = GameObject.Find("Opening Voice")?.GetComponent<AudioSource>();
        var narration = GameObject.Find("Narration")?.GetComponent<UnityEngine.UI.Text>();
        if (!Check(voice != null && voice.clip == clip && voice.isPlaying && voice.spatialBlend == 0f &&
            narration != null && narration.text.StartsWith("小时候"),
            "Start plays the user's actual opening recording together with its complete subtitle")) yield break;
        if (!Check(music.volume < musicBefore * 0.5f && ambience.volume < ambienceBefore && music.isPlaying && ambience.isPlaying,
            "the running music and room sound become quieter while the user's voice speaks")) yield break;
        var samples = voice.timeSamples;
        yield return Keys(0.3f);
        if (!Check(voice.timeSamples > samples, "the user's voice audio source actually advances")) yield break;
        yield return UnscaledKeys(0.15f, Key.Escape);
        samples = voice.timeSamples;
        yield return UnscaledKeys(0.35f);
        if (!Check(AudioListener.pause && voice.timeSamples == samples && !string.IsNullOrEmpty(narration.text),
            "pausing freezes the actual spoken recording and retains its subtitle")) yield break;
        yield return UnscaledKeys(0.15f, Key.Escape);
        yield return Keys(0.35f);
        if (!Check(!AudioListener.pause && voice.timeSamples > samples,
            "resuming continues the recorded voice instead of restarting or losing it")) yield break;
        yield return Keys(Mathf.Max(0.1f, 6.3f - voice.time));
        if (!Check(voice.isPlaying && !string.IsNullOrEmpty(narration.text),
            "the recorded opening and its subtitle continue beyond the old six-second text timeout")) yield break;
        Capture("00-delivered-voice-after-six-seconds");
        yield return Keys(0.2f);
        yield return Keys(Mathf.Max(0.1f, clip.length - voice.time + 0.7f));
        if (!Check(voice == null && GameObject.Find("Opening Voice") == null && string.IsNullOrEmpty(narration.text) &&
            Mathf.Approximately(music.volume, musicBefore) && Mathf.Approximately(ambience.volume, ambienceBefore),
            "finishing the recording clears its voice and subtitle and restores the previous mix")) yield break;
        SceneManager.LoadScene("00_Prologue");
        yield return Keys(1.4f);
        yield return Keys(0.15f, Key.Enter);
        yield return Keys(1.7f);
        voice = GameObject.Find("Opening Voice")?.GetComponent<AudioSource>();
        if (!Check(voice != null && voice.isPlaying, "a new journey starts a fresh recording for the transition cleanup check")) yield break;
        SceneManager.LoadScene("01_Kindergarten");
        yield return Keys(1.3f);
        if (!Check(voice == null && GameObject.Find("Opening Voice") == null && !StorySceneDirector.Instance.NarrationEnabled,
            "changing chapters stops the recording and does not revive retired narration")) yield break;
    }

    private IEnumerator UnscaledKeys(float seconds, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        yield return new WaitForSecondsRealtime(seconds);
    }

    private IEnumerator ReviewPauseMenu()
    {
        phase = "pause retains world state and delivers a released hand after resume";
        var pause = FindAnyObjectByType<PauseMenuController>();
        var playerX = Player.transform.position.x;
        yield return UnscaledKeys(0.15f, Key.Escape);
        if (!Check(pause != null && pause.IsPaused && pause.Visible && Time.timeScale == 0f && AudioListener.pause,
            "Escape opens the pause menu and freezes game time and sound")) yield break;
        if (!CheckPauseArtwork(pause)) yield break;
        var titleMusic = Resources.Load<SceneAudioLibrary>("SceneAudioLibrary")?.titleMusic;
        AudioSource pausedMusic = null;
        foreach (var source in FindObjectsByType<AudioSource>())
            if (source.clip == titleMusic && source.loop) pausedMusic = source;
        if (!Check(titleMusic != null && pausedMusic != null, "the pause test starts over the delivered title melody's real looping audio source")) yield break;
        var pausedSample = pausedMusic.timeSamples;
        Capture("00-pause-menu");
        yield return UnscaledKeys(0.35f, Key.D, Key.Space, Key.F, Key.T, Key.R);
        if (!Check(Mathf.Abs(Player.transform.position.x - playerX) < 0.02f && pause.IsPaused,
            "movement, interaction, restart and demonstration keys cannot act behind the pause menu")) yield break;
        if (!Check(pausedMusic.timeSamples == pausedSample,
            "the actual melody playback position stays frozen while the menu is open")) yield break;
        var mouse = InputSystem.AddDevice<Mouse>();
        var pauseArtwork = GameObject.Find("Delivered Pause Menu").GetComponent<UnityEngine.UI.Image>();
        var point = PauseArtworkScreenPoint(pauseArtwork, new Vector2(768f, 562f));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return UnscaledKeys(0.15f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        yield return UnscaledKeys(0.15f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return UnscaledKeys(0.25f);
        InputSystem.RemoveDevice(mouse);
        if (!Check(!pause.IsPaused && !pause.Visible && Time.timeScale == 1f && !AudioListener.pause,
            "clicking Continue restores the world and its audio")) yield break;

        SceneManager.LoadScene("04_Interlude_Growing");
        yield return Keys(1.3f);
        var titleStillPlaying = false;
        foreach (var source in FindObjectsByType<AudioSource>())
            titleStillPlaying |= source.isPlaying && source.clip == titleMusic;
        if (!Check(!titleStillPlaying, "loading the paper bridge cannot leave the title melody playing in the next scene")) yield break;
        if (!CheckSceneMusic(Resources.Load<SceneAudioLibrary>("SceneAudioLibrary")?.journeyMusic,
            "the restored paper interlude starts its own live journey-music loop")) yield break;
        if (!Check(FindObjectsByType<SceneAudio>().Length == 1,
            "the interlude audio bootstrap creates one audio controller without overlapping copies")) yield break;
        pause = FindAnyObjectByType<PauseMenuController>();
        var bridge = FindAnyObjectByType<InterludeController>().Bridge;
        yield return Keys(0.15f, Key.D);
        yield return Keys(0.15f);
        yield return Keys(0.15f, Key.E, Key.F);
        yield return Keys(2.5f, Key.E);
        if (!Check(bridge.Phase == PaperBridgePhase.Walking && bridge.Holding && bridge.WalkerX > 0.25f,
            "the pause boundary starts during an actual supported paper crossing")) yield break;
        yield return UnscaledKeys(0.15f, Key.E, Key.Escape);
        var pausedX = bridge.WalkerX;
        yield return UnscaledKeys(0.45f, Key.Space, Key.D, Key.Q, Key.F);
        if (!Check(pause.IsPaused && bridge.Holding && bridge.Phase == PaperBridgePhase.Walking &&
            Mathf.Abs(bridge.WalkerX - pausedX) < 0.001f && bridge.AttemptCount == 1 && !bridge.Folded[1],
            "releasing E and pressing game controls in the menu leaves the exact paper trial frozen")) yield break;
        Capture("04-pause-retains-held-paper");
        yield return UnscaledKeys(0.15f, Key.Escape);
        yield return Keys(0.5f);
        if (!Check(!pause.IsPaused && !bridge.Holding && bridge.Phase == PaperBridgePhase.Blocked &&
            bridge.Failure == PaperBridgeFailure.Unsupported,
            "resuming delivers the released hand and the paper genuinely loses support")) yield break;
        Capture("04-resume-delivers-release");
        yield return UnscaledKeys(0.15f, Key.Escape);
        if (!Check(pause.IsPaused, "the menu can be reopened after a resumed failure")) yield break;
        pause.MainMenuButton.onClick.Invoke();
        yield return UnscaledKeys(1.4f);
        var menu = FindAnyObjectByType<MainMenuController>();
        if (!Check(SceneManager.GetActiveScene().name == "00_Prologue" && Time.timeScale == 1f &&
            !AudioListener.pause && menu != null && menu.Visible && !Player.ControlsEnabled && JourneyChoices.PaperBridgeAttempts == 0,
            "returning to the title restores time and sound and starts a fresh journey at the menu")) yield break;
        if (!CheckSceneMusic(titleMusic, "returning through the pause menu restarts the delivered title melody as a loop")) yield break;
        yield return UnscaledKeys(0.15f, Key.Escape);
        if (!Check(!FindAnyObjectByType<PauseMenuController>().IsPaused && menu.Visible,
            "Escape cannot place another pause menu over the title screen")) yield break;
        yield return Keys(0.15f);
    }

    private IEnumerator ReviewTitleMenu()
    {
        phase = "delivered title menu and click entry";
        SceneManager.LoadScene("00_Prologue");
        yield return Keys(1.4f);
        var menu = FindAnyObjectByType<MainMenuController>();
        if (!Check(menu != null && menu.Visible && !Player.ControlsEnabled &&
            menu.Background.sprite != null && menu.StartButton.image.sprite != null,
            "the delivered title and Start artwork are visible before the prologue can move")) yield break;
        var narration = GameObject.Find("Narration")?.GetComponent<UnityEngine.UI.Text>();
        if (!Check(narration != null && string.IsNullOrEmpty(narration.text) && GameObject.Find("Opening Voice") == null,
            "opening narration waits for Start instead of being consumed behind the title")) yield break;
        var library = Resources.Load<SceneAudioLibrary>("SceneAudioLibrary");
        var titleMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/LoopAudio/bgm_main_theme_v2_主旋律.WAV");
        if (!Check(library != null && titleMusic != null && library.titleMusic == titleMusic,
            "the title music slot references the newly delivered main-theme recording")) yield break;
        if (!CheckSceneMusic(titleMusic, "the real title scene plays the new main melody in a live loop")) yield break;
        Capture("00-delivered-title-menu");
        var initialX = Player.transform.position.x;
        yield return Keys(0.4f, Key.D);
        yield return Keys(0.2f);
        if (!Check(Mathf.Abs(Player.transform.position.x - initialX) < 0.02f && menu.Visible,
            "movement keys do not move the hidden prologue character or dismiss the menu")) yield break;
        var mouse = InputSystem.AddDevice<Mouse>();
        var point = RectTransformUtility.WorldToScreenPoint(null, menu.StartButton.transform.position);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return Keys(0.2f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        yield return Keys(0.15f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return Keys(0.9f);
        InputSystem.RemoveDevice(mouse);
        if (!Check(menu.HasStarted && !menu.Visible && Player.ControlsEnabled &&
            SceneManager.GetActiveScene().name == "00_Prologue",
            "an actual pointer click fades the title and returns control to the playable prologue")) yield break;
        if (!Check(string.IsNullOrEmpty(narration.text),
            "the opening keeps its short breathing space after the title fades")) yield break;
        yield return Keys(0.85f);
        if (!Check(narration.text == "小时候，我以为长大，是某一天突然发生的事。\n像生日，像毕业，像门框上突然高出的一条线。",
            "the complete recording-script subtitle appears after Start and its intended delay")) yield break;
        Capture("00-after-start-click");
        menu.BeginGame();
        yield return Keys(0.15f);
        if (!Check(Player.ControlsEnabled && !menu.Visible, "repeated Start activation cannot restart or relock the prologue")) yield break;

        SceneManager.LoadScene("01_Kindergarten");
        yield return Keys(1.3f);
        if (!Check(!StorySceneDirector.Instance.NarrationEnabled && GameObject.Find("Opening Voice") == null,
            "the kindergarten does not revive its retired explanation or leak prologue voice playback")) yield break;
        if (!Check(library.kindergartenMusic != titleMusic, "kindergarten and title music remain distinct recordings")) yield break;
        if (!CheckSceneMusic(library.kindergartenMusic,
            "leaving the title for kindergarten plays its own loop instead of the main-menu melody")) yield break;
        SceneManager.LoadScene("02_Interlude_Firsts");
        yield return Keys(1.3f);
        if (!CheckSceneMusic(library.journeyMusic,
            "the restored firsts interlude plays the journey music through its runtime audio controller")) yield break;
        SceneManager.LoadScene("03_Stage");
        yield return Keys(1.3f);
        if (!Check(library.journeyMusic != titleMusic, "the journey and title music remain distinct recordings")) yield break;
        if (!CheckSceneMusic(library.journeyMusic,
            "the stage selects the journey loop rather than inheriting the title track")) yield break;

        SceneManager.LoadScene("00_Prologue");
        yield return Keys(1.3f);
        menu = FindAnyObjectByType<MainMenuController>();
        if (!CheckSceneMusic(titleMusic, "re-entering the title replaces the stage music with the main melody")) yield break;
        yield return Keys(0.15f, Key.Enter);
        yield return Keys(0.9f);
        Check(menu.HasStarted && Player.ControlsEnabled && !menu.Visible, "Enter also starts the delivered menu after a fresh key press");
    }

    private bool CheckSceneMusic(AudioClip expected, string message)
    {
        var audio = FindAnyObjectByType<SceneAudio>();
        return Check(expected != null && audio != null && audio.MusicClip == expected && audio.IsMusicPlaying,
            message + " (clip=" + audio?.MusicClip?.name + ")");
    }

    private bool CheckPauseArtwork(PauseMenuController pause)
    {
        var artwork = GameObject.Find("Delivered Pause Menu")?.GetComponent<UnityEngine.UI.Image>();
        var sprite = artwork == null ? null : artwork.sprite;
        var path = sprite == null ? "" : UnityEditor.AssetDatabase.GetAssetPath(sprite.texture);
        if (!Check(artwork != null && artwork.isActiveAndEnabled && artwork.color.a > 0.95f &&
            path == "Assets/Sprites/暂停UI/暂停菜单界面.png" && artwork.preserveAspect &&
            sprite.rect.x == 0f && sprite.rect.y == 0f &&
            sprite.rect.width == sprite.texture.width && sprite.rect.height == sprite.texture.height &&
            Mathf.Abs(artwork.rectTransform.rect.width / artwork.rectTransform.rect.height - 1.5f) < 0.005f,
            "the pause menu visibly uses the complete delivered 1536-by-1024 composition with its original proportions")) return false;
        if (!CheckPauseHotspot(artwork, pause.ContinueButton, new Vector2(768f, 562f), "RESUME")) return false;
        if (!CheckPauseHotspot(artwork, pause.QuitButton, new Vector2(768f, 728f), "QUIT")) return false;
        var menu = GameObject.Find("Pause Menu");
        var buttons = menu == null ? null : menu.GetComponentsInChildren<UnityEngine.UI.Button>();
        return Check(buttons != null && buttons.Length == 3 && pause.ContinueButton.interactable &&
            pause.MainMenuButton != null && pause.MainMenuButton.interactable &&
            pause.QuitButton != null && pause.QuitButton.interactable,
            "the pause view retains exactly three working actions: Continue, title and Quit");
    }

    private bool CheckPauseHotspot(UnityEngine.UI.Image artwork, UnityEngine.UI.Button button,
        Vector2 sourcePoint, string caption)
    {
        if (!Check(button != null && button.interactable && button.targetGraphic != null &&
            button.targetGraphic.raycastTarget && button.colors.normalColor.a <= 0.01f,
            "the printed " + caption + " button has a transparent interactive region")) return false;
        var buttonRect = button.GetComponent<RectTransform>();
        var center = artwork.rectTransform.InverseTransformPoint(buttonRect.TransformPoint(buttonRect.rect.center));
        var expected = PauseArtworkLocalPoint(artwork, sourcePoint);
        var offset = Vector2.Distance(new Vector2(center.x, center.y), expected);
        return Check(offset <= 4f && RectTransformUtility.RectangleContainsScreenPoint(buttonRect,
            PauseArtworkScreenPoint(artwork, sourcePoint), null),
            "the " + caption + " click region covers its printed center with at most four canvas pixels of offset (actual=" +
            offset.ToString("F2") + ")");
    }

    private static Vector2 PauseArtworkLocalPoint(UnityEngine.UI.Image artwork, Vector2 sourcePoint)
    {
        var rect = artwork.rectTransform.rect;
        return new Vector2(rect.xMin + sourcePoint.x / 1536f * rect.width,
            rect.yMax - sourcePoint.y / 1024f * rect.height);
    }

    private static Vector2 PauseArtworkScreenPoint(UnityEngine.UI.Image artwork, Vector2 sourcePoint)
    {
        return RectTransformUtility.WorldToScreenPoint(null,
            artwork.rectTransform.TransformPoint(PauseArtworkLocalPoint(artwork, sourcePoint)));
    }

    private IEnumerator ReviewReportedPresentation()
    {
        phase = "parent poses in their real gameplay situations";
        SceneManager.LoadScene("01_Kindergarten");
        yield return Keys(1.4f);
        var parents = FindAnyObjectByType<ParentVisualPresentation>();
        if (!Check(parents != null && parents.PoseName == "Idle" && Player.CharacterRenderer.enabled,
            "the parent starts with a complete idle pose beside the independently rendered child")) yield break;
        Capture("01-parent-full-idle");
        yield return Keys(0.8f, Key.E);
        if (!Check(parents.PoseName == "Hold" && parents.CompositeIncludesPlayer && !Player.CharacterRenderer.enabled,
            "taking the hand uses the delivered composite pose without drawing a duplicate child")) yield break;
        Capture("01-parent-holding-action");
        yield return Keys(0.3f);
        if (!Check(parents.PoseName == "Wait" && Player.CharacterRenderer.enabled,
            "letting go restores the playable child and shows the parents waiting")) yield break;
        Capture("01-parent-waiting-action");
        SceneManager.LoadScene("03_Stage");
        yield return Keys(1.4f);
        parents = FindAnyObjectByType<ParentVisualPresentation>();
        yield return Keys(0.4f, Key.E);
        if (!Check(parents != null && parents.PoseName == "Shoulder" && Player.CharacterRenderer.enabled,
            "the backstage support moment uses the shoulder gesture while keeping the teenager visible")) yield break;
        Capture("03-parent-shoulder-action");
        yield return Keys(0.3f);
        phase = "the workbench's painted feet reach the research floor";
        SceneManager.LoadScene("05_Research");
        yield return Keys(1.4f);
        yield return Walk(0f);
        var bench = GameObject.Find("Return Workbench");
        SpriteRenderer benchArt = null;
        foreach (var candidate in bench.GetComponentsInChildren<SpriteRenderer>())
            if (candidate.enabled && candidate.sprite != null) benchArt = candidate;
        if (!Check(benchArt != null && Mathf.Abs(benchArt.bounds.min.y + 2.72f) < 0.06f,
            "the workbench visible image rests on the floor instead of its transparent padding")) yield break;
        Capture("05-workbench-grounded");

        var research = FindAnyObjectByType<ResearchExpedition>();
        yield return Walk(research.Crate.transform.position.x - 0.9f);
        if (!Check(ResearchDiagramsHidden(), "the research crate and pressure plate have no prototype lines or draft labels")) yield break;
        yield return SaveMemoryReviewCapture("05-final-research-crate-and-plate");
        if (finished) yield break;
        yield return Keys(0.2f, Key.E);
        if (!Check(Hand.CurrentTarget == research.Crate, "the delivered research crate remains usable after presentation cleanup")) yield break;
        yield return Walk(5.3f, true);
        yield return Keys(0.35f);
        if (!Check(research.GateOpen && ResearchDiagramsHidden(), "the crate opens the pressure gate without restoring retired guides")) yield break;
        yield return SaveMemoryReviewCapture("05-final-research-plate-open");
        if (finished) yield break;

        phase = "shortened black passages retain the invisible age changes";
        JourneyChoices.RememberWorkshop("breath", "jump", true);
        JourneyChoices.RememberLearnedHabit(new LearnedHabitSnapshot(LearnedHabitKind.Jump, 0f, 15f, "", 1));
        SceneManager.LoadScene("06_FinalWalk");
        yield return Keys(1.4f);
        foreach (var center in FinalPassageLayout.Centers)
        {
            yield return Walk(center - 0.5f);
            yield return Keys(0.3f);
            if (!Check(PassageAlpha() >= 0.99f && Player.ControlsEnabled,
                "the shortened curtain fully covers the screen before its center at " + center)) yield break;
            yield return Walk(center + 0.5f);
            yield return Keys(0.3f);
            if (!Check(PassageAlpha() >= 0.99f,
                "the new age remains hidden just after the curtain center at " + center)) yield break;
        }
        var curtain = GameObject.Find("Memory Black Passage 1").GetComponent<SpriteRenderer>();
        var viewportWidth = Camera.main.orthographicSize * Camera.main.aspect * 2f;
        if (!Check(curtain.bounds.size.x < viewportWidth + 2f && curtain.bounds.size.x > viewportWidth + 1f,
            "the black passage is shorter while retaining a margin beyond the full camera width")) yield break;
        var recipient = EmotionalJourney.Target("young-presenter");
        var ending = FindAnyObjectByType<ReleaseEndingGoal>();
        yield return Walk(recipient.transform.position.x - 1.8f);
        for (var i = 0; i < 130 && !ending.HasEnteredDoor; i++) yield return Keys(0.1f);
        var farewell = recipient.GetComponent<FinalFarewellPresentation>();
        if (!Check(ending.HasEnteredDoor && farewell.DoorEntryComplete && !farewell.CharacterRenderer.enabled,
            "the next child finishes its learned action and goes fully inside the door")) yield break;
        var door = GameObject.Find("Unknown Door");
        yield return Walk(door.transform.position.x);
        yield return Keys(0.25f);
        var inFront = true;
        foreach (var part in door.GetComponentsInChildren<SpriteRenderer>())
            if (part.enabled) inFront &= part.sortingLayerID == Player.CharacterRenderer.sortingLayerID &&
                part.sortingOrder < Player.CharacterRenderer.sortingOrder;
        if (!Check(inFront && !farewell.CharacterRenderer.enabled && Player.ControlsEnabled,
            "the adult walks in front of the closed door while the departed child stays hidden")) yield break;
        Capture("06-adult-in-front-of-closed-door");
        yield return Walk(ending.OnwardPositionX + 0.2f);
    }
}
#endif
