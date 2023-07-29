using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Klein : Complex {
    public new static Dictionary<string, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat},
            {"Bottle", Map.KleinBottle},
            {"Figure 8", Map.Figure8},
            {"Pinched torus", Map.PinchedTorus}
        };
    }
    
    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        CurrentMap = initMap;
        InitVertices(initMap);
        InitTiles();
    }

    protected override void InitVertices(Map map) {
        switch(map) {
            case Map.Flat: vertices = PlaneMap(); break;
            case Map.KleinBottle: vertices = BottleMap(); break;
            case Map.Figure8: vertices = Figure8Map(); break;
            case Map.PinchedTorus: vertices = PinchedTorusMap(); break;
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
        if (newMap == CurrentMap) yield return null;
        else if (newMap == Map.Flat) {
            yield return StartCoroutine(ComplexLerp(
                new Vector3[][,]{vertices, PlaneMap()}, 2f));
        } else if (newMap == Map.KleinBottle) {
            yield return StartCoroutine(ComplexLerp(
                new Vector3[][,]{vertices, BottleMap()}, 2f));
        } else if (newMap == Map.Figure8) {
            yield return StartCoroutine(ComplexLerp(
                new Vector3[][,]{vertices, Figure8Map()}, 2f));
        } else if (newMap == Map.PinchedTorus) {
            yield return StartCoroutine(ComplexLerp(
                new Vector3[][,]{vertices, PinchedTorusMap()}, 2f));
        }
        CurrentMap = newMap;
    }

    public override IEnumerator RepeatU() {
        CopyDepthU++;
        bool isReversed;
        Vector3 flipper = Vector3.zero;
        Vector3 offsetV;
        int indexExtraV = CopyDepthV > 1 ? 1 : 0;

        for (int v = 0; v < resV; v++) {
            flipper = (tiles[0,ResV-v-1].Vertices[0].z - tiles[0,v].Vertices[0].z) * Vector3.forward;
            for (int u = 0; u < resU; u++) {
                for (int indexV = -CopyDepthV; indexV <= CopyDepthV; indexV++) {
                    offsetV = Offset[1] * indexV;                    
                    isReversed = Mathf.Abs(indexV) % 2 == 1;

                    if (isReversed) {
                        tiles[u,v].CreateClone(Offset[0] * CopyDepthU + offsetV + flipper, true);
                        tiles[u,v].CreateClone(Offset[0] * -CopyDepthU + offsetV + flipper, true);
                    } else {
                        tiles[u,v].CreateClone(Offset[0] * CopyDepthU + offsetV, false);
                        tiles[u,v].CreateClone(Offset[0] * -CopyDepthU + offsetV, false);
                    }
                }
            }
        }

        CalculateCorners(CopyDepthU, CopyDepthV);
        yield return null;
    }

    public override IEnumerator RepeatV() {
        CopyDepthV++;
        if (CopyDepthV == 1) CopyDepthV++;
        bool isReversed = CopyDepthV % 2 == 1;
        Vector3 flipper = Vector3.zero;
        Vector3 offsetU;

        for (int v = 0; v < resV; v++) {
            flipper = (tiles[0,ResV-v-1].Vertices[0].z - tiles[0,v].Vertices[0].z) * Vector3.forward;
            for (int u = 0; u < resU; u++) {
                // Fill left-right depending on CopyDepthU
                for (int indexU = -CopyDepthU; indexU <= CopyDepthU; indexU++) {
                    offsetU = Offset[0] * indexU;

                    if (CopyDepthV == 2) {
                        tiles[u,v].CreateClone(Offset[1] + offsetU + flipper, true);
                        tiles[u,v].CreateClone(-1 * Offset[1] + offsetU + flipper, true);
                    }

                    if (isReversed) {
                        tiles[u,v].CreateClone(Offset[1] * CopyDepthV + offsetU + flipper, true);
                        tiles[u,v].CreateClone(Offset[1] * -CopyDepthV + offsetU + flipper, true);
                    } else {
                        tiles[u,v].CreateClone(Offset[1] * CopyDepthV + offsetU, false);
                        tiles[u,v].CreateClone(Offset[1] * -CopyDepthV + offsetU, false);
                    }
                }
            }
        }

        CalculateCorners(CopyDepthU, CopyDepthV);
        yield return null;
    }

    public override void CalculateCorners(int depthU, int depthV) {
        Vector3 offset = Offset[0] * depthU + Offset[1] * depthV;
        _corners = new Vector3[]{
            vertices[0,ResV] + offset,
            vertices[ResU,ResV] - offset,
            vertices[ResU,0] - offset,
            vertices[0,0] + offset,
        };
    }

    private Vector3[,] BottleMap() {
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

    private Vector3[,] Figure8Map() {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        float p1, q1, sinp, cosp, sinp05, cosp05, sinq, cosq, sin2q;

        for (int p = 0; p <= resU; p++) {
            p1 = 2f*PI*(float)p / (float)resU;
            sincos(p1, out sinp, out cosp);
            sincos(p1*0.5f, out sinp05, out cosp05);

            for (int q = 0; q <= resV; q++) {
                q1 = 2f*PI*(float)q / (float)resV;
                sincos(q1, out sinq, out cosq);
                sin2q = sin(2f*q1);

                tempVerts[p,q] = new Vector3(
                    (2f + cosp05 * sinq - sinp05 * sin2q) * cosp,
                    sinp05 * sinq + cosp05 * sin2q,
                    (2f + cosp05 * sinq - sinp05 * sin2q) * sinp
                );
            }
        }
        return tempVerts;
    }

    private Vector3[,] PinchedTorusMap() {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        float p1, q1, sinp, cosp, sinp05, cosp05, sinq, cosq;
        float R = this.ResU / 16f;
        float r = this.ResV / 16f;

        for (int p = 0; p <= resU; p++) {
            p1 = 2f*PI*(float)p / (float)resU;
            sincos(p1, out sinp, out cosp);
            sincos(p1*0.5f, out sinp05, out cosp05);

            for (int q = 0; q <= resV; q++) {
                q1 = 2f*PI*(float)q / (float)resV;
                sincos(q1, out sinq, out cosq);

                tempVerts[p,q] = new Vector3(
                    (R + r * cosq) * cosp,
                    r * sinq * cosp05,
                    (R + r * cosq) * sinp
                );
            }
        }
        return tempVerts;
    }
}
