using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class KleinBottle : Complex {
    public new static Dictionary<string, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat},
            {"Bottle", Map.KleinBottle}
        };
    }

    public override Vector3[] Corners {
        get {
            _corners ??= new Vector3[]{
                vertices[0,ResV] + Offset[0] + Offset[1],
                vertices[ResU,ResV] - Offset[0] - Offset[1],
                vertices[ResU,0] - Offset[0] - Offset[1],
                vertices[0,0] + Offset[0] + Offset[1],
            };
            return _corners;
        }
    }
    
    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        currentMap = initMap;
        InitVertices(initMap);
        InitTiles();

        if (initMap == Map.Flat) RepeatComplex();
    }

    protected override void InitVertices(Map map) {
        switch(map) {
            case Map.Flat: vertices = PlaneMap(); break;
            case Map.KleinBottle: vertices = KleinMap(); break;
        }
    }

    public override Tile GetNeighbor(int u, int v) {
        // Wraps around u and v
        int u1 = u >= 0 ? u % resU : u + resU;
        int v1 = v >= 0 ? v % resV : v + resV;

        // Flips v when wrapping
        if (u % (2*resU) >= resU || u % (-2*resU) < 0)    
            v1 = resV - (v1 + 1);

        if (u1 >= 0 && u1 < resU && v1 >= 0 && v1 < resV) return tiles[u1,v1];
        else return new Tile();
    }

    public override IEnumerator ReMap(Map newMap) {
        if (newMap == currentMap) yield return null;
        else if (newMap == Map.Flat) {
            yield return StartCoroutine(ComplexLerp(
                new Vector3[][,]{vertices, PlaneMap()}, 2f));
            RepeatComplex();
        } else if (newMap == Map.KleinBottle) {
            DumpRepeatComplex();
            yield return StartCoroutine(ComplexLerp(
                new Vector3[][,]{vertices, KleinMap()}, 2f));
        }
        currentMap = newMap;
    }

    // NEEDS TO BE FIXED
    public override void RepeatComplex() {
        for (int v = 0; v < resV; v++) {
            for (int u = 0; u < resU; u++) {
                tiles[u,v].CreateChild(Offset[1]);
                tiles[u,v].CreateChild(-1*Offset[1]);
            }
        }
    }

    private Vector3[,] KleinMap() {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        for (int p = 0; p <= resU; p++) {
            float p1 = 4f*PI*(float)p / (float)resU;
            sincos(p1, out float sinp, out float cosp);
            for (int q = 0; q <= resV; q++) {
                float q1 = 2f*PI*(float)q / (float)resV;
                sincos(q1, out float sinq, out float cosq);

                if (p1 < PI) {
                    tempVerts[p,q] = new Vector3(
                        (2.5f - 1.5f*cosp) * cosq,
                        (2.5f - 1.5f*cosp) * sinq,
                        -2.5f * sinp
                    );
                } else if (p1 < 2f*PI) {
                    tempVerts[p,q] = new Vector3(
                        (2.5f - 1.5f*cosp) * cosq,
                        (2.5f - 1.5f*cosp) * sinq,
                        3f*p1 - 3f*PI
                    );
                } else if (p1 < 3f*PI) {
                    tempVerts[p,q] = new Vector3(
                        -2f + (2f + cosq) * cosp,
                        sinq,
                        (2f + cosq) * sinp + 3f*PI
                    );
                } else {
                    tempVerts[p,q] = new Vector3(
                        -2f + 2f*cosp - cosq,
                        sinq,
                        -3f*p1 + 12f*PI
                    );
                }
            }
        }
        return tempVerts;
    }
}
