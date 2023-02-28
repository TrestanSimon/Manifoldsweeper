using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class Cylinder : Complex {
    // public new int ResU = 72, ResV = 8;
    public float R, tau;

    public override void Setup(Camera cam, int ResU, int ResV) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        R = ResU / 72f;
        tau = 16f / (float)ResV;
    }

    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            sincos(2*PI*u / ResU, out float sinu, out float cosu);

            for (int v = 0; v <= ResV; v++) {
                vertices[u,v] = new Vector3(
                    ResU/(2f*PI) * cosu,
                    ResU/(2f*PI) * sinu,
                    v
                );
                normals[u,v] = Vector3.up; // Change this
            }
        }
    }

    public override Quad GetNeighbor(int u, int v) {
        // Wraps around u
        int u1 = u >= 0 ? u % ResU : u + ResU;
        int v1 = v;

        if (u1 >= 0 && u1 < ResU && v1 >= 0 && v1 < ResV) { return quads[u1,v1]; }
        else { return new Quad(); }
    }
}
