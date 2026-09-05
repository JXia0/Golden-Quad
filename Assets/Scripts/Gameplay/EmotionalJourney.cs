using UnityEngine;
using UnityEngine.UI;

namespace LetGo
{
    /// <summary>Upgrades the saved jam scenes at play time, preserving artists' saved layouts.</summary>
    public static class EmotionalJourney
    {
        public static void Install(StorySceneDirector director)
        {
            var scene = director.gameObject.scene.name;
            if (scene != "01_Kindergarten" && scene != "03_Stage" && scene != "05_Research" && scene != "06_FinalWalk") return;
            if (director.GetComponent<JourneyVisuals>() != null) return;
            director.NarrationEnabled = false;
            var visuals = director.gameObject.AddComponent<JourneyVisuals>();
            visuals.Initialize(director);
            // Old explanatory labels described mechanics that this version replaces.
            foreach (var label in Object.FindObjectsByType<TextMesh>())
                if (label.gameObject.scene == director.gameObject.scene && label.name.StartsWith("Label -")) label.gameObject.SetActive(false);
            foreach (var hud in Object.FindObjectsByType<CourageHUD>())
            {
                hud.enabled = false;
                hud.GetComponent<Image>().enabled = false;
                var background = hud.transform.parent.GetComponent<Image>();
                if (background != null) background.enabled = false;
            }
            switch (scene)
            {
                case "01_Kindergarten": director.gameObject.AddComponent<KindergartenChapter>().Initialize(director, visuals); break;
                case "03_Stage": director.gameObject.AddComponent<StagePerformance>().Initialize(director, visuals); break;
                case "05_Research": director.gameObject.AddComponent<ResearchExpedition>().Initialize(director, visuals); break;
            }
        }

        public static HoldTarget Target(string id)
        {
            foreach (var target in Object.FindObjectsByType<HoldTarget>())
                if (target.TargetId == id) return target;
            return null;
        }

        public static Transform Named(string name) => GameObject.Find(name)?.transform;
    }
}
