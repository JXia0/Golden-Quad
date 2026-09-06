using UnityEngine;
using UnityEngine.SceneManagement;

namespace LetGo
{
    /// <summary>Keeps the playable interludes inside the same audio journey as the authored scenes.</summary>
    public static class InterludeAudioBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Install(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

        private static void Install(Scene scene)
        {
            if (scene.name != "02_Interlude_Firsts" && scene.name != "04_Interlude_Growing") return;
            if (Object.FindAnyObjectByType<SceneAudio>() != null) return;

            var root = new GameObject("Interlude Scene Audio");
            SceneManager.MoveGameObjectToScene(root, scene);
            var effects = Source(root);
            var ambience = Source(root);
            var music = Source(root);
            var tension = Source(root);
            var audio = root.AddComponent<SceneAudio>();
            audio.ConfigureSources(effects, ambience, music, tension);
        }

        private static AudioSource Source(GameObject root)
        {
            var source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
