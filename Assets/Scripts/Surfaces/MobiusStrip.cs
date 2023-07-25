using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class MobiusStrip : Complex {
    public new static Dictionary<string, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat},
            {"Strip", Map.MobiusStrip},
            {"Sudanese", Map.MobiusSudanese}
        };
    }
    
    public float R, tau;

    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        R = resU / 72f;
        tau = 16f / (float)resV;
        currentMap = initMap;
        InitVertices(initMap);
        InitTiles();
        if (initMap == Map.Flat) RepeatComplex();
    }

    protected override void InitVertices(Map map) {
        switch(map) {
            case Map.Flat: vertices = PlaneMap(); break;
            case Map.MobiusStrip: vertices = StripMap(); break;
            case Map.MobiusSudanese: vertices = SudaneseMap(); break;
        }
    }

    public override Tile GetNeighbor(int u, int v) {
        // Wraps around u
        int u1 = u >= 0 ? u % resU : u + resU;
        int v1 = v;

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
        } else if (newMap == Map.MobiusStrip) {
            DumpRepeatComplex();
            yield return StartCoroutine(ComplexLerp(
                new Vector3[][,]{vertices, StripMap()}, 2f));
        } else if (newMap == Map.MobiusSudanese) {
            DumpRepeatComplex();
            yield return StartCoroutine(ComplexLerp(
                new Vector3[][,]{vertices, SudaneseMap()}, 2f));
        }
        currentMap = newMap;
    }

    // NEEDS TO BE FIXED
    public override void RepeatComplex() {
        Vector3 offset = 2f*vertices[0,resV/2];

        for (int v = 0; v < resV; v++) {
            for (int u = 0; u < resU; u++) {
                tiles[u,v].CreateChild(offset);
                tiles[u,v].CreateChild(-1*offset);
            }
        }
    }

    private Vector3[,] StripMap() {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        float q1, minor;

        for (int p = 0; p <= resU; p++) {
            sincos(2*PI*p / resU, out float sinp, out float cosp);
            sincos(PI*p / resU, out float sinp2, out float cosp2);

            for (int q = 0; q <= resV; q++) {
                q1 = q / (float)resV - 0.5f;
                minor = R + q1/tau * cosp2;

                tempVerts[p,q] = 5f * new Vector3(
                    q1/tau * sinp2,
                    minor * sinp,
                    minor * cosp
                );
            }
        }
        return tempVerts;
    }

    private Vector3[,] SudaneseMap() {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        float x, y, z, w, ys, ws;

        for (int p = 0; p <= resU; p++) {
            sincos(2*PI*p / resU, out float sin2p, out float cos2p);
            sincos(PI*p / resU, out float sinp, out float cosp);

            for (int q = 0; q <= resV; q++) {
                sincos(PI*q / resV, out float sinq, out float cosq);

                // Embed a Mobius strip in the 3-sphere
                x = sinq * cos2p;
                y = sinq * sin2p;
                z = cosq * cosp;
                w = cosq * sinp;

                // Stereographic projection
                ys = (w + y) / sqrt(2f);
                ws = (w - y) / sqrt(2f);
                tempVerts[p,q] = new Vector3(
                    x / (1 - ws),
                    ys / (1 - ws),
                    z / (1 - ws)
                );
            }
        }
        return tempVerts;
    }
}
