using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class MobiusStrip : Complex {
    // public new int ResU = 72, ResV = 8;
    public float R = 1f, tau = 2f;

    public override void Awake() {
        sideCount = 2;
    }

    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            sincos(2*PI*u / ResU, out float sinu, out float cosu);
            sincos(PI*u / ResU, out float sinu2, out float cosu2);

            for (int v = 0; v <= ResV; v++) {
                float v1 = v / (float)ResV - 0.5f;
                float minor = R + v1/tau * cosu2;

                vertices[u,v] = 5f * new Vector3(
                    minor * cosu,
                    v1/tau * sinu2,
                    minor * sinu
                );
                normals[u,v] = Vector3.up; // Change this
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v;
        if (u >= ResU || u < 0) {
            v1 = ResV - (v1 + 1);
        }

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) { return quads[u1,v1]; }
        else { return new Quad(); }
    }
}
