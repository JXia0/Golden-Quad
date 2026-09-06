using UnityEngine;

namespace LetGo
{
    /// <summary>Presentation-only parent poses; the existing hold and escort scripts remain authoritative.</summary>
    [DefaultExecutionOrder(125)]
    public sealed class ParentVisualPresentation : MonoBehaviour
    {
        private const float GroundY = -2.72f;
        private const float ParentHeight = 1.68f;

        private HoldTarget target;
        private HandConnection hand;
        private PlayerController2D player;
        private SpriteRenderer playerVisual;
        private SpriteRenderer parentVisual;
        private Sprite idle;
        private Sprite hold;
        private Sprite shoulder;
        private Sprite wait;
        private bool everHeld;
        private bool hidPlayer;
        private ParentPose pose = (ParentPose)(-1);

        public string PoseName => pose.ToString();
        public bool CompositeIncludesPlayer => pose == ParentPose.Hold;

        private enum ParentPose { Idle, Hold, Shoulder, Wait }

        public void Configure(HoldTarget parent, SpriteRenderer visible, HandConnection connection,
            PlayerController2D playerController, SpriteRenderer playerRenderer, JourneyArtPalette palette)
        {
            target = parent;
            parentVisual = visible;
            hand = connection;
            player = playerController;
            playerVisual = playerRenderer;
            if (target == null || parentVisual == null) { enabled = false; return; }

            idle = Trim(parentVisual.sprite, "char_parent_idle_trimmed", 2172f, 724f,
                new Rect(111f, 25f, 338f, 687f), new Vector2(0.5f, 0f));
            hold = Trim(palette?.Find("char_parent_hold"), "char_parent_hold_trimmed", 1312f, 1199f,
                new Rect(94f, 5f, 1197f, 1160f), new Vector2(0.13f, 0f));
            shoulder = Trim(palette?.Find("char_parent_shoulder"), "char_parent_shoulder_trimmed", 1222f, 1287f,
                new Rect(238f, 14f, 785f, 1258f), new Vector2(0.5f, 0f));
            wait = Trim(palette?.Find("char_parent_wait"), "char_parent_wait_trimmed", 1222f, 1287f,
                new Rect(204f, 17f, 787f, 1259f), new Vector2(0.5f, 0f));
            ApplyPose(ParentPose.Idle);
        }

        private void LateUpdate()
        {
            if (target == null || parentVisual == null || player == null) return;
            var held = hand != null && hand.CurrentTarget == target;
            if (held) everHeld = true;
            var kindergarten = target.TargetId == "parent";
            var next = held ? (kindergarten ? ParentPose.Hold : ParentPose.Shoulder)
                : everHeld ? ParentPose.Wait : ParentPose.Idle;
            ApplyPose(next);

            if (next == ParentPose.Hold && kindergarten)
            {
                HidePlayer();
                parentVisual.flipX = playerVisual != null && playerVisual.flipX;
                parentVisual.transform.position = new Vector3(player.transform.position.x, GroundY,
                    parentVisual.transform.position.z);
            }
            else
            {
                RestorePlayer();
                parentVisual.transform.position = new Vector3(target.transform.position.x, GroundY,
                    parentVisual.transform.position.z);
            }
        }

        private void ApplyPose(ParentPose next)
        {
            if (pose == next) return;
            pose = next;
            var sprite = next switch
            {
                ParentPose.Hold => hold != null ? hold : idle,
                ParentPose.Shoulder => shoulder != null ? shoulder : idle,
                ParentPose.Wait => wait != null ? wait : idle,
                _ => idle
            };
            if (sprite == null) return;
            parentVisual.sprite = sprite;
            FitHeight(parentVisual, ParentHeight);
        }

        private void HidePlayer()
        {
            if (playerVisual == null || hidPlayer) return;
            hidPlayer = playerVisual.enabled;
            if (hidPlayer) playerVisual.enabled = false;
        }

        private void RestorePlayer()
        {
            if (!hidPlayer || playerVisual == null) return;
            playerVisual.enabled = true;
            hidPlayer = false;
        }

        private static void FitHeight(SpriteRenderer renderer, float height)
        {
            if (renderer == null || renderer.sprite == null) return;
            var parentScale = renderer.transform.parent == null ? Vector3.one : renderer.transform.parent.lossyScale;
            var factor = height / renderer.sprite.bounds.size.y;
            renderer.transform.localScale = new Vector3(
                factor / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                factor / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
        }

        private static Sprite Trim(Sprite source, string name, float sourceWidth, float sourceHeight,
            Rect sourceRect, Vector2 pivot)
        {
            if (source == null || source.texture == null) return null;
            var texture = source.texture;
            var rect = new Rect(sourceRect.x * texture.width / sourceWidth,
                sourceRect.y * texture.height / sourceHeight,
                sourceRect.width * texture.width / sourceWidth,
                sourceRect.height * texture.height / sourceHeight);
            rect.x = Mathf.Clamp(rect.x, 0f, texture.width - 1f);
            rect.y = Mathf.Clamp(rect.y, 0f, texture.height - 1f);
            rect.width = Mathf.Clamp(rect.width, 1f, texture.width - rect.x);
            rect.height = Mathf.Clamp(rect.height, 1f, texture.height - rect.y);
            var sprite = Sprite.Create(texture, rect, pivot, 100f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            return sprite;
        }

        private void OnDisable() => RestorePlayer();

        private void OnDestroy()
        {
            RestorePlayer();
            if (idle != null) Destroy(idle);
            if (hold != null) Destroy(hold);
            if (shoulder != null) Destroy(shoulder);
            if (wait != null) Destroy(wait);
        }
    }
}
