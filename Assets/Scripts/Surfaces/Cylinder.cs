using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Cylinder : Complex {
    public new static Dictionary<string, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat},
            {"Cylinder", Map.Cylinder}
        };
    }

    private float radius;
    
    public override Vector3[] Corners {
        get {
            _corners ??= new Vector3[]{
                vertices[0,ResV] + Offset[1],
                vertices[ResU,ResV] - Offset[1],
                vertices[ResU,0] - Offset[1],
                vertices[0,0] + Offset[1],
            };
            return _corners;
        }
    }

    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        radius = resU / 16f;
        currentMap = initMap;
        InitVertices(initMap);
        InitTiles();

        if (initMap == Map.Flat) 
            RepeatComplex();
    }

    protected override void InitVertices(Map map) {
        switch(map) {
            case Map.Flat: vertices = CylinderInvoluteMap(1, radius); break;
            case Map.Cylinder: vertices = CylinderInvoluteMap(0, radius); break;
        }
    }

    public override Tile GetNeighbor(int u, int v) {
        // Wraps around u
        int u1 = u >= 0 ? u % resU : u + resU;
        int v1 = v;

        if (u1 >= 0 && u1 < resU && v1 >= 0 && v1 < resV) return tiles[u1,v1];
        else return new Tile();
    }

    public override IEnumerator ReMap(Map newMap) {
        if (newMap == currentMap) yield return null;
        else if (newMap == Map.Flat) {
            yield return StartCoroutine(CylinderToPlane());
            RepeatComplex();
        } else if (newMap == Map.Cylinder) {
            DumpRepeatComplex();
            yield return StartCoroutine(CylinderToPlane(true));
        }
        currentMap = newMap;
    }

    public override void RepeatComplex() {
        for (int v = 0; v < resV; v++) {
            for (int u = 0; u < resU; u++) {
                tiles[u,v].CreateChild(Offset[1]);
                tiles[u,v].CreateChild(-1*Offset[1]);
            }
        }
    }

    public IEnumerator CylinderToPlane(bool reverse = false) {
        float time = 0f;
        float t;
        float duration = 2f;

        while (time < duration) {
            t = reverse ? 1f - time / duration : time / duration;
            t = t * t * (3f - 2f * t);
            UpdateVertices(CylinderInvoluteMap(t, radius));

            time += Time.deltaTime;
            yield return null;
        }
        // Finalize mapping
        vertices = CylinderInvoluteMap((reverse ? 0f : 1f), radius);
        UpdateVertices(vertices);
    }
}
