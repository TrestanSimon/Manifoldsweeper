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
        bool reversed = false
    ) : base(u, v, 1, vertices, parent) {
        _gameObjects[0].name = $"Clone ({u}, {v})";
        _reversed = reversed;
    }

    public override void SetMaterial(Material material, bool isRevealed, bool isNumber) {
        base.SetMaterial(material);
        if (isRevealed && isNumber) {
            _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
        } else {
            _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
        }
    }
}