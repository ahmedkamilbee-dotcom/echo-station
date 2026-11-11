using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.Player
{
    /// <summary>
    /// First-person controller for Ali. Slow, heavy, quiet: no jump, no sprint.
    /// Ali never speaks, so the body is the only voice: breathing and footsteps.
    /// Requires a CharacterController on the same GameObject and a child camera.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class AliController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Transform cameraPivot;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 1.35f;      // slow on purpose
        [SerializeField] float crouchSpeed = 0.7f;
        [SerializeField] float gravity = -12f;
        [SerializeField] float standHeight = 1.75f;
        [SerializeField] float crouchHeight = 1.0f;

        [Header("Look")]
        [SerializeField] float mouseSensitivity = 0.08f;
        [SerializeField] float maxPitch = 80f;
        [SerializeField] float lookSmoothing = 12f;    // a bit of weight to the head

        [Header("Head bob")]
        [SerializeField] float bobAmplitude = 0.025f;
        [SerializeField] float bobFrequency = 1.6f;

        [Header("Footsteps")]
        [SerializeField] AudioSource footstepSource;
        [SerializeField] AudioClip[] footstepClips;
        [SerializeField] float stepInterval = 0.62f;

        /// <summary>When false, input is ignored (used by scripted moments).</summary>
        public bool InputEnabled { get; set; } = true;
        public bool IsMoving { get; private set; }
        public bool IsCrouching { get; private set; }
        /// <summary>Set by HidingSpot while Ali is crouched inside a hiding place.</summary>
        public bool IsHidden { get; set; }
        /// <summary>Set by BreathHold while the player holds Ali's breath.</summary>
        public bool BreathHeld { get; set; }

        CharacterController controller;
        float verticalVelocity;
        float yaw, pitch, targetYaw, targetPitch;
        float bobTimer, bobWeight, stepTimer;
        Vector3 pivotBaseLocal;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = standHeight;
            if (cameraPivot == null && Camera.main != null) cameraPivot = Camera.main.transform;
            pivotBaseLocal = cameraPivot != null ? cameraPivot.localPosition : Vector3.up * 1.6f;
            yaw = targetYaw = transform.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            Vector2 move = Vector2.zero;
            if (InputEnabled)
            {
                if (kb.wKey.isPressed) move.y += 1;
                if (kb.sKey.isPressed) move.y -= 1;
                if (kb.dKey.isPressed) move.x += 1;
                if (kb.aKey.isPressed) move.x -= 1;
                bool wantCrouch = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;
                // Can't stand up under a desk or the grate.
                bool blocked = Physics.SphereCast(transform.position + Vector3.up * controller.radius, controller.radius * 0.95f,
                    Vector3.up, out _, standHeight - controller.radius * 2f, ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore);
                IsCrouching = wantCrouch || (IsCrouching && blocked);

                Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
                targetYaw += delta.x;
                targetPitch = Mathf.Clamp(targetPitch - delta.y, -maxPitch, maxPitch);
            }

            // Smoothed look gives the head a little weight.
            float t = 1f - Mathf.Exp(-lookSmoothing * Time.deltaTime);
            yaw = Mathf.Lerp(yaw, targetYaw, t);
            pitch = Mathf.Lerp(pitch, targetPitch, t);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // Crouch height (used to hide under desks and the grate).
            float targetHeight = IsCrouching ? crouchHeight : standHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, 8f * Time.deltaTime);
            controller.center = Vector3.up * controller.height * 0.5f;

            // Movement
            move = Vector2.ClampMagnitude(move, 1f);
            float speed = IsCrouching ? crouchSpeed : walkSpeed;
            Vector3 dir = (transform.right * move.x + transform.forward * move.y) * speed;
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            verticalVelocity += gravity * Time.deltaTime;
            dir.y = verticalVelocity;
            controller.Move(dir * Time.deltaTime);

            IsMoving = move.sqrMagnitude > 0.01f && controller.isGrounded;
            HeadBobAndSteps(speed);
        }

        void HeadBobAndSteps(float speed)
        {
            if (cameraPivot == null) return;
            float eyeHeight = controller.height - 0.15f;
            Vector3 basePos = new Vector3(pivotBaseLocal.x, eyeHeight, pivotBaseLocal.z);

            if (IsMoving)
            {
                bobTimer = Mathf.Repeat(bobTimer + Time.deltaTime * bobFrequency * (speed / walkSpeed) * Mathf.PI * 2f, Mathf.PI * 2f);
                stepTimer += Time.deltaTime * (speed / walkSpeed);
                if (stepTimer >= stepInterval) { stepTimer = 0f; PlayStep(); }
            }
            bobWeight = Mathf.MoveTowards(bobWeight, IsMoving ? 1f : 0f, 3f * Time.deltaTime);
            float bob = Mathf.Sin(bobTimer) * bobAmplitude * bobWeight;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, basePos + Vector3.up * bob, 10f * Time.deltaTime);
        }

        void PlayStep()
        {
            if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;
            footstepSource.pitch = Random.Range(0.92f, 1.05f);
            footstepSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], IsCrouching ? 0.35f : 0.7f);
        }

        /// <summary>Instantly move Ali (used by the looping corridor). Keeps look direction relative to the turn.</summary>
        public void Teleport(Vector3 position, float yawDelta)
        {
            controller.enabled = false;
            transform.position = position;
            yaw += yawDelta; targetYaw += yawDelta;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            controller.enabled = true;
        }
    }
}
