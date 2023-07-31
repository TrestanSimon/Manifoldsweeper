using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericTile : Quad {
    protected GameObject[] _flags;
    protected GameObject _revealPS; // Particle system for tile reveal

    // Constructor for Invalid Tiles
    public GenericTile() {}

    // Normal constructor
    public GenericTile(
        int u, int v, int sideCount,
        Vector3[] vertices,
        Transform parent
    ) : base(vertices, sideCount) {
        for (int i = 0; i < sideCount; i++) {            
            // Make tiles child of Complex GameObject
            _gameObjects[i].transform.parent = parent;

            // For identifying tile instance from GameObject
            Tag tag = _gameObjects[i].AddComponent<Tag>();
            tag.u = u; tag.v = v;
        }
    }

    // Updates mesh(es) with provided vertices
    public override void UpdateVertices(
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        base.UpdateVertices(vert0, vert1, vert2, vert3);
    }

    public virtual void SetMaterial(Material material, bool isRevealed, bool isNumber) {
        base.SetMaterial(material);
        // This is a mess...
        if (isRevealed && isNumber) {
            _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
            if (_sideCount > 1)
                _meshes[1].uv = QuadUVCoords;
        } else if (!isRevealed) {
            if (_sideCount > 1)
                _meshes[1].uv = QuadUVCoords;
        } else {
            _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
            if (_sideCount > 1)
                _meshes[1].uv = QuadUVCoords.Reverse().ToArray();
        }
    }

    public void SetColor(Color color) {
        CurrentMaterial.color = color;
    }

    public virtual IEnumerator Reveal(Material material, GameObject breakPS = null) {
        SetMaterial(material);

        if (breakPS != null) {
            for (int i = 0; i < _sideCount; i++) {
                _revealPS = Complex.CreateGO(breakPS, (_vertices[0]+_vertices[2])/2+ _meshes[i].normals[0] * _Scale/2f, Quaternion.identity, _Scale);
                _revealPS.transform.parent = _gameObjects[i].transform;
            }
        }

        yield return null;
    }

    public virtual void PlaceFlags(GameObject flagPrefab, Material flagMaterial, bool scaled = true) {
        _flags ??= new GameObject[_sideCount];
        float scale = scaled ? _Scale : 1f;

        // Points to where flag will be planted
        Vector3 stake = (_vertices[0] + _vertices[2]) / 2f;

        // Create flag for each side
        for (int i = 0; i < _sideCount; i++) {
            Vector3 flagPos = stake + _meshes[i].normals[0] * _Scale/2f;
            Quaternion flagRot = Quaternion.LookRotation(_meshes[i].normals[0])
                * Quaternion.AngleAxis(90, Vector3.up);

            _flags[i] = Complex.CreateGO(flagPrefab, flagPos, flagRot, scale);
            _flags[i].transform.parent = _gameObjects[i].transform;
            _flags[i].name = "Flag";

            SetMaterial(flagMaterial);
        }
    }

    public virtual void RemoveFlags() {
        if (_flags is not null) {
            foreach (GameObject flag in _flags)
                Complex.Destroy(flag);
        }
    }

    public virtual void UpdateFlags() {
        for (int i = 0; i < _sideCount; i++) {
            _flags[i].transform.position = (_vertices[0] + _vertices[2]) / 2f + _meshes[i].normals[0] * _Scale/2f;
            _flags[i].transform.rotation = Quaternion.LookRotation(_meshes[i].normals[0])
                * Quaternion.AngleAxis(90, Vector3.up);
        }
    }

    public override void DestroySelf() {
        RemoveFlags();        
        base.DestroySelf();
    }
}