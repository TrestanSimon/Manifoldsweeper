using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour {
    private Complex _complex;
    private Quad _mouseOver;
    private int _mineCount;

    private Material _materialUknown;
    private Material _materialEmpty;
    private Material _materialMine;
    private Material _materialExploded;
    private Material _materialFlag;
    private Material _materialNum1;
    private Material _materialNum2;
    private Material _materialNum3;
    private Material _materialNum4;
    private Material _materialNum5;
    private Material _materialNum6;
    private Material _materialNum7;
    private Material _materialNum8;
    private GameObject _flagPrefab, _breakPS;

    private bool _gameon, _gameover;

    private List<Coroutine> _coroutinePropagate = new List<Coroutine>();

    private void Awake() {
        // Load materials
        _materialUknown = Resources.Load("Materials/DesertMats/TileUnknown", typeof(Material)) as Material;
        _materialEmpty = Resources.Load("Materials/DesertMats/TileEmpty", typeof(Material)) as Material;
        _materialMine = Resources.Load("Materials/DesertMats/TileMine", typeof(Material)) as Material;
        _materialExploded = Resources.Load("Materials/DesertMats/TileExploded", typeof(Material)) as Material;
        _materialFlag = Resources.Load("Materials/DesertMats/TileNo", typeof(Material)) as Material;
        _materialNum1 = Resources.Load("Materials/DesertMats/Tile1", typeof(Material)) as Material;
        _materialNum2 = Resources.Load("Materials/DesertMats/Tile2", typeof(Material)) as Material;
        _materialNum3 = Resources.Load("Materials/DesertMats/Tile3", typeof(Material)) as Material;
        _materialNum4 = Resources.Load("Materials/DesertMats/Tile4", typeof(Material)) as Material;
        _materialNum5 = Resources.Load("Materials/DesertMats/Tile5", typeof(Material)) as Material;
        _materialNum6 = Resources.Load("Materials/DesertMats/Tile6", typeof(Material)) as Material;
        _materialNum7 = Resources.Load("Materials/DesertMats/Tile7", typeof(Material)) as Material;
        _materialNum8 = Resources.Load("Materials/DesertMats/Tile8", typeof(Material)) as Material;
        // Load prefabs
        _flagPrefab = Resources.Load("Prefabs/Flag", typeof(GameObject)) as GameObject;
        _breakPS = Resources.Load("Prefabs/BreakPS", typeof(GameObject)) as GameObject;
    }

    // Checks for user inputs every frame
    private void Update() {
        if (Input.GetKeyDown(KeyCode.R)) NewGame();
        else if (_gameover != true) {
            if (Input.GetMouseButtonDown(1)) AttemptFlagMouseOver();
            else if (Input.GetMouseButtonDown(0)) AttemptRevealMouseOver();
        }
    }

    public void Setup(Complex complex, int ResU, int ResV, int mineCount) {
        _complex = complex;
        _mineCount = Mathf.Clamp(mineCount, 0, ResU*ResV - 1);
    }

    public void NewGame(bool clearFlags = true) {
        foreach (Coroutine coroutine in _coroutinePropagate)
            if (coroutine != null) StopCoroutine(coroutine);

        _coroutinePropagate.Clear();
        
        _gameon = false;
        _gameover = false;

        foreach (Quad quad in _complex.Quads) {
            quad.type = Quad.Type.Empty;
            quad.Revealed = false;
            quad.Exploded = false;
            quad.Visited = false;
            if (clearFlags) quad.Flagged = false;
        }

        GenerateMines();
        GenerateNumbers();

        // Set all quad materials according to type/state
        foreach (Quad quad in _complex.Quads)
            quad.SetMaterial(GetMaterial(quad));
    }

    private void GenerateMines() {
        int u, v;
        for (int i = 0; i < _mineCount; i++) {
            u = Random.Range(0, _complex.ResU);
            v = Random.Range(0, _complex.ResV);

            // Check if Quad is already a mine
            while (_complex.Quads[u,v].type == Quad.Type.Mine) {
                u = Random.Range(0, _complex.ResU);
                v = Random.Range(0, _complex.ResV);
            }

            _complex.Quads[u,v].type = Quad.Type.Mine;
        }
    }

    private void GenerateNumbers() {
        for (int i = 0; i < _complex.ResU; i++) {
            for (int j = 0; j < _complex.ResV; j++) {
                if (_complex.Quads[i,j].type == Quad.Type.Mine) continue;
                _complex.Quads[i,j].Number = CountMines(_complex.Quads[i,j]);
            }
        }
    }

    private int CountMines(Quad quad) {
        int count = 0;
        for (int du = -1; du <= 1; du++) {
            for (int dv = -1; dv <= 1; dv++) {
                if (du == 0 && dv == 0) continue;
                if (_complex.GetNeighbor(quad.U + du, quad.V + dv).type == Quad.Type.Mine)
                    count++;
            }
        }
        return count;
    }

    private void AttemptFlagMouseOver() {
        _mouseOver = _complex.MouseIdentify();
        _mouseOver?.Flag(_flagPrefab, _materialFlag, _materialUknown);
    }

    private void AttemptRevealMouseOver() {
        _mouseOver = _complex.MouseIdentify();
        if (_mouseOver == null || _mouseOver.type == Quad.Type.Invalid
            || _mouseOver.Revealed || _mouseOver.Flagged) return;

        // Require first click to be an empty tile
        if (!_gameon)
            while (_mouseOver.type != Quad.Type.Empty)
                NewGame(false);

        switch (_mouseOver.type) {
            case Quad.Type.Mine:
                Explode(_mouseOver); break;
            case Quad.Type.Empty:
                Flood(_mouseOver);
                CheckWinCondition();
                _gameon = true;
                break;
            default:
                RevealQuad(_mouseOver);
                CheckWinCondition();
                _gameon = true;
                break;
        }
    }

    private void Explode(Quad quad) {
        Debug.Log("Game over");
        _gameover = true;
        _gameon = false;

        quad.Revealed = true;
        quad.Exploded = true;

        foreach (Quad quad1 in _complex.Quads) {
            if (quad1.type == Quad.Type.Mine)
                RevealQuad(quad1);
        }
    }

    private void Flood(Quad quad) {
        Queue<Quad> queue = new Queue<Quad>();

        // Reveal starting quad
        quad.Visited = true;
        quad.Depth = 0;
        RevealQuad(quad);

        queue.Enqueue(quad);

        while (queue.Any()) {
            quad = queue.Dequeue();

            List<Quad> neighbors = _complex.GetNeighbors(quad);

            foreach (Quad neighbor in neighbors) {
                if (!neighbor.Revealed && !neighbor.Visited) {
                    // Add empty neighbors to queue
                    if (neighbor.type == Quad.Type.Empty) {
                        neighbor.Visited = true;
                        queue.Enqueue(neighbor);
                    }

                    neighbor.Depth = quad.Depth + 1;
                    RevealQuad(neighbor);
                }
            }
        }
    }

    private void RevealQuad(Quad quad) {
        quad.Revealed = true;
        _coroutinePropagate.Add(StartCoroutine(
            quad.DelayedReveal(GetMaterial(quad), _breakPS)));
    }

    private void CheckWinCondition() {
        // Check if all non-mines have been revealed
        foreach (Quad quad in _complex.Quads)
            if (quad.type != Quad.Type.Mine
                && !quad.Revealed) return;

        // Flag all mines
        foreach (Quad quad in _complex.Quads)
            if (quad.type == Quad.Type.Mine)
                quad.Flagged = true;

        Debug.Log("Game win");
        _gameover = true;
        _gameon = false;
    }

    private Material GetMaterial(Quad quad) {
        if (quad.Revealed) return GetRevealedMaterial(quad);
        else if (quad.Flagged) return _materialFlag;
        else return _materialUknown;
    }

    private Material GetRevealedMaterial(Quad quad) {
        switch (quad.type) {
            case Quad.Type.Empty: return _materialEmpty;
            case Quad.Type.Mine: return quad.Exploded ? _materialExploded : _materialMine;
            case Quad.Type.Number: return GetNumberMaterial(quad);
            default: return null;
        }
    }

    private Material GetNumberMaterial(Quad quad) {
        switch (quad.Number) {
            case 1: return _materialNum1;
            case 2: return _materialNum2;
            case 3: return _materialNum3;
            case 4: return _materialNum4;
            case 5: return _materialNum5;
            case 6: return _materialNum6;
            case 7: return _materialNum7;
            case 8: return _materialNum8;
            default: return null;
        }
    }
}
