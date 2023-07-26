using System;
using System.Linq;
using UnityEngine;

public class GenericTile : Quad {
    protected GameObject[] _flags;
    protected Cloud[] _clouds;
    protected int? _cloudSeed;
    protected GameObject _revealPS; // Particle system for tile reveal

    // Constructor for Invalid Tiles
    public GenericTile() {}

    // Normal constructor
    public GenericTile(
        int u, int v, int sideCount,
        Vector3[] vertices,
        Transform parent,
        int? cloudSeed = null
    ) : base(vertices, sideCount) {
        cloudSeed ??= UnityEngine.Random.Range(0, 2);
        _cloudSeed = cloudSeed;
        _clouds = new Cloud[_sideCount];

        for (int i = 0; i < sideCount; i++) {            
            // Make tiles child of Complex GameObject
            _gameObjects[i].transform.parent = parent;

            // For identifying tile instance from GameObject
            Tag tag = _gameObjects[i].AddComponent<Tag>();
            tag.u = u; tag.v = v;
        }
        InitializeClouds();
    }

    // Updates mesh(es) with provided vertices
    public override void UpdateVertices(
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        base.UpdateVertices(vert0, vert1, vert2, vert3);
        foreach (Cloud cloud in _clouds) UpdateClouds();
    }

    public virtual void SetMaterial(Material material, bool isRevealedNumber) {
        base.SetMaterial(material);
        if (isRevealedNumber) {
            _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
            if (_sideCount > 1)
                _meshes[1].uv = QuadUVCoords;
        } else {
            _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
            if (_sideCount > 1)
                _meshes[1].uv = QuadUVCoords.Reverse().ToArray();
        }
    }

    protected virtual void InitializeClouds(bool random = false) {
        for (int i = 0; i < _sideCount; i++) {
            Vector3 altitude = _meshes[i].normals[0] * _Scale/10f;
            _clouds[i] = new Cloud(
                new Vector3[] {
                    _vertices[0] + altitude,
                    _vertices[1] + altitude,
                    _vertices[2] + altitude,
                    _vertices[3] + altitude
                }, 2, _cloudSeed
            );
            _clouds[i].Parent(_gameObjects[i]);
        }
    }

    private void UpdateClouds() {
        for (int i = 0; i < _clouds.Length; i++) {
            Vector3 altitude = _meshes[i].normals[0] * _Scale/10f;
            _clouds[i].UpdateVertices(
                    _vertices[0] + altitude,
                    _vertices[1] + altitude,
                    _vertices[2] + altitude,
                    _vertices[3] + altitude
            );
        }
    }

    public virtual void ActivateClouds(bool activated) {
        foreach (Cloud cloud in _clouds)
            cloud.Active(activated);
    }

    public virtual void Reveal(Material material, GameObject breakPS = null) {
        SetMaterial(material);

        ActivateClouds(false);

        if (breakPS != null) {
            _revealPS = Complex.CreateGO(breakPS, _vertices[0], Quaternion.identity, _Scale);
            _revealPS.transform.parent = _gameObjects[0].transform;
        }
    }

    public virtual void PlaceFlags(GameObject flagPrefab) {
        _flags ??= new GameObject[_sideCount];

        // Points to where flag will be planted
        Vector3 stake = (_vertices[0] + _vertices[2]) / 2f;

        // Create flag for each side
        for (int i = 0; i < _sideCount; i++) {
            Vector3 flagPos = stake + _meshes[i].normals[0] * _Scale/2f;
            Quaternion flagRot = Quaternion.LookRotation(_meshes[i].normals[0])
                * Quaternion.AngleAxis(90, Vector3.up);

            _flags[i] = Complex.CreateGO(flagPrefab, flagPos, flagRot, _Scale);
            _flags[i].transform.parent = _gameObjects[i].transform;
            _flags[i].name = "Flag";
        }
    }

    public virtual void RemoveFlags() {
        if (_flags is not null)
            foreach (GameObject flag in _flags)
                Complex.Destroy(flag);
    }

    public virtual void UpdateFlags() {
        Vector3 stake = (_vertices[0] + _vertices[2]) / 2f;
        for (int i = 0; i < _sideCount; i++) {
            _flags[i].transform.position = stake + _meshes[i].normals[0] * _Scale/2f;
            _flags[i].transform.rotation = Quaternion.LookRotation(_meshes[i].normals[0])
                * Quaternion.AngleAxis(90, Vector3.up);
        }
    }

    public override void DestroySelf() {
        RemoveFlags();
        
        if (_clouds is not null)
            foreach (Quad cloud in _clouds)
                cloud.DestroySelf();
        
        base.DestroySelf();
    }
}