using System.Collections;
using UnityEngine;

using static Unity.Mathematics.math;

public class Torus : Complex {
    public enum Map {
        Planar,
        Donut
    }
    public float r, R;
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

    public override void Setup(int ResU, int ResV) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        r = this.ResU / 16f;
        R = this.ResV / 16f;
        planar = false;
        GenerateVertices();
        GenerateQuads();
    }
    
    protected override void GenerateVertices() {
        vertices = new Vector3[ResU+1, ResV+1];
        for (int p = 0; p <= ResU; p++) {
            sincos(2*PI*p/ResU + minorOffset, out float sinp, out float cosp);
            float minor = R + r*cosp;

            for (int q = 0; q <= ResV; q++) {
                sincos(2*PI*q/ResV, out float sinq, out float cosq);

                vertices[p,q] = new Vector3(
                    minor * cosq,
                    r * sinp,
                    minor * sinq
                );
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        // Wraps around u and v
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v >= 0 ? v % ResV : v + ResV;

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) { return quads[u1,v1]; }
        else { return new Quad(); }
    }

    public IEnumerator TorusToCylinder(bool reverse = false) {
        float time = 0f;
        float duration = 1f;
        float progress;
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];

        while (time < duration) {
            progress = time/duration;
            UpdateVertices(TorusToCylinderMap(
                reverse ? 1f - progress : progress));

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
        float a, t, minor, sinq, cosq;

        for (int p = 0; p < ResU+1; p++) {
            for (int q = 0; q < ResV+1; q++) {
                // Transformation follows involutes
                a = 2*PI*q/ResV; // Starting point
                t = (PI - a)*progress + a; // Involute curve parameter
                minor = r * cos(2*PI*p/ResU + minorOffset)
                    /sqrt(1 + (t - a)*(t - a)*(1 - progress)*(1 - progress));
                sincos(t, out sinq, out cosq);

                // In x and z, the first term gives the involutes of the circles
                // that wrap around the torus toroidally, and the second term
                // preserves the shape of the circles that wrap around poloidally
                tempVerts[p,q] = new Vector3(
                    R * (cosq + (t - a)*sinq)
                        + minor * (cosq + (t - a)*(1 - progress)*sinq)
                        + R*progress,
                    vertices[p,q].y,
                    R * (sinq - (t - a)*cosq)
                        + minor * (sinq - (t - a)*(1 - progress)*cosq)
                );
            }
        }
        return tempVerts;
    }

    public override IEnumerator ToPlane() {
        if (planar) {
            yield return StartCoroutine(CylinderToPlane(true));
            yield return StartCoroutine(TorusToCylinder(true));
        } else {
            yield return StartCoroutine(TorusToCylinder());
            yield return StartCoroutine(CylinderToPlane());
        }
        planar = !planar;
    }
}
