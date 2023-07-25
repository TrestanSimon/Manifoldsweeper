using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : GenericTile {
    public enum Type {
        Invalid,
        Empty,
        Number,
        Mine
    }

    private int _u, _v;
    private Type _type;
    private int _number; // Number of mines in neighborhood
    private bool _revealed, _flagged, _exploded;

    private List<GenericTile> _children;

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

    public List<GenericTile> Children {
        get => _children;
    }

    // Constructor for Invalid Tiles
    public Tile() {}

    // Normal constructor
    public Tile(
        int u, int v, int sideCount,
        Vector3[] vertices,
        Complex complex
    ) : base(u, v, sideCount, vertices, complex.transform) {
        U = u; V = v;
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

    // Resets tile state for new game
    public void Reset(bool clearFlags) {
        type = Tile.Type.Empty;
        Revealed = false;
        Exploded = false;
        Visited = false;
        if (clearFlags) Flagged = false;
        
        // Reactivate clouds
        foreach (Cloud cloud in _clouds)
            cloud.Active(true);
    }

    // Delayed reveal based on flood depth
    public IEnumerator DelayedReveal(Material material, GameObject breakPS = null) {
        if (type == Type.Invalid) yield break;
        yield return new WaitForSeconds(0.02f * Depth);

        Reveal(material, breakPS);
        foreach (GenericTile child in _children)
            child.Reveal(material, breakPS);
    }

    // Places flag(s)
    public int FlagToggle(GameObject flagPrefab,
        Material materialFlag, Material materialUnknown
    ) {
        if (Flagged) return UnFlag(flagPrefab, materialUnknown);
        else return Flag(flagPrefab, materialUnknown);
    }

    public int Flag(GameObject flagPrefab, Material materialFlag) {
        if (type == Type.Invalid || Revealed || Flagged) return 0;

        Flagged = true;
        
        PlaceFlags(flagPrefab, materialFlag);
        foreach (GenericTile child in _children)
            child.PlaceFlags(flagPrefab, materialFlag);

        return 1;
    }

    // Removes flag(s)
    public int UnFlag(GameObject flagPrefab, Material materialUnknown) {
        if (type == Type.Invalid || Revealed || !Flagged) return 0;

        Flagged = false;

        SetMaterial(materialUnknown);
        return -1;
    }

    public override void UpdateFlags() {
        base.UpdateFlags();
        foreach (GenericTile child in _children)
            child.UpdateFlags();
    }

    public void CreateChild() {
        GenericTile child = new GenericTile(U, V, 2, _vertices, null);
        // UPDATE CHILD
        Children.Add(child);
    }
}
