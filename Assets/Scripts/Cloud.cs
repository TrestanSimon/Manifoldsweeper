using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cloud : Quad {
    // Constructor for Invalid Tiles
    public Cloud() {}

    // Normal constructor
    public Cloud(
        Vector3[] vertices, int sideCount, int? cloudSeed
    ) : base(vertices, sideCount, false) {
        SetMaterial(Resources.Load(
                $"Materials/Clouds/TileCloud{cloudSeed}", typeof(Material)) as Material);
        for (int i = 0; i < _sideCount; i++) {
            _gameObjects[i].name = $"Cloud {i}";
            // _meshRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    public void Parent(GameObject parent) {
        foreach (GameObject cloud in _gameObjects)
            cloud.transform.parent = parent.transform;
    }

    public void Active(bool active) {
        foreach (GameObject cloud in _gameObjects)
            cloud.SetActive(active);
    }
}
