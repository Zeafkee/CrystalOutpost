using UnityEngine;

namespace CrystalOutpost.World
{
    public sealed class NavObstacle : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radius = 0.8f;

        public float Radius => radius;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
