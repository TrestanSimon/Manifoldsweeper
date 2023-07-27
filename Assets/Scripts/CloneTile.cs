using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloneTile : GenericTile {
    private bool _reversed;

    public CloneTile(
        int u, int v,
        Vector3[] vertices,
        Transform parent,
        int? cloudSeed = null,
        bool reversed = false
    ) : base(u, v, 1, vertices, parent, cloudSeed) {
        _gameObjects[0].name = $"Clone ({u}, {v})";
        _reversed = reversed;
    }

    protected override void InitializeClouds(bool random = false) {
        for (int i = 0; i < _sideCount; i++) {
            Vector3 altitude = _meshes[i].normals[0] * _Scale/10f;
            _clouds[i] = new Cloud(
                new Vector3[] {
                    _vertices[0] + altitude,
                    _vertices[1] + altitude,
                    _vertices[2] + altitude,
                    _vertices[3] + altitude
                }, 1, _cloudSeed
            );
            _clouds[i].Parent(_gameObjects[i]);
        }
    }

    public override void SetMaterial(Material material, bool isRevealedNumber) {
        base.SetMaterial(material);
        if (isRevealedNumber) {
            _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
        } else {
            if (!_reversed)
                _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
            else
                _meshes[0].uv = QuadUVCoords;
        }
    }
}