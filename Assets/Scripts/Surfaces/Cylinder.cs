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

    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        radius = resU / 16f;
        currentMap = initMap;
        InitVertices(initMap);
        InitTiles();
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
        } else if (newMap == Map.Cylinder) {
            yield return StartCoroutine(CylinderToPlane(true));
        }
        currentMap = newMap;
    }

    public override void RepeatComplex() {
        Instantiate(gameObject,
            2f*(vertices[0,resV/2] + radius*Vector3.up),
            Quaternion.identity);
        Instantiate(gameObject,
            -2f*(vertices[0,resV/2] + radius*Vector3.up),
            Quaternion.identity);
    }

    public IEnumerator CylinderToPlane(bool reverse = false) {
        float time = 0f;
        float duration = 1f;
        float progress;

        while (time < duration) {
            progress = time / duration;
            UpdateVertices(CylinderInvoluteMap(
                (reverse ? 1f - progress : progress), radius));

            time += Time.deltaTime;
            yield return null;
        }
        // Finalize mapping
        vertices = CylinderInvoluteMap((reverse ? 0f : 1f), radius);
        UpdateVertices(vertices);
    }
}
