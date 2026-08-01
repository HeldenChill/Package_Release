using UnityEngine;
using global::Utilities;

namespace Hung.Utilities.Input
{
    using System;
    using Hung.DesignPattern;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using Hung.Utilities.Timer;

    public class PlayerInput : Singleton<PlayerInput>
    {
        public const float RAYCAST_DISTANCE = 500f;
        public const float DOUBLE_CLICK_TIME = 0.3f; // Time in seconds to consider a double click
        public static Action<Ray, RaycastHit, InputAction.CallbackContext> _OnRaycastClick;
        public static Action<Ray, RaycastHit> _OnRaycastDoubleClick;
        public static Action<Ray, RaycastHit, InputAction.CallbackContext> _OnRaycastClickHold;
        public static Action<Ray, RaycastHit> _OnRaycastHold;
        protected PlayerInputActions inputActions;
        protected Ray clickedRay;
        [SerializeField]
        protected Camera _camera;
        public Vector2 MousePosition => inputActions.Player.TouchPosition.ReadValue<Vector2>();
        public Vector2 MouseWorldPosition => _camera.ScreenToWorldPoint(MousePosition);
        protected STimer doubleClickTimer;
        protected bool isMouseHold = false;
        private void Awake()
        {
            inputActions = new PlayerInputActions();
            doubleClickTimer = TimerManager.Ins.PopSTimer();
            inputActions.Player.Enable();
            inputActions.Player.TouchPress.performed += UpdateRaycastClick;
            inputActions.Player.TouchHold.performed += UpdateRaycastClickHold;
            inputActions.Player.TouchHold.canceled += UpdateRaycastClickHold;
        }
        protected void UpdateRaycastClick(InputAction.CallbackContext context)
        {
            //DevLog.Log(DevId.Hung, "Touch");
            Vector2 clickPosition = inputActions.Player.TouchPosition.ReadValue<Vector2>();
            bool isClickOnUI = UTILITIES.IsPointerOverUIObject(clickPosition);
            if (isClickOnUI)
            {
                return;
            }
            clickedRay = _camera.ScreenPointToRay(clickPosition);
            //DevLog.Log(DevId.Hung, $"RAY: Origin - {clickedRay.origin}, Direction - {clickedRay.direction}");
            if (Physics.Raycast(clickedRay, out RaycastHit hitInfo, RAYCAST_DISTANCE))
            {
                _OnRaycastClick?.Invoke(clickedRay, hitInfo, context);
            }
            if (doubleClickTimer.IsStart)
            {
                _OnRaycastDoubleClick?.Invoke(clickedRay, hitInfo);
                doubleClickTimer.Stop();
            }
            else
            {
                doubleClickTimer.Start(DOUBLE_CLICK_TIME);
            }

        }

        protected void UpdateRaycastClickHold(InputAction.CallbackContext context)
        {
            Vector2 clickPosition = inputActions.Player.TouchPosition.ReadValue<Vector2>();
            bool isClickOnUI = UTILITIES.IsPointerOverUIObject(clickPosition);
            if (isClickOnUI)
            {
                return;
            }
            clickedRay = _camera.ScreenPointToRay(clickPosition);
            Physics.Raycast(clickedRay, out RaycastHit hitInfo, RAYCAST_DISTANCE);
            _OnRaycastClickHold?.Invoke(clickedRay, hitInfo, context);
            if( context.performed)
            {
                isMouseHold = true;
            }
            else if (context.canceled)
            {
                isMouseHold = false;
            }
        }
        protected void UpdateRaycastHold()
        {
            Vector2 clickPosition = inputActions.Player.TouchPosition.ReadValue<Vector2>();
            clickedRay = _camera.ScreenPointToRay(clickPosition);
            Physics.Raycast(clickedRay, out RaycastHit hitInfo, RAYCAST_DISTANCE);
            _OnRaycastHold?.Invoke(clickedRay, hitInfo);         
        }
        void Update()
        {
            if (isMouseHold)
            {
                UpdateRaycastHold();
            }
        }
        private void OnDestroy()
        {
            inputActions.Player.TouchPress.performed -= UpdateRaycastClick;
            inputActions.Player.TouchHold.performed -= UpdateRaycastClickHold;
            inputActions.Player.TouchHold.canceled -= UpdateRaycastClickHold;
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(clickedRay.origin, clickedRay.origin + clickedRay.direction * RAYCAST_DISTANCE);
        }
    }
}
