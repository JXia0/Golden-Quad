using System.Collections;
using UnityEngine;

namespace LetGo
{
    public sealed class OpeningNarration : MonoBehaviour
    {
        [TextArea] [SerializeField] private string line;
        [SerializeField] private float delay = 0.8f;
        [SerializeField] private float duration = 5f;
        private StorySceneDirector director;
        private AudioSource voiceSource;
        private SceneAudio duckedAudio;

        public void Configure(string value, float wait = 0.8f, float showFor = 5f)
        {
            line = value;
            delay = wait;
            duration = showFor;
        }

        private IEnumerator Start()
        {
            // Let the director install its narration policy and title screen first.
            yield return null;
            director = StorySceneDirector.Instance;
            if (!CanNarrate()) yield break;

            var menu = director.GetComponent<MainMenuController>();
            while (CanNarrate() && ((menu != null && menu.Visible) || director.IsTransitioning))
            {
                yield return null;
            }
            if (!CanNarrate()) yield break;

            yield return new WaitForSeconds(Mathf.Max(0f, delay));
            if (!CanNarrate() || director.IsTransitioning) yield break;

            var clip = gameObject.scene.name == "00_Prologue"
                ? Resources.Load<AudioClip>("Voice/vo_prologue") : null;
            if (clip != null)
            {
                var voice = new GameObject("Opening Voice");
                voice.transform.SetParent(transform, false);
                voiceSource = voice.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
                voiceSource.loop = false;
                voiceSource.spatialBlend = 0f;
                voiceSource.ignoreListenerPause = false;
                voiceSource.clip = clip;
                var sceneAudio = SceneAudio.Instance;
                if (sceneAudio != null && sceneAudio.gameObject.scene == gameObject.scene)
                {
                    duckedAudio = sceneAudio;
                    duckedAudio.SetOpeningVoiceDucked(true);
                }
                voiceSource.Play();
            }

            director.Say(line, clip == null ? duration : Mathf.Max(duration, clip.length + 0.25f));
            if (voiceSource == null) yield break;
            while (voiceSource != null && (voiceSource.isPlaying || AudioListener.pause))
            {
                if (!CanNarrate() || director.IsTransitioning) break;
                yield return null;
            }
            ReleaseVoice();
        }

        private bool CanNarrate()
        {
            return director != null && director.gameObject.scene == gameObject.scene &&
                director.NarrationEnabled && !string.IsNullOrWhiteSpace(line);
        }

        private void ReleaseVoice()
        {
            if (duckedAudio != null) duckedAudio.SetOpeningVoiceDucked(false);
            duckedAudio = null;
            if (voiceSource == null) return;
            voiceSource.Stop();
            voiceSource.clip = null;
            Destroy(voiceSource.gameObject);
            voiceSource = null;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ReleaseVoice();
        }
    }
}
