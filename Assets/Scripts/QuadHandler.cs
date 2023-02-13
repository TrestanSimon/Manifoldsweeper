using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadHandler : MonoBehaviour {
    public Material materialUknown;
    public Material materialEmpty;
    public Material materialMine;
    public Material materialExploded;
    public Material materialFlag;
    public Material materialNum1;
    public Material materialNum2;
    public Material materialNum3;
    public Material materialNum4;
    public Material materialNum5;
    public Material materialNum6;
    public Material materialNum7;
    public Material materialNum8;
    public Material materialScale;
    public Material materialLip;

    public void Draw(Quad[,] quads) {
        int m = quads.GetLength(0);
        int n = quads.GetLength(1);
        Quad quad;

        for (int i = 0; i < m; i++) {
            for (int j = 0; j < n; j++) {
                quad = quads[i,j];
                quad.SetMaterial(GetState(quad));
            }
        }
    }

    private Material GetState(Quad quad) {
        if (quad.revealed) { return GetRevealed(quad); }
        else if (quad.flagged) { return materialFlag; }
        else { return materialUknown; }
    }

    private Material GetRevealed(Quad quad) {
        switch (quad.type) {
            case Quad.Type.Empty: return materialEmpty;
            case Quad.Type.Mine: return quad.exploded ? materialExploded : materialMine;
            case Quad.Type.Number: return GetNumber(quad);
            default: return null;
        }
    }

    private Material GetNumber(Quad quad) {
        switch (quad.number) {
            case 1: return materialNum1;
            case 2: return materialNum2;
            case 3: return materialNum3;
            case 4: return materialNum4;
            case 5: return materialNum5;
            case 6: return materialNum6;
            case 7: return materialNum7;
            case 8: return materialNum8;
            default: return null;
        }
    }
}
