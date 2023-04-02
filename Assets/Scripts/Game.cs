using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour {
    private Complex _complex;
    private Tile _mouseOver;
    private int _mineCount, _flagCount;
    private float _timer;

    private bool _gameOn, _gameLost, _gameWon;

    private Material _materialUknown;
    private Material[] _materialEmpty;
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

    private List<Coroutine> _coroutinePropagate = new List<Coroutine>();

    public int MineCount { get => _mineCount;}
    public int FlagCount { get => _flagCount; }
    public float Timer { get => _timer; }
    public bool GameOn { get => _gameOn; }
    public bool GameLost { get => _gameLost; }
    public bool GameWon { get => _gameWon; }

    private void Awake() {
        // Load materials
        _materialUknown = Resources.Load("Materials/OceanMats/TileCloud", typeof(Material)) as Material;
        _materialEmpty = new Material[]{
            Resources.Load("Materials/OceanMats/TileOcean", typeof(Material)) as Material,
            Resources.Load("Materials/OceanMats/TileOcean2", typeof(Material)) as Material,
        };
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
        else if (!_gameLost && !_gameWon) {
            if (Input.GetMouseButtonDown(1)) AttemptFlagMouseOver();
            else if (Input.GetMouseButtonDown(0)) AttemptRevealMouseOver();
        }
        if (_gameOn) _timer += Time.deltaTime;
    }

    public void Setup(Complex complex, int ResU, int ResV, int mineCount) {
        _complex = complex;
        _mineCount = Mathf.Clamp(mineCount, 0, ResU*ResV - 1);
        _flagCount = 0;
        _timer = 0f;
    }

    public void NewGame(bool clearFlags = true) {
        foreach (Coroutine coroutine in _coroutinePropagate)
            if (coroutine != null) StopCoroutine(coroutine);

        _coroutinePropagate.Clear();
        
        _gameOn = false;
        _gameLost = false;
        _gameWon = false;
        _timer = 0f;
        if (clearFlags) _flagCount = 0;

        foreach (Tile tile in _complex.Tiles) {
            tile.type = Tile.Type.Empty;
            tile.Revealed = false;
            tile.Exploded = false;
            tile.Visited = false;
            if (clearFlags) tile.Flagged = false;
        }

        GenerateMines();
        GenerateNumbers();

        // Set all quad materials according to type/state
        foreach (Tile tile in _complex.Tiles)
            tile.SetMaterial(GetMaterial(tile));
    }

    private void GenerateMines() {
        int u, v;
        for (int i = 0; i < _mineCount; i++) {
            u = Random.Range(0, _complex.ResU);
            v = Random.Range(0, _complex.ResV);

            // Check if Quad is already a mine
            while (_complex.Tiles[u,v].type == Tile.Type.Mine) {
                u = Random.Range(0, _complex.ResU);
                v = Random.Range(0, _complex.ResV);
            }

            _complex.Tiles[u,v].type = Tile.Type.Mine;
        }
    }

    private void GenerateNumbers() {
        for (int i = 0; i < _complex.ResU; i++) {
            for (int j = 0; j < _complex.ResV; j++) {
                if (_complex.Tiles[i,j].type == Tile.Type.Mine) continue;
                _complex.Tiles[i,j].Number = CountMines(_complex.Tiles[i,j]);
            }
        }
    }

    private int CountMines(Tile tile) {
        int count = 0;
        for (int du = -1; du <= 1; du++) {
            for (int dv = -1; dv <= 1; dv++) {
                if (du == 0 && dv == 0) continue;
                if (_complex.GetNeighbor(tile.U + du, tile.V + dv).type == Tile.Type.Mine)
                    count++;
            }
        }
        return count;
    }

    private void AttemptFlagMouseOver() {
        _mouseOver = _complex.MouseIdentify();
        if (_mouseOver != null)
            _flagCount += _mouseOver.FlagToggle(
                _flagPrefab, _materialFlag, _materialUknown);
    }

    private void AttemptRevealMouseOver() {
        _mouseOver = _complex.MouseIdentify();
        if (_mouseOver == null || _mouseOver.type == Tile.Type.Invalid
            || _mouseOver.Revealed || _mouseOver.Flagged) return;

        // Require first click to be an empty tile
        if (!_gameOn)
            while (_mouseOver.type != Tile.Type.Empty)
                NewGame(false);

        switch (_mouseOver.type) {
            case Tile.Type.Mine:
                Explode(_mouseOver); break;
            case Tile.Type.Empty:
                Flood(_mouseOver);
                if (!CheckWinCondition()) _gameOn = true;
                break;
            default:
                RevealQuad(_mouseOver);
                if (!CheckWinCondition()) _gameOn = true;
                break;
        }
    }

    private void Explode(Tile tile) {
        Debug.Log("Game over");
        _gameLost = true;
        _gameOn = false;

        tile.Revealed = true;
        tile.Exploded = true;

        foreach (Tile tile1 in _complex.Tiles) {
            if (tile1.type == Tile.Type.Mine)
                RevealQuad(tile1);
        }
    }

    private void Flood(Tile tile) {
        Queue<Tile> queue = new Queue<Tile>();

        // Reveal starting quad
        tile.Visited = true;
        tile.Depth = 0;
        RevealQuad(tile);

        queue.Enqueue(tile);

        while (queue.Any()) {
            tile = queue.Dequeue();

            List<Tile> neighbors = _complex.GetNeighbors(tile);

            foreach (Tile neighbor in neighbors) {
                if (!neighbor.Revealed && !neighbor.Visited && !neighbor.Flagged) {
                    // Add empty neighbors to queue
                    if (neighbor.type == Tile.Type.Empty) {
                        neighbor.Visited = true;
                        queue.Enqueue(neighbor);
                    }

                    neighbor.Depth = tile.Depth + 1;
                    RevealQuad(neighbor);
                }
            }
        }
    }

    private void RevealQuad(Tile tile) {
        tile.Revealed = true;
        _coroutinePropagate.Add(StartCoroutine(
            tile.DelayedReveal(GetMaterial(tile), _breakPS)));
    }

    private bool CheckWinCondition() {
        // Check if all non-mines have been revealed
        foreach (Tile tile in _complex.Tiles)
            if (tile.type != Tile.Type.Mine
                && !tile.Revealed) return false;

        Debug.Log("Game win");
        _gameWon = true;
        _gameOn = false;
        Debug.Log($"{_gameOn} and {_gameWon}");

        // Flag all mines
        foreach (Tile tile in _complex.Tiles)
            if (tile.type == Tile.Type.Mine)
                _flagCount += tile.Flag(_flagPrefab, _materialFlag);
        
        return true;
    }

    private Material GetMaterial(Tile tile) {
        if (tile.Revealed) return GetRevealedMaterial(tile);
        else if (tile.Flagged) return _materialFlag;
        else return _materialUknown;
    }

    private Material GetRevealedMaterial(Tile tile) {
        switch (tile.type) {
            case Tile.Type.Empty: return _materialEmpty[0];
            case Tile.Type.Mine: return tile.Exploded ? _materialExploded : _materialMine;
            case Tile.Type.Number: return GetNumberMaterial(tile);
            default: return null;
        }
    }

    private Material GetNumberMaterial(Tile tile) {
        switch (tile.Number) {
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
