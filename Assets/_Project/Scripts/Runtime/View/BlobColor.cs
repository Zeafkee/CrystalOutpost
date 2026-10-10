using UnityEngine;

namespace CrystalOutpost.View
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public sealed class BlobColor : MonoBehaviour
    {
        [SerializeField] private Color color = new Color(0f, 0f, 0f, 0.5f);

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _block;

        public void Set(Color value)
        {
            color = value;
            Apply();
        }

        private void OnEnable() => Apply();

        private void Apply()
        {
            _block ??= new MaterialPropertyBlock();
            _block.SetColor(ColorId, color);
            GetComponent<Renderer>().SetPropertyBlock(_block);
        }

#if UNITY_EDITOR
        private void OnValidate() => Apply();
#endif
    }
}
