using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cloud : Quad {
    private Material _MaterialCloud {
        get => Resources.Load(
            "Materials/OceanMats/TileCloud 1", typeof(Material)) as Material;
    }
    // Constructor for Invalid Tiles
    public Cloud() {}

    // Normal constructor
    public Cloud(
        Vector3[] vertices
    ) : base(vertices, 1, false) {
        SetMaterial(_MaterialCloud);
        for (int i = 0; i < _sideCount; i++) {
            // _meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
