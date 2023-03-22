using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plane : Complex {
    public new static Dictionary<string, Map> MapDict {
        get => new Dictionary<string, Map>(){
            {"Flat", Map.Flat}
        };
    }
    
    public override void Setup(int resU, int resV, Map initMap) {
        sideCount = 2;
        this.resU = resU;
        this.resV = resV;
        currentMap = initMap;
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

    public override Quad GetNeighbor(int u, int v) {
        if (u >= 0 && u < resU && v >= 0 && v < resV) return quads[u,v];
        else return new Quad(); // returns Quad of type Invalid
    }
}
