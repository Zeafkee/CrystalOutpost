using System.Collections.Generic;
using UnityEngine;

namespace CrystalOutpost.EditorTools
{
    public sealed class MeshBuilder
    {
        public const float Body = 0f;
        public const float Leg = 1f;
        public const float Arm = 2f;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<Vector2> _limbs = new List<Vector2>();
        private readonly List<Vector3> _pivots = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();

        private Color _color = Color.white;
        private Vector2 _limb = Vector2.zero;
        private Vector3 _pivot = Vector3.zero;

        public MeshBuilder Paint(Color32 color, float emission = 0f)
        {
            Color linear = new Color(color.r / 255f, color.g / 255f, color.b / 255f, 1f).linear;
            _color = new Color(linear.r, linear.g, linear.b, emission);
            return this;
        }

        public MeshBuilder Limb(float type, float side, Vector3 pivot)
        {
            _limb = new Vector2(type, side);
            _pivot = pivot;
            return this;
        }

        public MeshBuilder NoLimb()
        {
            _limb = Vector2.zero;
            _pivot = Vector3.zero;
            return this;
        }

        public MeshBuilder Box(Vector3 center, Vector3 size) => Box(center, size, Quaternion.identity);

        public MeshBuilder Box(Vector3 center, Vector3 size, Quaternion rotation)
        {
            Vector3 h = size * 0.5f;
            Vector3[] c =
            {
                new Vector3(-h.x, -h.y, -h.z), new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z), new Vector3(-h.x, h.y, -h.z),
                new Vector3(-h.x, -h.y, h.z), new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z), new Vector3(-h.x, h.y, h.z),
            };
            for (int i = 0; i < 8; i++) c[i] = center + rotation * c[i];

            Quad(c[0], c[3], c[2], c[1]);
            Quad(c[5], c[6], c[7], c[4]);
            Quad(c[4], c[7], c[3], c[0]);
            Quad(c[1], c[2], c[6], c[5]);
            Quad(c[3], c[7], c[6], c[2]);
            Quad(c[4], c[0], c[1], c[5]);
            return this;
        }

        public MeshBuilder Prism(Vector3 baseCenter, float height, float bottomRadius, float topRadius, int sides,
            Quaternion rotation, float twist = 0f)
        {
            Vector3 top = baseCenter + rotation * (Vector3.up * height);
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f + twist;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f + twist;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));

                Vector3 b0 = baseCenter + rotation * (d0 * bottomRadius);
                Vector3 b1 = baseCenter + rotation * (d1 * bottomRadius);
                Vector3 t0 = top + rotation * (d0 * topRadius);
                Vector3 t1 = top + rotation * (d1 * topRadius);

                if (topRadius > 0.0001f && bottomRadius > 0.0001f) Quad(b0, t0, t1, b1);
                else if (bottomRadius > 0.0001f) Triangle(b0, top, b1);
                else Triangle(baseCenter, t0, t1);

                if (bottomRadius > 0.0001f) Triangle(baseCenter, b0, b1);
                if (topRadius > 0.0001f) Triangle(top, t1, t0);
            }

            return this;
        }

        public MeshBuilder Prism(Vector3 baseCenter, float height, float bottomRadius, float topRadius, int sides) =>
            Prism(baseCenter, height, bottomRadius, topRadius, sides, Quaternion.identity);

        public MeshBuilder Crystal(Vector3 baseCenter, float height, float radius, int sides, Quaternion rotation, float waist = 0.35f)
        {
            Prism(baseCenter, height * waist, radius * 0.45f, radius, sides, rotation);
            Vector3 mid = baseCenter + rotation * (Vector3.up * height * waist);
            Prism(mid, height * (1f - waist), radius, 0f, sides, rotation);
            return this;
        }

        public MeshBuilder Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int start = _vertices.Count;
            Add(a, normal); Add(b, normal); Add(c, normal); Add(d, normal);
            _triangles.Add(start); _triangles.Add(start + 1); _triangles.Add(start + 2);
            _triangles.Add(start); _triangles.Add(start + 2); _triangles.Add(start + 3);
            return this;
        }

        public MeshBuilder Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int start = _vertices.Count;
            Add(a, normal); Add(b, normal); Add(c, normal);
            _triangles.Add(start); _triangles.Add(start + 1); _triangles.Add(start + 2);
            return this;
        }

        private void Add(Vector3 position, Vector3 normal)
        {
            _vertices.Add(position);
            _normals.Add(normal);
            _colors.Add(_color);
            _limbs.Add(_limb);
            _pivots.Add(_pivot);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (_vertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _limbs);
            mesh.SetUVs(1, _pivots);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();

            Bounds bounds = mesh.bounds;
            bounds.Expand(0.6f);
            mesh.bounds = bounds;
            mesh.UploadMeshData(false);
            return mesh;
        }
    }
}
