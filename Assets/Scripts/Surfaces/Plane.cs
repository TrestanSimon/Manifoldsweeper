using System.Collections;
using UnityEngine;

public class Plane : Complex {
    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        planar = true;
        GenerateVertices(initMap);
        GenerateQuads();
    }
    
    protected override void GenerateVertices(Map map) {
        vertices = new Vector3[resU+1, resV+1];
        for (int p = 0; p <= resU; p++) {
            for (int q = 0; q <= resV; q++) {
                vertices[p,q] = new Vector3(
                    -p + resU/2f,
                    0,
                    -q + resV/2f
                ) / 2f;
            }
        }
    }
}
