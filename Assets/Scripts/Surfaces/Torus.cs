using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Torus : Complex {
    public new static Dictionary<string, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat},
            {"Torus", Map.Torus}
        };
    }
    
    private float r, R;
    private float minorOffset = PI/2f; // Added to 2*PI*p/ResU
    // Necessary so that the p-u seam is at the top of the torus
    // and the mapping to a plane breaks the cylinder at this seam

    public override int ResU {
        get => resU;
        protected set {
            resU = Mathf.Clamp(value, 3, 99);
        }
    }
    public override int ResV {
        get => resV;
        protected set {
            resV = Mathf.Clamp(value, 3, 99);
        }
    }

    public override void Setup(int ResU, int ResV, Map initMap) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        r = this.ResU / 16f;
        R = this.ResV / 16f;
        currentMap = initMap;
        InitVertices(initMap);
        InitTiles();
    }
    
    protected override void InitVertices(Map map) {
        switch(map) {
            case Map.Flat: vertices = CylinderInvoluteMap(1, r); break;
            case Map.Torus: vertices = TorusInvolutesMap(0); break;
        }
    }

    public override Tile GetNeighbor(int u, int v) {
        // Wraps around u and v
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v >= 0 ? v % ResV : v + ResV;

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) return tiles[u1,v1];
        else return new Tile();
    }

    public override IEnumerator ReMap(Map newMap) {
        if (newMap == currentMap) yield return null;
        else if (newMap == Map.Flat) {
            yield return StartCoroutine(TorusToCylinder());
            yield return StartCoroutine(CylinderToPlane());
        } else if (newMap == Map.Torus) {
            yield return StartCoroutine(CylinderToPlane(true));
            yield return StartCoroutine(TorusToCylinder(true));
        }
        currentMap = newMap;
    }

    public IEnumerator TorusToCylinder(bool reverse = false) {
        float time = 0f;
        float duration = 1f;
        float progress;

        while (time < duration) {
            progress = reverse ? 1f - time/duration : time/duration;
            UpdateVertices(TorusInvolutesMap(progress));

            time += Time.deltaTime;
            yield return null;
        }

        // Finalize mapping
        vertices = TorusInvolutesMap(reverse ? 0f : 1f);
        UpdateVertices(vertices);
    }

    public IEnumerator CylinderToPlane(bool reverse = false) {
        float time = 0f;
        float duration = 1f;
        float progress;

        while (time < duration) {
            progress = time / duration;
            UpdateVertices(CylinderInvoluteMap(
                (reverse ? 1f - progress : progress), r));

            time += Time.deltaTime;
            yield return null;
        }
        // Finalize mapping
        vertices = CylinderInvoluteMap((reverse ? 0f : 1f), r);
        UpdateVertices(vertices);
    }

    // Maps from torus to cylinder
    private Vector3[,] TorusInvolutesMap(float progress) {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        float p1, q1, t, minor, sinq, cosq, sinp, cosp;

        for (int p = 0; p < ResU+1; p++) {
            p1 = 2*PI*p/ResU + minorOffset;
            for (int q = 0; q < ResV+1; q++) {
                q1 = 2*PI*q/ResV;
                // Transformation follows involutes
                t = (PI - q1)*progress + q1; // Involute curve parameter
                sincos(t, out sinq, out cosq);
                sincos(p1, out sinp, out cosp);
                minor = r * cosp
                    /sqrt(1 + (t - q1)*(t - q1)*(1 - progress)*(1 - progress));

                // In x and z, the first term gives the involutes of the circles
                // that wrap around the torus toroidally, and the second term
                // preserves the shape of the circles that wrap around poloidally
                tempVerts[p,q] = new Vector3(
                    R * (cosq + (t - q1)*sinq)
                        + minor * (cosq + (t - q1)*(1 - progress)*sinq)
                        + R*progress,
                    r * sinp,
                    R * (sinq - (t - q1)*cosq)
                        + minor * (sinq - (t - q1)*(1 - progress)*cosq)
                );
            }
        }
        return tempVerts;
    }
}
