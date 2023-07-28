using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cloud : Quad {
    private Material _cloudMaterial;

    // Constructor for Invalid Tiles
    public Cloud() {}

    // Normal constructor
    public Cloud(
        Vector3[] vertices, int sideCount, Material material
    ) : base(vertices, sideCount, false) {
        SetMaterial(material);
        for (int i = 0; i < _sideCount; i++) {
            _gameObjects[i].name = $"Cloud {i}";
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

    public void Flag(Material flagMaterial) {
        _cloudMaterial ??= _meshRenderers[0].material;
        SetMaterial(flagMaterial);
    }

    public void UnFlag() {
        SetMaterial(_cloudMaterial);
    }
}
