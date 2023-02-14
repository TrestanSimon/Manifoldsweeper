using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour {
    Complex complex;
    Quad[,] quads;
    private int ResU, ResV;
    private bool gameon;
    private bool gameover;

    private void Awake() {
        complex = GetComponent<Complex>();
        ResU = complex.ResU;
        ResV = complex.ResV;
    }

    private void Start() {
        NewGame();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) { NewGame(); }
        else if (gameover != true) {
            if (Input.GetMouseButtonDown(1)) {
                // Flag();
            } else if (Input.GetMouseButtonDown(0)) {
                // Reveal();
            }
        }
    }

    private void NewGame() {
        gameon = false;
        gameover = false;
    }

    private void GenerateField() {
        Quad quad;

        // Reset all Quad instances
        for (int i = 0; i < ResU; i++) {
            for (int j = 0; j < ResV; j++) {
                quad = quads[i,j];
                quad.type = Quad.Type.Empty;
                quad.revealed = false;
                quad.flagged = false;
                quad.exploded = false;
            }
        }
        
        // Destroy all flags
        foreach (KeyValuePair<Vector2Int, GameObject> flag in complex.flags) {
            Destroy(flag.Value);
        }
        complex.flags.Clear();

        // GenerateMines();
        // GenerateNumbers();
        // Draw(quads);
    }
}
