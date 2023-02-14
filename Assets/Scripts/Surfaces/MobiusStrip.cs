using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static Unity.Mathematics.math;

public class MobiusStrip : Complex {
    public override void GenerateVertices() {
        vertices = new Vector3[ResU + 1, ResV + 1];
        normals = new Vector3[ResU + 1, ResV + 1];
        for (int u = 0; u <= ResU; u++) {
            sincos(2*PI*u / ResU, out float sinu, out float cosu);
            sincos(PI*u / ResU, out float sinu2, out float cosu2);

            for (int v = 0; v <= ResV; v++) {
                float v1 = v / (float)ResV - 0.5f;
                float minor = 1f + v1/2f * cosu2;

                vertices[u,v] = 5f * new Vector3(
                    minor * cosu,
                    v1/2f * sinu2,
                    minor * sinu
                );
                normals[u,v] = Vector3.up; // Change this
            }
        }
    }
}
