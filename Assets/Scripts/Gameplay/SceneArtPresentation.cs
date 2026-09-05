using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LetGo
{
    // Presentation only: reads existing gameplay state, never changes interactions or physics.
    [DefaultExecutionOrder(100)]
    public sealed class SceneArtPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer vignette;
        [SerializeField] private Vector2 backdropLimits;
        private Camera view;
        private HandConnection hand;
        private EmotionalEnvironment environment;
        private SpriteRenderer playerArt;
        private Collider2D playerBody;
        private PlayerController2D player;
        private string visibleAge;
        private readonly List<(SpriteRenderer source, SpriteRenderer visible)> actors = new();

        public void Configure(SpriteRenderer overlay, Vector2 limits) { vignette = overlay; backdropLimits = limits; }

        private IEnumerator Start()
        {
            view = GetComponent<Camera>();
            hand = FindAnyObjectByType<HandConnection>();
            environment = FindAnyObjectByType<EmotionalEnvironment>();
            player = FindAnyObjectByType<PlayerController2D>();
            if (player != null) { playerArt = player.CharacterRenderer; playerBody = player.GetComponent<Collider2D>(); }
            // The chapters create their visual props in Start.
            yield return null;
            foreach (var renderer in FindObjectsByType<SpriteRenderer>())
                if (renderer.name == "Grounded Visual")
                    actors.Add((renderer.transform.parent.GetComponent<SpriteRenderer>(), renderer));
            var bag = GameObject.Find("Bag from home")?.GetComponent<SpriteRenderer>();
            if (bag != null && bag.sprite != null)
            {
                var visible = new GameObject("Bag Visual", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
                visible.sprite = bag.sprite;
                visible.sharedMaterial = bag.sharedMaterial;
                visible.sortingOrder = bag.sortingOrder;
                visible.transform.position = bag.transform.position + Vector3.up * (-2.72f - bag.bounds.min.y);
                visible.transform.localScale = bag.transform.lossyScale;
                visible.transform.SetParent(bag.transform, true);
                actors.Add((bag, visible));
                bag.enabled = false;
            }
            var lamp = GameObject.Find("Working lamp")?.GetComponent<SpriteRenderer>();
            var desk = GameObject.Find("Research Desk")?.transform.Find("Aligned Visual")?.GetComponent<SpriteRenderer>();
            if (lamp != null && desk != null)
            {
                lamp.transform.position = new Vector3(desk.bounds.center.x - 1.2f,
                    desk.bounds.max.y + lamp.bounds.extents.y - 0.03f, lamp.transform.position.z);
                var extraLamp = GameObject.Find("Desk Lamp")?.GetComponent<SpriteRenderer>();
                if (extraLamp != null) extraLamp.enabled = false;
            }
            var seat = GameObject.Find("A familiar empty seat")?.GetComponent<SpriteRenderer>();
            if (seat != null)
            {
                seat.transform.position += Vector3.up * (-1.35f - seat.bounds.min.y);
                seat.sortingOrder = -2;
                var scarf = GameObject.Find("Scarf on the empty seat")?.GetComponent<SpriteRenderer>();
                if (scarf != null)
                {
                    scarf.transform.position = seat.bounds.center + Vector3.up * 0.12f;
                    // Do not show a square as an undelivered scarf.
                    if (scarf.sprite != null && scarf.sprite.texture == Texture2D.whiteTexture) scarf.enabled = false;
                }
            }
        }

        private void LateUpdate()
        {
            if (playerArt != null && playerArt.sprite != null)
            {
                var spriteName = playerArt.sprite.name;
                var age = spriteName.StartsWith("char_adult") ? "adult" : spriteName.StartsWith("char_teen") ? "teen" : "child";
                if (age != visibleAge)
                {
                    visibleAge = age;
                    if (age != "child")
                    {
                        var height = age == "adult" ? 2.35f : 2.1f;
                        var scale = Mathf.Abs(playerArt.transform.localScale.y) * height / playerArt.bounds.size.y;
                        player.SetCharacterScale(Vector3.one * scale);
                    }
                }
            }
            if (playerArt != null && playerBody != null)
                playerArt.transform.position += Vector3.up * (playerBody.bounds.min.y - playerArt.bounds.min.y);
            foreach (var pair in actors)
                if (pair.source != null && pair.visible != null)
                {
                    pair.visible.color = pair.source.color;
                    pair.visible.flipX = pair.source.flipX;
                    pair.visible.flipY = pair.source.flipY;
                }
            if (vignette == null || view == null) return;
            var halfWidth = view.orthographicSize * view.aspect;
            var cameraPosition = view.transform.position;
            cameraPosition.x = Mathf.Clamp(cameraPosition.x, backdropLimits.x + halfWidth, backdropLimits.y - halfWidth);
            view.transform.position = cameraPosition;
            var size = vignette.sprite.bounds.size;
            vignette.transform.localScale = new Vector3(2 * view.orthographicSize * view.aspect / size.x,
                2 * view.orthographicSize / size.y, 1);
            var completed = StorySceneDirector.Instance != null && StorySceneDirector.Instance.ObjectivesComplete;
            var tension = hand == null || environment == null || completed ? 0 : 1 - hand.Stability01;
            var color = vignette.color;
            color.a = Mathf.Lerp(color.a, 0.2f * tension, Time.deltaTime * 3);
            vignette.color = color;
        }
    }
}
