using System.Collections;
using UnityEngine;

public class Plane : Complex {
    public Vector3 center;

    public override void Setup(Camera cam, int ResU, int ResV) {
        sideCount = 2;
        this.ResU = ResU;
        this.ResV = ResV;
        planar = true;
    }
    
    public override void GenerateVertices() {
        vertices = new Vector3[ResU+1, ResV+1];
        for (int p = 0; p <= ResU; p++) {
            for (int q = 0; q <= ResV; q++) {
                vertices[p,q] = new Vector3(
                    -p + ResU/2f,
                    0,
                    -q + ResV/2f
                ) / 2f;
            }
        }
    }
}
