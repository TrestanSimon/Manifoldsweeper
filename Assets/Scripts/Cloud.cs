using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cloud : Quad {
    private Material _MaterialCloud {
        get {
            int rand = Random.Range(0, 2);
            return Resources.Load(
                $"Materials/Clouds/TileCloud{rand}", typeof(Material)) as Material;
            
        }
    }
    // Constructor for Invalid Tiles
    public Cloud() {}

    // Normal constructor
    public Cloud(
        Vector3[] vertices
    ) : base(vertices, 2, false) {
        SetMaterial(_MaterialCloud);
        for (int i = 0; i < _sideCount; i++) {
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
