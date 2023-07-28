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

    private List<CloneTile> _clones;

    private Material _cloudMaterialCache;

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
    public bool Revealed { get => _revealed; }
    public bool Flagged { get => _flagged; }
    public bool Exploded { get => _exploded; }
    public bool Visited { get; set; }
    public int Depth { get; set; }

    private Material _CurrentMaterial {
        get => _gameObjects[0].GetComponent<MeshRenderer>().material;
    }

    // Constructor for Invalid Tiles
    public Tile() {}

    // Normal constructor
    public Tile(
        int u, int v,
        Vector3[] vertices,
        Complex complex
    ) : base(u, v, 2, vertices, complex.transform) {
        U = u; V = v;
        _clones = new List<CloneTile>();

        _revealed = false;
        _flagged = false;
        _exploded = false;

        for (int i = 0; i < _sideCount; i++)
            _gameObjects[i].name = $"Tile {i} ({u}, {v})";
        
        _meshRenderers[0].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        _meshRenderers[1].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
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
        base.SetMaterial(material, Revealed, type == Type.Number);
        foreach (CloneTile clone in _clones)
            clone.SetMaterial(material, Revealed, type == Type.Number);
    }

    // Resets tile state for new game
    public void Reset(bool clearFlags) {
        type = Tile.Type.Empty;
        _revealed = false;
        _exploded = false;
        Visited = false;
        if (clearFlags) RemoveFlags();
    }

    // Delayed reveal based on flood depth
    public IEnumerator DelayedReveal(Material material, GameObject breakPS = null) {
        if (type == Type.Invalid) yield break;

        // breakPS = null;

        _revealed = true;
        // CheckWinCondition is ran before WaitForSeconds ends
        yield return new WaitForSeconds(0.05f * Depth);

        yield return Reveal(material, breakPS);
    }

    public override IEnumerator Reveal(Material material, GameObject breakPS = null) {
        if (type == Type.Mine) _exploded = true;

        yield return base.Reveal(material, breakPS);
        foreach (CloneTile clone in _clones)
            yield return clone.Reveal(material, breakPS);

        yield return null;
    }

    // Places flag(s)
    public int FlagToggle(GameObject flagPrefab, Material flagMaterial) {
        if (type == Type.Invalid || Revealed) return 0;

        if (Flagged) {
            RemoveFlags();
            return -1;
        } else {
            PlaceFlags(flagPrefab, flagMaterial);
            return 1;
        }
    }

    public override void PlaceFlags(GameObject flagPrefab, Material flagMaterial, bool scaled = true) {
        if (type == Type.Invalid || Revealed || Flagged) return;

        _flagged = true;

        _cloudMaterialCache ??= CurrentMaterial;
        
        base.PlaceFlags(flagPrefab, flagMaterial);
        foreach (CloneTile clone in _clones)
            clone.PlaceFlags(flagPrefab, flagMaterial);
    }

    // Removes flag(s)
    public override void RemoveFlags() {
        if (type == Type.Invalid || Revealed || !Flagged) return;

        _flagged = false;

        SetMaterial(_cloudMaterialCache);

        base.RemoveFlags();
        foreach (CloneTile clone in _clones) {
            clone.SetMaterial(_cloudMaterialCache);
            clone.RemoveFlags();
        }
    }

    public override void UpdateFlags() {
        base.UpdateFlags();
        foreach (CloneTile clone in _clones)
            clone.UpdateFlags();
    }

    public void CreateClone(Vector3 offset, bool reversed = false) {
        CloneTile clone = new CloneTile(
            U, V, OffsetVertices(offset), _gameObjects[0].transform, reversed);

        clone.SetMaterial(_CurrentMaterial, Revealed, type == Type.Number);
        if (Flagged) clone.PlaceFlags(_flags[0], CurrentMaterial, false);

        _clones.Add(clone);
    }

    public void DestroyClones() {
        foreach (CloneTile clone in _clones)
            clone.DestroySelf();
        _clones.Clear();
    }

    public override void DestroySelf() {
        DestroyClones();
        base.DestroySelf();
    }
}
