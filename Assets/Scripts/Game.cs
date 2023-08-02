using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Game : MonoBehaviour {
    private Complex _complex;
    private Tile _mouseOver;
    private int _mineCount, _flagCount;
    private float _timer;

    private bool _gameOn, _gameLost, _gameWon, _paused;

    private Material _materialUknown;
    private Material _materialMine;
    private Material _materialExploded;
    private Material _materialNum1;
    private Material _materialNum2;
    private Material _materialNum3;
    private Material _materialNum4;
    private Material _materialNum5;
    private Material _materialNum6;
    private Material _materialNum7;
    private Material _materialNum8;
    private Material _flagMaterial;
    private Dictionary<string, Material> _emptyMaterials;
    private GameObject _flagPrefab, _breakPS, _breakPSVol;

    private List<Coroutine> _coroutinePropagate = new List<Coroutine>();

    public int MineCount { get => _mineCount;}
    public int FlagCount { get => _flagCount; }
    public float Timer { get => _timer; }
    public bool GameOn { get => _gameOn; }
    public bool GameLost { get => _gameLost; }
    public bool GameWon { get => _gameWon; }
    public bool Paused {
        get => _paused;
        set => _paused = value;
    }

    private void Awake() {
        // Load materials
        _materialUknown = Resources.Load($"Materials/TileCloud", typeof(Material)) as Material;
        _materialMine = Resources.Load("Materials/NonEmptyTiles/TileMine", typeof(Material)) as Material;
        _materialExploded = Resources.Load("Materials/NonEmptyTiles/TileExploded", typeof(Material)) as Material;
        _materialNum1 = Resources.Load("Materials/NonEmptyTiles/Tile1", typeof(Material)) as Material;
        _materialNum2 = Resources.Load("Materials/NonEmptyTiles/Tile2", typeof(Material)) as Material;
        _materialNum3 = Resources.Load("Materials/NonEmptyTiles/Tile3", typeof(Material)) as Material;
        _materialNum4 = Resources.Load("Materials/NonEmptyTiles/Tile4", typeof(Material)) as Material;
        _materialNum5 = Resources.Load("Materials/NonEmptyTiles/Tile5", typeof(Material)) as Material;
        _materialNum6 = Resources.Load("Materials/NonEmptyTiles/Tile6", typeof(Material)) as Material;
        _materialNum7 = Resources.Load("Materials/NonEmptyTiles/Tile7", typeof(Material)) as Material;
        _materialNum8 = Resources.Load("Materials/NonEmptyTiles/Tile8", typeof(Material)) as Material;
        _flagMaterial = Resources.Load("Materials/TileFlag", typeof(Material)) as Material;
        _emptyMaterials = new Dictionary<string, Material>();
        
        // Load prefabs
        _flagPrefab = Resources.Load("Prefabs/Flag", typeof(GameObject)) as GameObject;
        _breakPS = Resources.Load("Prefabs/BreakPS", typeof(GameObject)) as GameObject;
        _breakPSVol = Resources.Load("Prefabs/BreakPSVol", typeof(GameObject)) as GameObject;
    }

    // Checks for user inputs every frame
    private void Update() {
        if (!_paused) {
            if (Input.GetKeyDown(KeyCode.R)) NewGame();
            else if (!_gameLost && !_gameWon) {
                if (Input.GetMouseButtonDown(1)) AttemptFlagMouseOver();
                else if (Input.GetMouseButtonDown(0)) AttemptRevealMouseOver();
            }
            if (_gameOn) _timer += Time.deltaTime;
        }
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

        foreach (Tile tile in _complex.Tiles)
            tile.Reset(clearFlags);

        GenerateMines();
        GenerateNumbers();

        // Set all quad materials according to type/state
        foreach (Tile tile in _complex.Tiles)
            if (!tile.Flagged)
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
            _flagCount += _mouseOver.FlagToggle(_flagPrefab, _flagMaterial);
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
                ExplodeTile(_mouseOver); break;
            case Tile.Type.Empty:
                FloodTile(_mouseOver);
                if (!CheckWinCondition()) _gameOn = true;
                break;
            default:
                RevealTile(_mouseOver);
                if (!CheckWinCondition()) _gameOn = true;
                break;
        }
    }

    private void ExplodeTile(Tile tile) {
        Debug.Log("Game over");
        _gameLost = true;
        _gameOn = false;

        tile.Exploded = true;

        RevealTile(tile);

        foreach (Tile tile1 in _complex.Tiles) {
            if (tile1.type == Tile.Type.Mine)
                RevealTile(tile1);
        }
    }

    private void FloodTile(Tile tile) {
        Queue<Tile> queue = new Queue<Tile>();

        // Reveal starting quad
        tile.Visited = true;
        tile.Depth = 0;
        RevealTile(tile);

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
                    RevealTile(neighbor);
                }
            }
        }
    }

    private void RevealTile(Tile tile) {
        if (tile.Exploded)
            _coroutinePropagate.Add(StartCoroutine(
                tile.DelayedReveal(GetRevealedMaterial(tile), _breakPSVol)));
        else if (_complex.CurrentMap != Complex.Map.Flat)
            _coroutinePropagate.Add(StartCoroutine(
                tile.DelayedReveal(GetRevealedMaterial(tile), _breakPS)));
        else
            _coroutinePropagate.Add(StartCoroutine(
                tile.DelayedReveal(GetRevealedMaterial(tile), null)));
    }

    private bool CheckWinCondition() {
        // Check if all non-mines have been revealed
        foreach (Tile tile in _complex.Tiles)
            if (tile.type != Tile.Type.Mine && !tile.Revealed)
                return false;

        Debug.Log("Game win");
        _gameWon = true;
        _gameOn = false;
        Debug.Log($"{_gameOn} and {_gameWon}");

        // Flag all mines
        foreach (Tile tile in _complex.Tiles)
            if (tile.type == Tile.Type.Mine)
                tile.PlaceFlags(_flagPrefab, _flagMaterial);
                _flagCount = MineCount;
        
        return true;
    }

    private Material GetMaterial(Tile tile) {
        if (tile.Revealed) return GetRevealedMaterial(tile);
        else return _materialUknown;
    }

    private Material GetRevealedMaterial(Tile tile) {
        switch (tile.type) {
            case Tile.Type.Empty: return GetEmptyMaterial(tile);
            case Tile.Type.Mine: return tile.Exploded ? _materialExploded : _materialMine;
            case Tile.Type.Number: return GetNumberMaterial(tile);
            default: return null;
        }
    }

    private Material GetEmptyMaterial(Tile tile) {
        List<Tile> neighbors = _complex.GetNeighbors(tile, false);
        bool[] beach = new bool[8];
        string name = "";

        for (int i = 0; i < 8; i++) {
            if (neighbors[i].type != Tile.Type.Empty
                && neighbors[i].type != Tile.Type.Invalid) {
                beach[i] = true;
            } else beach[i] = false;
        }

        if (beach[1]) beach[0] = beach[2] = true;
        if (beach[4]) beach[2] = beach[7] = true;
        if (beach[6]) beach[7] = beach[5] = true;
        if (beach[3]) beach[5] = beach[0] = true;

        for (int i = 0; i < 8; i++)
            if (beach[i]) name += $"{i}";

        // Only loads/caches materials when needed
        if (!_emptyMaterials.ContainsKey(name))
            _emptyMaterials.Add(name, Resources.Load(
                $"Materials/EmptyTiles/TileEmpty" + name,
                typeof(Material)) as Material);

        return _emptyMaterials[name];
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
