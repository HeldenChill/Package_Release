using UnityEngine;
using UnityEngine.InputSystem;
using Hung.Base;

namespace Gameplay.Character.BrainSystem
{
    /// <summary>
    /// Brain module that drives BrainData from an InputActionAsset. The asset and the
    /// map/action names are serialized, so this module works for any game's bindings —
    /// it has no dependency on a game-generated PlayerInputActions wrapper.
    /// Enabling/disabling the maps is the game's input service's job, not this module's.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerInput : BrainModule
    {
        [SerializeField] protected InputActionAsset actions;
        [SerializeField] protected string mapName = "Movement";
        [SerializeField] protected string moveAction = "Move";
        [SerializeField] protected string lookAction = "Look";
        [SerializeField] protected string jumpAction = "Jump";
        [SerializeField] protected string crouchAction = "Crouch";
        [SerializeField] protected string runAction = "Run";

        protected InputActionMap map;

        protected void Awake()
        {
            // The game's input service (e.g. InputManager) typically builds its own generated
            // PlayerInputActions wrapper via `new`, which clones a fresh InputActionAsset
            // (InputActionAsset.FromJson under the hood) distinct from whatever asset is dragged
            // into this field in the Inspector. Prefer Locator.Input's shared asset so map
            // enable/disable calls the service makes actually affect the maps we read from.
            if (Locator.Input != null && Locator.Input.Asset != null)
                actions = Locator.Input.Asset;

            map = actions.FindActionMap(mapName, throwIfNotFound: true);

            var move = map.FindAction(moveAction, throwIfNotFound: true);
            move.started += Move;
            move.performed += Move;
            move.canceled += Move;

            map.FindAction(lookAction, throwIfNotFound: true).performed += Look;

            var crouch = map.FindAction(crouchAction, throwIfNotFound: true);
            crouch.started += Crouch;
            crouch.performed += Crouch;
            crouch.canceled += Crouch;

            var run = map.FindAction(runAction, throwIfNotFound: true);
            run.started += Run;
            run.performed += Run;
            run.canceled += Run;

            map.FindAction(jumpAction, throwIfNotFound: true).started += Jump;
        }

        protected void OnDestroy()
        {
            if (map == null) return;

            var move = map.FindAction(moveAction);
            if (move != null)
            {
                move.started -= Move;
                move.performed -= Move;
                move.canceled -= Move;
            }

            var look = map.FindAction(lookAction);
            if (look != null) look.performed -= Look;

            var crouch = map.FindAction(crouchAction);
            if (crouch != null)
            {
                crouch.started -= Crouch;
                crouch.performed -= Crouch;
                crouch.canceled -= Crouch;
            }

            var run = map.FindAction(runAction);
            if (run != null)
            {
                run.started -= Run;
                run.performed -= Run;
                run.canceled -= Run;
            }

            var jump = map.FindAction(jumpAction);
            if (jump != null) jump.started -= Jump;
        }

        protected void Move(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Vector2 moveDirection = context.ReadValue<Vector2>();
                Data.MoveDirection = new Vector3(moveDirection.x, 0, moveDirection.y);
            }

            if (context.canceled)
            {
                Data.MoveDirection = Vector3.zero;
            }
        }
        protected void Look(InputAction.CallbackContext context)
        {
            Data.MouseDeltaPosition = context.ReadValue<Vector2>();
        }
        protected void Jump(InputAction.CallbackContext context)
        {
            Data.Jump.Value = true;
        }
        protected void Crouch(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Data.IsCrouch = true;
            }

            if (context.canceled)
            {
                Data.IsCrouch = false;
            }
        }
        protected void Run(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Data.IsRun = true;
            }

            if (context.canceled)
            {
                Data.IsRun = false;
            }
        }
        public override void StartNavigation()
        {
        }

        public override void StopNavigation()
        {
        }

        public override void UpdateData()
        {

        }
    }
}
