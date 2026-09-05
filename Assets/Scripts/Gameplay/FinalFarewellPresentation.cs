using UnityEngine;

namespace LetGo
{
    /// <summary>Owns the departing person's animation and doorway silhouette, without moving the gameplay target.</summary>
    [DefaultExecutionOrder(125)]
    public sealed class FinalFarewellPresentation : MonoBehaviour
    {
        private const float GroundY = -2.72f;
        private const float ActorHeight = 1.48f;
        private const float EntrySeconds = 0.65f;
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MovingId = Animator.StringToHash("Moving");
        private static readonly int SelfAnchoringId = Animator.StringToHash("SelfAnchoring");

        private SpriteRenderer[] originalPersonRenderers;
        private SpriteRenderer[] originalDoorRenderers;
        private SpriteRenderer character;
        private SpriteRenderer doorSource;
        private SpriteRenderer doorLeaf;
        private SpriteRenderer doorInterior;
        private SpriteRenderer leftJamb;
        private SpriteRenderer rightJamb;
        private SpriteRenderer lintel;
        private Sprite square;
        private Animator animator;
        private HoldTarget target;
        private Transform doorway;
        private bool configured;
        private bool hasSpeed;
        private bool hasMoving;
        private bool hasSelfAnchoring;
        private bool lookingBack;
        private bool entering;
        private float hop;
        private float entryStarted;
        private Vector3 entryStart;
        private Bounds doorBounds;

        public bool DoorEntryComplete { get; private set; }
        public SpriteRenderer CharacterRenderer => character;

        public void Configure(HoldTarget recipient, Transform door, RuntimeAnimatorController teenController)
        {
            if (configured || recipient == null || door == null) return;
            configured = true;
            target = recipient;
            doorway = door;
            originalPersonRenderers = recipient.GetComponentsInChildren<SpriteRenderer>(true);
            originalDoorRenderers = door.GetComponentsInChildren<SpriteRenderer>(true);
            var originalPerson = BestRenderer(originalPersonRenderers, "Grounded Visual");
            doorSource = BestRenderer(originalDoorRenderers, "Aligned Visual");

            var visual = new GameObject("Character Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(recipient.transform, false);
            character = visual.GetComponent<SpriteRenderer>();
            character.sprite = originalPerson != null ? originalPerson.sprite : null;
            character.color = Color.white;
            character.sortingOrder = 12;
            DisableOriginalPerson();

            animator = recipient.GetComponent<Animator>();
            if (animator == null) animator = recipient.gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = teenController;
            if (teenController != null)
            {
                foreach (var parameter in animator.parameters)
                {
                    if (parameter.nameHash == SpeedId && parameter.type == AnimatorControllerParameterType.Float) hasSpeed = true;
                    if (parameter.nameHash == MovingId && parameter.type == AnimatorControllerParameterType.Bool) hasMoving = true;
                    if (parameter.nameHash == SelfAnchoringId && parameter.type == AnimatorControllerParameterType.Bool) hasSelfAnchoring = true;
                }
                animator.Update(0f);
            }

            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);
            square.name = "farewell_door_shadow";
            doorInterior = DoorPart("Farewell Door Interior", square, new Color(0.015f, 0.02f, 0.028f), 11);
            leftJamb = DoorPart("Farewell Left Jamb", square, new Color(0.055f, 0.066f, 0.081f), 16);
            rightJamb = DoorPart("Farewell Right Jamb", square, new Color(0.055f, 0.066f, 0.081f), 16);
            lintel = DoorPart("Farewell Lintel", square, new Color(0.075f, 0.087f, 0.1f), 16);
            doorLeaf = DoorPart("Farewell Door Leaf", doorSource != null ? doorSource.sprite : square, Color.white, 15);
            if (doorSource != null) doorLeaf.sharedMaterial = doorSource.sharedMaterial;
            SetMotion(0f, false);
            PlaceCharacter(0f);
        }

        public void SetMotion(float horizontalSpeed, bool lookingBack, float hop01 = 0f)
        {
            if (!configured || entering || DoorEntryComplete) return;
            this.lookingBack = lookingBack || horizontalSpeed < -0.05f;
            hop = Mathf.Clamp01(hop01);
            SetAnimationSpeed(Mathf.Abs(horizontalSpeed));
        }

        public void BeginDoorEntry()
        {
            if (!configured || entering || DoorEntryComplete) return;
            entering = true;
            entryStarted = Time.time;
            entryStart = target.transform.position;
            lookingBack = false;
            hop = 0f;
            doorBounds = doorSource != null && doorSource.sprite != null
                ? doorSource.bounds
                : new Bounds(new Vector3(doorway.position.x, GroundY + 1.34f, doorway.position.z), new Vector3(1.18f, 2.68f, 0.01f));
            // The authored door may have extra image padding. Keep the opening rooted on the floor.
            var center = doorBounds.center;
            center.y = GroundY + doorBounds.size.y * 0.5f;
            doorBounds.center = center;
            SetAnimationSpeed(0.65f);
            UpdateDoor(0f);
        }

        private void LateUpdate()
        {
            if (!configured) return;
            // SceneArtPresentation also knows the old Grounded Visual; only this component owns it now.
            DisableOriginalPerson();
            if (DoorEntryComplete)
            {
                character.enabled = false;
                UpdateDoor(1f);
                return;
            }
            var progress = entering ? Mathf.Clamp01((Time.time - entryStarted) / EntrySeconds) : 0f;
            PlaceCharacter(progress);
            if (!entering) return;
            UpdateDoor(progress);
            if (progress < 1f) return;
            DoorEntryComplete = true;
            character.enabled = false;
            SetAnimationSpeed(0f);
        }

        private void PlaceCharacter(float entryProgress)
        {
            if (character == null || character.sprite == null) return;
            // Stay in front while the door starts opening, then pass behind its frame.
            character.sortingOrder = entering && entryProgress < 0.24f ? 17 : 12;
            var depth = entering ? Mathf.SmoothStep(0f, 1f, entryProgress) : 0f;
            var size = ActorHeight * Mathf.Lerp(1f, 0.91f, depth) / Mathf.Max(0.001f, character.sprite.bounds.size.y);
            var parentScale = target.transform.lossyScale;
            character.transform.localScale = new Vector3(size / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                size / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
            character.flipX = lookingBack ^ (parentScale.x < 0f);
            var position = target.transform.position;
            if (entering)
                position.x = Mathf.Lerp(entryStart.x, doorBounds.center.x + doorBounds.extents.x * 0.28f, depth);
            character.transform.position = position;
            var lift = entering ? 0f : Mathf.Sin(hop * Mathf.PI) * 0.36f;
            character.transform.position += Vector3.up * (GroundY + lift - character.bounds.min.y);
            var alpha = entering ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 0.88f, entryProgress)) : 1f;
            character.color = new Color(1f, 1f, 1f, alpha);
            character.enabled = alpha > 0.001f;
        }

        private void UpdateDoor(float progress)
        {
            if (!entering) return;
            foreach (var renderer in originalDoorRenderers)
                if (renderer != null) renderer.enabled = false;
            var width = Mathf.Max(0.5f, doorBounds.size.x);
            var height = Mathf.Max(1f, doorBounds.size.y);
            var x = doorBounds.center.x;
            var y = GroundY + height * 0.5f;
            var z = doorway.position.z;
            SetDoorRect(doorInterior, new Vector3(x, y, z), width, height);
            var jambWidth = width * 0.065f;
            SetDoorRect(leftJamb, new Vector3(x - width * 0.5f, y, z), jambWidth, height);
            SetDoorRect(rightJamb, new Vector3(x + width * 0.5f, y, z), jambWidth, height);
            SetDoorRect(lintel, new Vector3(x, GroundY + height, z), width + jambWidth, height * 0.024f);
            // A fixed hinge, narrowing door leaf and dark interior make the exit read as depth.
            var open = Mathf.Sin(Mathf.PI * Mathf.SmoothStep(0f, 1f, progress));
            var leafWidth = width * Mathf.Lerp(1f, 0.13f, open);
            SetDoorRect(doorLeaf, new Vector3(x + width * 0.5f - leafWidth * 0.5f, y, z), leafWidth, height);
            doorLeaf.color = Color.Lerp(Color.white, new Color(0.57f, 0.62f, 0.7f), open * 0.45f);
        }

        private void SetAnimationSpeed(float speed)
        {
            if (animator == null) return;
            if (hasSpeed) animator.SetFloat(SpeedId, speed);
            if (hasMoving) animator.SetBool(MovingId, speed > 0.05f);
            if (hasSelfAnchoring) animator.SetBool(SelfAnchoringId, false);
        }

        private void DisableOriginalPerson()
        {
            foreach (var renderer in originalPersonRenderers)
                if (renderer != null) renderer.enabled = false;
        }

        private SpriteRenderer DoorPart(string name, Sprite sprite, Color color, int order)
        {
            var part = new GameObject(name, typeof(SpriteRenderer));
            part.transform.SetParent(doorway, false);
            var renderer = part.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return renderer;
        }

        private static void SetDoorRect(SpriteRenderer renderer, Vector3 position, float width, float height)
        {
            if (renderer == null || renderer.sprite == null) return;
            var source = renderer.sprite.bounds.size;
            var parentScale = renderer.transform.parent.lossyScale;
            renderer.transform.localScale = new Vector3(width / Mathf.Max(0.001f, source.x * Mathf.Abs(parentScale.x)),
                height / Mathf.Max(0.001f, source.y * Mathf.Abs(parentScale.y)), 1f);
            renderer.transform.position = position;
            renderer.transform.position += position - renderer.bounds.center;
            renderer.enabled = true;
        }

        private static SpriteRenderer BestRenderer(SpriteRenderer[] renderers, string preferredName)
        {
            SpriteRenderer best = null;
            var bestArea = 0f;
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sprite == null) continue;
                if (renderer.name == preferredName) return renderer;
                var area = renderer.bounds.size.x * renderer.bounds.size.y;
                if (best != null && area <= bestArea) continue;
                bestArea = area;
                best = renderer;
            }
            return best;
        }

        private void OnDestroy()
        {
            if (square != null) Destroy(square);
        }
    }
}
