using UnityEngine;

namespace CrystalOutpost.View
{
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private Transform target;
        [SerializeField] private float pitch = 58f;
        [SerializeField] private float visibleWidth = 13.5f;
        [SerializeField] private float followSharpness = 6f;
        [SerializeField] private float lookAhead = 2.5f;

        private Vector3 _focus;
        private float _lastAspect = -1f;
        private float _distance;

        private void Start()
        {
            _focus = FocusPoint();
            Apply();
        }

        private void LateUpdate()
        {
            Vector3 wanted = FocusPoint();
            _focus = Vector3.Lerp(_focus, wanted, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            Apply();
        }

        private Vector3 FocusPoint()
        {
            Vector3 p = target != null ? target.position : Vector3.zero;
            p.y = 0f;
            p.z += lookAhead;
            return p;
        }

        private void Apply()
        {
            if (!Mathf.Approximately(cam.aspect, _lastAspect))
            {
                _lastAspect = cam.aspect;
                float halfVertical = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * cam.aspect);
                _distance = visibleWidth * 0.5f / Mathf.Tan(halfHorizontal);
            }

            Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.SetPositionAndRotation(_focus - rotation * Vector3.forward * _distance, rotation);
        }

        public void SnapToTarget()
        {
            _lastAspect = -1f;
            _focus = FocusPoint();
            Apply();
        }
    }
}
