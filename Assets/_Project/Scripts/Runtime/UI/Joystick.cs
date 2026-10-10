using UnityEngine;
using UnityEngine.InputSystem;

namespace CrystalOutpost.UI
{
    public sealed class Joystick : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform knobRect;
        [SerializeField] private float radius = 120f;
        [SerializeField, Range(0.5f, 1f)] private float maxStartHeight = 0.86f;

        private bool _dragging;
        private Vector2 _startScreen;

        public Vector2 Direction { get; private set; }

        public Vector2? Override { get; set; }

        private void Awake() => baseRect.gameObject.SetActive(false);

        private void OnDisable()
        {
            _dragging = false;
            Direction = Vector2.zero;
            if (baseRect != null) baseRect.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Override.HasValue)
            {
                Direction = Override.Value;
                return;
            }

            Vector2 keyboard = ReadKeyboard();
            Pointer pointer = Pointer.current;

            if (pointer != null)
            {
                Vector2 screen = pointer.position.ReadValue();

                if (!_dragging && pointer.press.wasPressedThisFrame && screen.y < Screen.height * maxStartHeight)
                {
                    _dragging = true;
                    _startScreen = screen;
                    baseRect.gameObject.SetActive(true);
                    baseRect.anchoredPosition = ScreenToCanvas(screen);
                    knobRect.anchoredPosition = Vector2.zero;
                }
                else if (_dragging && !pointer.press.isPressed)
                {
                    _dragging = false;
                    baseRect.gameObject.SetActive(false);
                }

                if (_dragging)
                {
                    Vector2 delta = ScreenToCanvas(screen) - ScreenToCanvas(_startScreen);
                    Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
                    knobRect.anchoredPosition = clamped;
                    Direction = clamped / radius;
                    return;
                }
            }

            Direction = keyboard;
        }

        private Vector2 ScreenToCanvas(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            return local;
        }

        private static Vector2 ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
    }
}
