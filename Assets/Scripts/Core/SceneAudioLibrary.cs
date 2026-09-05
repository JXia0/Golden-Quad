using UnityEngine;

namespace LetGo
{
    public sealed class SceneAudioLibrary : ScriptableObject
    {
        public AudioClip footstepOne;
        public AudioClip footstepTwo;
        public AudioClip interact;
        public AudioClip itemMove;
        public AudioClip objectiveLight;
        public AudioClip doorOpen;
        public AudioClip handRelease;
        public AudioClip heartbeat;
        public AudioClip calmBreath;
        public AudioClip applause;
        public AudioClip paperRustle;
        public AudioClip finalLight;
        public AudioClip ambience;
        public void Apply(SceneAudio target)
        {
            if (target == null) return;
            target.SetEffects(footstepOne, footstepTwo, interact, itemMove, objectiveLight, doorOpen,
                handRelease, heartbeat, calmBreath, applause, paperRustle, finalLight);
            target.SetFallbackAmbience(ambience);
        }
    }
}
