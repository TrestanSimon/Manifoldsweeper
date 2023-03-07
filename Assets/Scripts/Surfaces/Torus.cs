using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Torus : Complex {
    public float r, R;
    private float minorOffset = PI/2f; // Added to 2*PI*p/ResU
    // Necessary so that the p-u seam is at the top of the torus
    // and the mapping to a plane breaks the cylinder at this seam
    // cf angleOffset in Cylinder.cs
    private float tu = 0f, tv = 0f;
    private float zoom = 10f;
    private Vector3 circleMajor = Vector3.zero;
    private Vector3 circleMinor = Vector3.zero;

    public override void Setup(Camera cam, int ResU, int ResV) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        r = ResU / 16f;
        R = ResV / 16f;
        UpdateCamera(cam, true);
    }
    
    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
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
                normals[p,q] = new Vector3(
                    cosp * cosq,
                    sinp,
                    cosp * sinq
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

    public override void UpdateCamera(Camera cam, bool force = false) {
        scroll = Input.mouseScrollDelta.y * sensitivity * -1f;
        if (Input.GetMouseButtonDown(0)) {
            mousePos = Input.mousePosition;
        }
        if ((Input.GetMouseButton(0) && mousePos != null) || force) {
            dmousePos = Input.mousePosition - mousePos;

            tu -= clamp(dmousePos.y/300f, -30f, 30f) * Time.deltaTime;
            sincos(tu, out float sinu, out float cosu);
            tv -= clamp(dmousePos.x/300f, -30f, 30f) * Time.deltaTime;
            sincos(tv, out float sinv, out float cosv);

            // Major (toroidal) and minor (poloidal) circles
            circleMajor = new Vector3(R * cosv, 0f, R * sinv);
            circleMinor = new Vector3(r * cosu * cosv, r * sinu, r * cosu * sinv);
        }
        if (scroll != 0f) {
            zoom += scroll;
            zoom = clamp(zoom, 2f, 20f);
        }
        cam.transform.position = circleMajor + zoom * circleMinor;
        cam.transform.LookAt(circleMajor);
    }

    public IEnumerator TorusToCylinder() {
        float time = 0f;
        float duration = 1f;
        float progress = 0f;
        Vector3[,] tempVerts = new Vector3[ResU+1,ResV+1];

        while (time < duration) {
            progress = time/duration;
            UpdateVertices(TorusToCylinderMap(progress));

            time += Time.deltaTime;
            yield return null;
        }

        // Finalize mapping
        vertices = TorusToCylinderMap(1);
        UpdateVertices(vertices);
    }

    public IEnumerator CylinderToPlane() {
        float time = 0f;
        float duration = 1f;
        float progress = 0f;

        while (time < duration) {
            progress = time / duration;
            UpdateVertices(CylinderToPlaneMap(progress, r, R));

            time += Time.deltaTime;
            yield return null;
        }
        // Finalize mapping
        vertices = CylinderToPlaneMap(1, r, R);
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
                        + minor * (cosq + (t - a)*(1 - progress)*sinq),
                    vertices[p,q].y,
                    R * (sinq - (t - a)*cosq)
                        + minor * (sinq - (t - a)*(1 - progress)*cosq)
                );
            }
        }
        return tempVerts;
    }

    public override IEnumerator ToPlane() {
        yield return StartCoroutine(TorusToCylinder());
        yield return StartCoroutine(CylinderToPlane());
    }
}
