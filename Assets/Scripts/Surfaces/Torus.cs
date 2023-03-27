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
    // cf angleOffset in Cylinder.cs

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
        InitQuads();
    }
    
    protected override void InitVertices(Map map) {
        vertices = new Vector3[ResU+1,ResV+1];

        switch(map) {
            case Map.Flat: vertices = CylinderToPlaneMap(1, r); break;
            case Map.Torus: vertices = TorusToCylinderMap(0); break;
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        // Wraps around u and v
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v >= 0 ? v % ResV : v + ResV;

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) return quads[u1,v1];
        else return new Quad();
    }

    public override IEnumerator ReMap(Map newMap) {
        yield return StartCoroutine(ToPlane());
    }

    public IEnumerator TorusToCylinder(bool reverse = false) {
        float time = 0f;
        float duration = 1f;
        float progress;

        while (time < duration) {
            progress = reverse ? 1f - time/duration : time/duration;
            UpdateVertices(TorusToCylinderMap(progress));

            time += Time.deltaTime;
            yield return null;
        }

        // Finalize mapping
        vertices = TorusToCylinderMap(reverse ? 0f : 1f);
        UpdateVertices(vertices);
    }

    public IEnumerator CylinderToPlane(bool reverse = false) {
        float time = 0f;
        float duration = 1f;
        float progress;

        while (time < duration) {
            progress = time / duration;
            UpdateVertices(CylinderToPlaneMap(
                (reverse ? 1f - progress : progress), r));

            time += Time.deltaTime;
            yield return null;
        }
        // Finalize mapping
        vertices = CylinderToPlaneMap((reverse ? 0f : 1f), r);
        UpdateVertices(vertices);
    }

    // Maps from torus to cylinder
    private Vector3[,] TorusToCylinderMap(float progress) {
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];
        float a, t, minor, sinq, cosq, sinp, cosp;

        for (int p = 0; p < ResU+1; p++) {
            for (int q = 0; q < ResV+1; q++) {
                // Transformation follows involutes
                a = 2*PI*q/ResV; // Starting point
                t = (PI - a)*progress + a; // Involute curve parameter
                sincos(t, out sinq, out cosq);
                sincos(2*PI*p/ResU + minorOffset, out sinp, out cosp);
                minor = r * cosp
                    /sqrt(1 + (t - a)*(t - a)*(1 - progress)*(1 - progress));

                // In x and z, the first term gives the involutes of the circles
                // that wrap around the torus toroidally, and the second term
                // preserves the shape of the circles that wrap around poloidally
                tempVerts[p,q] = new Vector3(
                    R * (cosq + (t - a)*sinq)
                        + minor * (cosq + (t - a)*(1 - progress)*sinq)
                        + R*progress,
                    r * sinp,
                    R * (sinq - (t - a)*cosq)
                        + minor * (sinq - (t - a)*(1 - progress)*cosq)
                );
            }
        }
        return tempVerts;
    }

    public override IEnumerator ToPlane() {
        if (currentMap != Map.Flat) {
            yield return StartCoroutine(TorusToCylinder());
            yield return StartCoroutine(CylinderToPlane());
            currentMap = Map.Flat;
        } else {
            yield return StartCoroutine(CylinderToPlane(true));
            yield return StartCoroutine(TorusToCylinder(true));
            currentMap = Map.Torus;
        }
    }
}
