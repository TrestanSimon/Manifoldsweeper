using System;
using System.Linq;
using System.Collections;
using UnityEngine;

public class Tile : Quad {
    public enum Type {
        Invalid,
        Empty,
        Number,
        Mine
    }

    private Type _type;
    private int _number; // Number of mines in neighborhood
    private bool _revealed, _flagged, _exploded;
    private GameObject[] _flags;
    private Cloud[] _clouds;
    private GameObject _revealPS; // Particle system for tile reveal

    public int U {
        get => _u;
        private set {
            if (value < 0 || value > 99)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "U index range is between 0 and 99.");
            else _u = value;
        }
    }
    public int V {
        get => _v;
        private set {
            if (value < 0 || value > 99)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "V index range is between 0 and 99.");
            else _v = value;
        }
    }

    public Type type {
        get => _type;
        set => _type = value;
    }

    public int Number {
        get => _number;
        set {
            if (value < 0 || value > 8)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Tile number range is between 0 and 8.");
            if (type != Type.Mine) {
                if (value == 0) type = Type.Empty;
                else type = Type.Number;
            }
            _number = value;
        }
    }
    public bool Revealed {
        get => _revealed;
        set {
            if (value && Flagged)
                Flagged = false;
            _revealed = value;
        }
    }
    public bool Flagged {
        get => _flagged;
        set {
            if (!_revealed) {
                if (value && _flags == null)
                    _flags = new GameObject[_sideCount];
                else if (!value && _flags != null)
                    Complex.DestroyGOs(_flags);
                _flagged = value;
            }
        }
    }
    public bool Exploded {
        get => _exploded;
        set {
            if (_type == Type.Mine)
                _exploded = value;
        }
    }
    public bool Visited { get; set; }
    public int Depth { get; set; }

    // Constructor for Invalid Tiles
    public Tile() {}

    // Normal constructor
    public Tile(
        int u, int v, int sideCount,
        Vector3[] vertices,
        Complex complex
    ) : base(vertices) {
        U = u; V = v;

        for (int i = 0; i < sideCount; i++) {
            _gameObjects[i].name = $"Quad {i} ({u}, {v})";
            
            // Make tiles child of Complex GameObject
            _gameObjects[i].transform.parent = complex.transform;

            // For identifying tile instance from GameObject
            Tag tag = _gameObjects[i].AddComponent<Tag>();
            tag.u = U; tag.v = V;
        }
        //GenerateClouds();
    }

    // Updates mesh(es) with provided vertices
    public override void UpdateVertices(
        Vector3 vert0, Vector3 vert1,
        Vector3 vert2, Vector3 vert3
    ) {
        base.UpdateVertices(vert0, vert1, vert2, vert3);
        if (Flagged) UpdateFlags();
    }

    public override void SetMaterial(Material material) {
        base.SetMaterial(material);
        if (_sideCount > 1) {
            if (type == Type.Number && Revealed) {
                _meshes[0].uv = QuadUVCoords;
                _meshes[1].uv = QuadUVCoords.Reverse().ToArray();
            }
            else {
                _meshes[0].uv = QuadUVCoords.Reverse().ToArray();
                _meshes[1].uv = QuadUVCoords.Reverse().ToArray();
            }
        }
    }

    private void GenerateClouds() {
        _clouds ??= new Cloud[_sideCount];
        Vector3 altitude;

        for (int i = 0; i < _sideCount; i++) {
            altitude = _meshes[i].normals[0] * _Scale/20f;
            _clouds[i] = new Cloud(
                new Vector3[] {
                    _vertices[0] + altitude,
                    _vertices[1] + altitude,
                    _vertices[2] + altitude,
                    _vertices[3] + altitude
                }
            );
        }
    }

    // Delayed reveal based on flood depth
    public IEnumerator DelayedReveal(Material material, GameObject breakPS = null) {
        if (type == Type.Invalid) yield break;
        yield return new WaitForSeconds(0.02f * Depth);

        SetMaterial(material);

        if (breakPS != null) {
            _revealPS = Complex.CreateGO(breakPS, _vertices[0], Quaternion.identity, _Scale);
            _revealPS.transform.parent = _gameObjects[0].transform;
        }
    }

    // Places flag(s)
    public int FlagToggle(GameObject flagPrefab,
        Material materialFlag, Material materialUnknown
    ) {
        if (Flagged) return UnFlag(flagPrefab, materialUnknown);
        else return Flag(flagPrefab, materialFlag);
    }

    public int Flag(GameObject flagPrefab, Material materialFlag) {
        if (type == Type.Invalid || Revealed || Flagged) return 0;

        Flagged = true;
        
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
        SetMaterial(materialFlag);
        return 1;
    }

    public int UnFlag(GameObject flagPrefab, Material materialUnknown) {
        if (type == Type.Invalid || Revealed || !Flagged) return 0;

        Flagged = false;

        SetMaterial(materialUnknown);
        return -1;
    }

    private void UpdateFlags() {
        Vector3 stake = (_vertices[0] + _vertices[2]) / 2f;
        for (int i = 0; i < _sideCount; i++) {
            _flags[i].transform.position = stake + _meshes[i].normals[0] * _Scale/2f;
            _flags[i].transform.rotation = Quaternion.LookRotation(_meshes[i].normals[0])
                * Quaternion.AngleAxis(90, Vector3.up);
        }
    }
}
