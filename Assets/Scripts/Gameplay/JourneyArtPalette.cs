using UnityEngine;

namespace LetGo
{
    public sealed class JourneyArtPalette : ScriptableObject
    {
        [System.Serializable] public struct Entry { public string id; public Sprite sprite; }
        public Entry[] entries;
        public Sprite Find(string id)
        {
            if (entries == null) return null;
            foreach (var entry in entries) if (entry.id == id && entry.sprite != null) return entry.sprite;
            return null;
        }
    }
}
