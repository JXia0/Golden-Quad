using UnityEngine;

namespace LetGo
{
    public sealed class SceneAudio : MonoBehaviour
    {
        public static SceneAudio Instance { get; private set; }

        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource tensionSource;
        [SerializeField] private AudioClip footstepOne;
        [SerializeField] private AudioClip footstepTwo;
        [SerializeField] private AudioClip interact;
        [SerializeField] private AudioClip itemMove;
        [SerializeField] private AudioClip objectiveLight;
        [SerializeField] private AudioClip doorOpen;
        [SerializeField] private AudioClip handRelease;
        [SerializeField] private AudioClip heartbeat;
        [SerializeField] private AudioClip calmBreath;
        [SerializeField] private AudioClip applause;
        [SerializeField] private AudioClip paperRustle;
        [SerializeField] private AudioClip finalLight;
        [SerializeField] private AudioClip ambience;
        [SerializeField] private AudioClip music;
        private bool alternateFootstep;
        public AudioClip AmbienceClip => ambience;
        public bool IsAmbiencePlaying => ambienceSource != null && ambienceSource.isPlaying && ambienceSource.loop;

        public void SetFallbackAmbience(AudioClip clip)
        {
            if (ambience == null && clip != null) ambience = clip;
        }

        public void ConfigureSources(AudioSource effects, AudioSource ambienceLoop, AudioSource musicLoop,
            AudioSource tensionLoop)
        {
            effectsSource = effects;
            ambienceSource = ambienceLoop;
            musicSource = musicLoop;
            tensionSource = tensionLoop;
        }

        public void SetClips(AudioClip stepOne, AudioClip stepTwo, AudioClip interaction, AudioClip moveItem,
            AudioClip objective, AudioClip door, AudioClip release, AudioClip heartbeatLoop, AudioClip breath,
            AudioClip audienceApplause, AudioClip paper, AudioClip ending, AudioClip ambienceLoop, AudioClip musicLoop)
        {
            SetEffects(stepOne, stepTwo, interaction, moveItem, objective, door, release, heartbeatLoop, breath,
                audienceApplause, paper, ending);
            ambience = ambienceLoop;
            music = musicLoop;
        }

        public void SetEffects(AudioClip stepOne, AudioClip stepTwo, AudioClip interaction, AudioClip moveItem,
            AudioClip objective, AudioClip door, AudioClip release, AudioClip heartbeatLoop, AudioClip breath,
            AudioClip audienceApplause, AudioClip paper, AudioClip ending)
        {
            footstepOne = stepOne;
            footstepTwo = stepTwo;
            interact = interaction;
            itemMove = moveItem;
            objectiveLight = objective;
            doorOpen = door;
            handRelease = release;
            heartbeat = heartbeatLoop;
            calmBreath = breath;
            applause = audienceApplause;
            paperRustle = paper;
            finalLight = ending;
        }

        private void Awake() => Instance = this;

        private void Start()
        {
            Resources.Load<SceneAudioLibrary>("SceneAudioLibrary")?.Apply(this);
            StartLoop(ambienceSource, ambience, 0.22f);
            StartLoop(musicSource, music, 0.28f);
            StartLoop(tensionSource, heartbeat, 0f);
        }

        public void PlayFootstep()
        {
            alternateFootstep = !alternateFootstep;
            Play(alternateFootstep ? footstepOne : footstepTwo, 0.45f);
        }

        public void PlayInteract() => Play(interact, 0.7f);
        public void PlayItemMove() => Play(itemMove != null ? itemMove : paperRustle, 0.75f);
        public void PlayObjective() => Play(objectiveLight, 0.85f);
        public void PlayDoor() => Play(doorOpen, 0.8f);
        public void PlayRelease() => Play(handRelease, 0.85f);
        public void PlayBreath() => Play(calmBreath, 0.8f);
        public void PlayApplause() => Play(applause, 0.85f);
        public void PlayFinal() => Play(finalLight, 0.9f);

        public void SetTension(float value)
        {
            if (tensionSource != null) tensionSource.volume = Mathf.Lerp(0f, 0.42f, Mathf.Clamp01(value));
        }

        private void Play(AudioClip clip, float volume)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip, volume);
        }

        private static void StartLoop(AudioSource source, AudioClip clip, float volume)
        {
            if (source == null || clip == null) return;
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.volume = volume;
            source.Play();
        }
    }
}
