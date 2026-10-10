using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrystalOutpost.View
{
    public sealed class InstanceBatcher
    {
        private const int BatchSize = 1023;

        private sealed class Batch
        {
            public readonly Matrix4x4[] Matrices = new Matrix4x4[BatchSize];
            public readonly Vector4[] Data = new Vector4[BatchSize];
            public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
            public int Count;
        }

        private readonly Mesh _mesh;
        private readonly Material _material;
        private readonly int _propertyId;
        private readonly List<Batch> _batches = new List<Batch>();
        private int _used;

        public int InstanceCount { get; private set; }
        public int DrawCalls => _used;

        public InstanceBatcher(Mesh mesh, Material material, string instancedProperty)
        {
            _mesh = mesh;
            _material = material;
            _propertyId = Shader.PropertyToID(instancedProperty);
        }

        public void Begin()
        {
            for (int i = 0; i < _used; i++) _batches[i].Count = 0;
            _used = 0;
            InstanceCount = 0;
        }

        public void Add(in Matrix4x4 matrix, in Vector4 data)
        {
            if (_used == 0 || _batches[_used - 1].Count == BatchSize)
            {
                if (_used == _batches.Count) _batches.Add(new Batch());
                _used++;
            }

            Batch batch = _batches[_used - 1];
            batch.Matrices[batch.Count] = matrix;
            batch.Data[batch.Count] = data;
            batch.Count++;
            InstanceCount++;
        }

        public void Draw(in Bounds worldBounds, int layer)
        {
            for (int i = 0; i < _used; i++)
            {
                Batch batch = _batches[i];
                if (batch.Count == 0) continue;

                batch.Block.SetVectorArray(_propertyId, batch.Data);
                var parameters = new RenderParams(_material)
                {
                    worldBounds = worldBounds,
                    matProps = batch.Block,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    layer = layer,
                };

                Graphics.RenderMeshInstanced(parameters, _mesh, 0, batch.Matrices, batch.Count);
            }
        }
    }
}
