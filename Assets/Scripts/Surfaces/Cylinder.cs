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
        CurrentMap = initMap;
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
        if (newMap == CurrentMap) yield return null;
        else if (newMap == Map.Flat) {
            yield return StartCoroutine(CylinderToPlane());
        } else if (newMap == Map.Cylinder) {
            yield return StartCoroutine(CylinderToPlane(true));
        }
        CurrentMap = newMap;
    }

    public override void RepeatU() {
    }

    public override void RepeatV() {
        Color fadeColor = new Color(1f, 1f, 1f, 1f);

        CopyDepthV++;
        for (int v = 0; v < resV; v++) {
            for (int u = 0; u < resU; u++) {
                tiles[u,v].CreateClone(Offset[1] * CopyDepthV);
                tiles[u,v].CreateClone(-1*Offset[1] * CopyDepthV);
            }
        }

        CalculateCorners(CopyDepthU, CopyDepthV);
    }

    public override void CalculateCorners(int depthU, int depthV) {
        _corners = new Vector3[]{
            vertices[0,ResV] + Offset[1] * depthU,
            vertices[ResU,ResV] - Offset[1] * depthU,
            vertices[ResU,0] - Offset[1] * depthU,
            vertices[0,0] + Offset[1] * depthU,
        };
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
