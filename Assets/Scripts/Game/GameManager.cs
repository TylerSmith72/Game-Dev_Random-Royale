using System.Collections;
using System.Collections.Generic;
using FishNet.Component.Spawning;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    // Game state enum
    public enum GameState
    {
        WaitingForPlayers,
        Starting,
        Playing,
        GameOver
    }

    // Prefab and manager references
    public GameObject playerPrefab;
    public PlayerSpawner playerSpawner;
    public GameObject terrainManagerObject;

    // UI References
    [SerializeField] private TextMeshProUGUI gameStateText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI winnerText;

    // Game settings
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float gameStartCountdown = 10f;
    [SerializeField] private float gameOverDelay = 3f;
    [SerializeField] private float respawnDelay = 5f;

    // Private variables for tracking game state
    public GameState _currentState = GameState.WaitingForPlayers;
    private Dictionary<NetworkConnection, GameObject> _registeredPlayers = new Dictionary<NetworkConnection, GameObject>();
    private List<NetworkConnection> _alivePlayers = new List<NetworkConnection>();
    public List<NetworkConnection> _deadPlayers = new List<NetworkConnection>();
    public List<int> deadPlayerIds = new List<int>(); // Client-side list of dead player IDs
    private NetworkConnection _lastPlayerStanding;
    private float _countdownTimer;
    private Coroutine _gameStartCoroutine;
    private int _currentPlayerCount = 0;

    public static GameManager Instance { get; private set; }
    public GameState CurrentState => _currentState;

    public void Awake()
    {
        Instance = this;

        terrainManagerObject = FindObjectOfType<SeedGenerator>()?.gameObject;
        Debug.Log("TerrainManager found: " + (terrainManagerObject != null));

        // Hide the game over panel initially
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Set initial UI text
        UpdateGameStateUI();
    }

    // public override void OnStartServer()
    // {
    //     base.OnStartServer();
    //     Debug.Log("GameManager started on server");

    //     // Initialize the game state on the server
    //     SetGameState(GameState.WaitingForPlayers);
    // }

    // Called when a player is spawned and registered
    public void RegisterPlayer(GameObject player)
    {
        Debug.Log("Registering player in GameManager: " + player);

        // Set references
        player.GetComponent<PlayerSetup>().SetGameManager(this.gameObject);

        // If we're the server, add this player to our tracking
        if (IsServerInitialized)
        {
            NetworkConnection conn = player.GetComponent<NetworkBehaviour>().Owner;
            if (!_registeredPlayers.ContainsKey(conn))
            {
                _registeredPlayers.Add(conn, player);
                _alivePlayers.Add(conn);

                // Update player count
                _currentPlayerCount = _registeredPlayers.Count;
                UpdatePlayerCountClient(_currentPlayerCount);

                Debug.Log($"Player registered. Total players: {_currentPlayerCount}");

                // Check if we should start the game
                CheckGameStart();
            }
        }
    }

    // Server method to check if game should start
    private void CheckGameStart()
    {
        if (IsServerInitialized && _currentState == GameState.WaitingForPlayers && _currentPlayerCount >= minPlayersToStart)
        {
            Debug.Log("Minimum players reached, starting countdown");
            SetGameState(GameState.Starting);

            // Start the countdown coroutine
            if (_gameStartCoroutine != null)
            {
                StopCoroutine(_gameStartCoroutine);
            }
            _gameStartCoroutine = StartCoroutine(StartGameCountdown());
        }
    }

    // Countdown coroutine before game starts
    private IEnumerator StartGameCountdown()
    {
        _countdownTimer = gameStartCountdown;

        while (_countdownTimer > 0)
        {
            UpdateCountdownClient((int)_countdownTimer);
            yield return new WaitForSeconds(1f);
            _countdownTimer--;
        }

        // Start the game when countdown reaches zero
        SetGameState(GameState.Playing);
        UpdateCountdownClient(0); // Clear countdown
        StartGameOnAllClients();
    }

    // Change the game state
    private void SetGameState(GameState newState)
    {
        if (!IsServerInitialized) return;

        _currentState = newState;
        Debug.Log($"Game state changed to: {_currentState}");

        // Update clients about the new state
        UpdateGameStateClient(_currentState.ToString());
    }

    // Check if game is over (only one player alive)
    private void CheckGameOver()
    {
        if (!IsServerInitialized || _currentState != GameState.Playing) return;

        // If only one player is alive or no players alive, end the game
        if (_alivePlayers.Count == 1)
        {
            _lastPlayerStanding = _alivePlayers[0];
            StartCoroutine(EndGame());
        }
        else if (_alivePlayers.Count == 0)
        {
            StartCoroutine(EndGame());
        }
    }

    // End game coroutine
    private IEnumerator EndGame()
    {
        SetGameState(GameState.GameOver);

        // Display winner info
        string winnerName = "No winner";
        if (_lastPlayerStanding != null && _registeredPlayers.ContainsKey(_lastPlayerStanding))
        {
            winnerName = _registeredPlayers[_lastPlayerStanding].name;
        }

        ShowGameOverClient(winnerName);

        // Wait for a delay then restart the game
        yield return new WaitForSeconds(gameOverDelay);

        // Reset for a new game
        ResetGame();
    }

    // Reset the game state for a new round
    private void ResetGame()
    {
        if (!IsServerInitialized) return;

        // Respawn all players
        foreach (var player in _registeredPlayers)
        {
            ServerRequestRespawn(player.Key);
        }

        // Clear lists and reset game state
        _deadPlayers.Clear();
        _alivePlayers.Clear();
        foreach (var conn in _registeredPlayers.Keys)
        {
            _alivePlayers.Add(conn);
        }

        // Wait for players to join again if needed
        SetGameState(GameState.WaitingForPlayers);
        HideGameOverClient();

        // Check if we already have enough players to start again
        CheckGameStart();
    }

    // Handle player death
    public void PlayerDied(NetworkConnection conn)
    {
        if (!IsServerInitialized) return;

        if (_alivePlayers.Contains(conn))
        {
            _alivePlayers.Remove(conn);
            _deadPlayers.Add(conn);
            Debug.Log($"Player died. Remaining players: {_alivePlayers.Count}");

            // Check if the game is over
            CheckGameOver();

            // Schedule respawn after delay
            //StartCoroutine(RespawnAfterDelay(conn));
        }
    }

    // Client calls this to request the dead players list
    [ServerRpc(RequireOwnership = false)]
    public void RequestDeadPlayersList(NetworkConnection conn = null)
    {
        // Only the server runs this, then sends the list to all clients
        int[] deadIds = new int[_deadPlayers.Count];
        for (int i = 0; i < _deadPlayers.Count; i++)
        {
            deadIds[i] = _deadPlayers[i].ClientId;
        }
        SyncDeadPlayersClient(deadIds);
    }

    // Server sends the dead players list to all clients
    [ObserversRpc]
    private void SyncDeadPlayersClient(int[] deadIds)
    {
        deadPlayerIds.Clear();
        deadPlayerIds.AddRange(deadIds);
        Debug.Log("Synced dead player IDs from server.");
    }

    private IEnumerator RespawnAfterDelay(NetworkConnection conn)
    {
        yield return new WaitForSeconds(respawnDelay);

        // Only respawn during Playing state
        if (_currentState == GameState.Playing)
        {
            ServerRequestRespawn(conn);
            _alivePlayers.Add(conn);
        }
    }

    // Respawn a player
    [ServerRpc(RequireOwnership = false)]
    public void ServerRequestRespawn(NetworkConnection conn)
    {
        Debug.Log("Respawning player...");

        // Despawn old player
        if (conn.FirstObject != null)
        {
            ServerManager.Despawn(conn.FirstObject);
        }

        // Get a random spawn point
        Vector3 spawnPosition = GetRandomSpawnPosition();
        Quaternion spawnRotation = Quaternion.identity;

        // if (playerSpawner != null && playerSpawner.Spawns.Length > 0)
        // {
        //     int randomIndex = Random.Range(0, playerSpawner.Spawns.Length);
        //     Transform spawnPoint = playerSpawner.Spawns[randomIndex];
        //     spawnPosition = spawnPoint.position;
        //     spawnRotation = spawnPoint.rotation;
        // }

        GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        ServerManager.Spawn(newPlayer, conn);

        // Set up references
        newPlayer.GetComponent<PlayerSetup>().SetGameManager(gameObject);

        if (terrainManagerObject != null)
        {
            terrainManagerObject.GetComponent<SeedGenerator>().SetPlayer(newPlayer);
        }

        // Update the registered players dictionary
        if (_registeredPlayers.ContainsKey(conn))
        {
            _registeredPlayers[conn] = newPlayer;
        }
        else
        {
            _registeredPlayers.Add(conn, newPlayer);
        }

        // Ensure the player is considered alive after respawning
        if (!_alivePlayers.Contains(conn))
        {
            _alivePlayers.Add(conn);
        }

        Debug.Log("Player respawned successfully.");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Get MeshGenerator reference
        var meshGen = FindObjectOfType<MeshGenerator>();
        if (meshGen == null || meshGen.terrainDataDictionary == null || meshGen.terrainDataDictionary.Count == 0)
        {
            Debug.LogWarning("MeshGenerator or terrain data not ready, using fallback spawn.");
            return Vector3.zero;
        }

        // Terrain bounds
        float minX = -meshGen.xSize / 2f;
        float maxX = meshGen.xSize / 2f;
        float minZ = -meshGen.zSize / 2f;
        float maxZ = meshGen.zSize / 2f;

        for (int attempts = 0; attempts < 20; attempts++) // Try up to 20 times to find a valid spot
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);

            // Find the chunk this point is in
            int chunkSize = meshGen.chunkSize;
            float vertexSpacing = meshGen.vertexSpacing;
            Vector2Int chunkCoord = new Vector2Int(
                Mathf.FloorToInt(x / (vertexSpacing * chunkSize)),
                Mathf.FloorToInt(z / (vertexSpacing * chunkSize))
            );

            if (meshGen.terrainDataDictionary.TryGetValue(chunkCoord, out MeshData meshData))
            {
                // Convert world position to local chunk position
                float localX = ((x % (vertexSpacing * chunkSize)) + (vertexSpacing * chunkSize)) % (vertexSpacing * chunkSize);
                float localZ = ((z % (vertexSpacing * chunkSize)) + (vertexSpacing * chunkSize)) % (vertexSpacing * chunkSize);

                float y = 0f;
                try
                {
                    y = GetHeightFromMeshData(meshData, localX, localZ, (int)(vertexSpacing * chunkSize));
                }
                catch
                {
                    continue; // Try another random point if out of bounds
                }

                if (y > 10 && y < 200)
                {
                    return new Vector3(x, y + 5f, z); // Spawn 5 meters above ground
                }
            }
        }

        // Fallback if no valid spot found
        Debug.LogWarning("Could not find valid random spawn, using Vector3.zero.");
        return Vector3.zero;
    }

    // Helper: Copy this from your TreeGenerator or make it static somewhere
    private float GetHeightFromMeshData(MeshData meshData, float localX, float localZ, int chunkSize)
    {
        int resolution = Mathf.RoundToInt(Mathf.Sqrt(meshData.vertices.Length));
        float vertexSpacing = (float)chunkSize / (resolution - 1);

        int xIndex = Mathf.FloorToInt(localX / vertexSpacing);
        int zIndex = Mathf.FloorToInt(localZ / vertexSpacing);

        xIndex = Mathf.Clamp(xIndex, 0, resolution - 1);
        zIndex = Mathf.Clamp(zIndex, 0, resolution - 1);

        int vertexIndex = zIndex * resolution + xIndex;
        return meshData.vertices[vertexIndex].y;
    }

    // Create a method to handle player disconnection
    private void PlayerDisconnected(NetworkConnection conn)
    {
        if (!IsServerInitialized) return;

        if (_registeredPlayers.ContainsKey(conn))
        {
            // Player disconnected
            _registeredPlayers.Remove(conn);
            _alivePlayers.Remove(conn);
            _deadPlayers.Remove(conn);

            // Update player count
            _currentPlayerCount = _registeredPlayers.Count;
            UpdatePlayerCountClient(_currentPlayerCount);

            Debug.Log($"Player disconnected. Total players: {_currentPlayerCount}");

            // Check if game should end or state should change
            if (_currentState == GameState.Playing)
            {
                CheckGameOver();
            }
            else if (_currentState == GameState.Starting && _currentPlayerCount < minPlayersToStart)
            {
                // Cancel the countdown if we don't have enough players
                if (_gameStartCoroutine != null)
                {
                    StopCoroutine(_gameStartCoroutine);
                    _gameStartCoroutine = null;
                }
                SetGameState(GameState.WaitingForPlayers);
            }
        }
    }

    // Subscribe to connection state changes
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("GameManager started on server");

        // Initialize the game state on the server
        SetGameState(GameState.WaitingForPlayers);

        // Subscribe to client disconnection events
        ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    // Clean up when the object is destroyed
    public override void OnStopServer()
    {
        base.OnStopServer();

        // Unsubscribe from client disconnection events
        ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
    }

    // Handle client connection state changes
    private void OnRemoteConnectionState(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
        {
            PlayerDisconnected(conn);
        }
    }

    // Update game state on all clients
    [ObserversRpc]
    private void UpdateGameStateClient(string stateName)
    {
        if (gameStateText != null)
        {
            gameStateText.text = $"Game State: {stateName}";
        }

        // Parse and update the local _currentState for clients
        if (System.Enum.TryParse(stateName, out GameState parsedState))
        {
            _currentState = parsedState;
        }

        Debug.Log($"Game state updated to: {stateName}");
    }

    // Update player count on all clients
    [ObserversRpc]
    private void UpdatePlayerCountClient(int count)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {count}";
        }
    }

    // Update countdown on all clients
    [ObserversRpc]
    private void UpdateCountdownClient(int seconds)
    {
        if (countdownText != null)
        {
            if (seconds > 0)
            {
                countdownText.text = $"Starting in: {seconds}";
                countdownText.gameObject.SetActive(true);
            }
            else
            {
                countdownText.gameObject.SetActive(false);
            }
        }
    }

    // Start the game on all clients
    [ObserversRpc]
    private void StartGameOnAllClients()
    {
        Debug.Log("Game started!");

        if (IsOwner)
        {
            // Call Random Respawn
            //ServerRequestRespawn(base.Owner);
        }
    }

    // Show game over screen on all clients
    [ObserversRpc]
    private void ShowGameOverClient(string winnerName)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (winnerText != null)
            {
                winnerText.text = $"Winner: {winnerName}";
            }
        }

        Debug.Log($"Game Over! Winner: {winnerName}");
    }

    // Hide game over screen on all clients
    [ObserversRpc]
    private void HideGameOverClient()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    // Update UI elements based on current state
    private void UpdateGameStateUI()
    {
        if (gameStateText != null)
        {
            gameStateText.text = $"Game State: {_currentState}";
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {_currentPlayerCount}";
        }
    }
}