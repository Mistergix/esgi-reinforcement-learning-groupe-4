using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PGSauce.Core.PGDebugging;
using PGSauce.Core.Utilities;
using PGSauce.Games.IaEsgi.Ia;
using PGSauce.Unity;

namespace PGSauce.Games.IaEsgi.GridWorldConsole
{
    public class GridWorld : QAlgorithm<QAgentGridWorldConsole, QStateGridWorldConsole>
    {
        #region Public And Serialized Fields
        [SerializeField] private GridWorldConsoleData level;
        private Dictionary<Coords, QStateGridWorldConsole> _statesDictionary;

        [SerializeField]
        private Camera cam;

        public int groundTile;
        public GameObject groundPrefabs;

        public int playerTile;
        public GameObject playerPrefabs;

        public int bombsTile;
        public GameObject bombsPrefabs;

        public int energyTile;
        public GameObject energyPrefabs;

        public int wallTile;
        public GameObject wallPrefabs;

        public int endTile;
        public GameObject endPrefabs;

        public float tileSize;

        #endregion
        #region Private Fields
        private int rows, cols;
        private int[,] levelData;
        private Vector2 middleOffset = new Vector2();

        private GameObject player;


        #endregion
        #region Properties
        #endregion
        #region Public Methods
        #endregion
        #region Private Methods
        #endregion

        protected override bool ContinueToRunAlgorithm(QStateGridWorldConsole state)
        {
            return !state.Equals(_statesDictionary[level.end]);
        }

        protected override void InitializeAlgorithm()
        {
            ParseLevel();
            CreateLevel();
            var actionUp = new QActionGridWorldConsole(agent => agent.GoUp(), "Go up");
            var actionDown = new QActionGridWorldConsole(agent => agent.GoDown(), "Go Down");
            var actionLeft = new QActionGridWorldConsole(agent => agent.GoLeft(), "Go Left");
            var actionRight = new QActionGridWorldConsole(agent => agent.GoRight(), "Go Right");
            var actions = new List<QAction<QAgentGridWorldConsole, QStateGridWorldConsole>> { actionDown, actionUp, actionRight, actionLeft };
            _statesDictionary = CreateStates();
            Agent = new QAgentGridWorldConsole(this, new List<QStateGridWorldConsole>(_statesDictionary.Values.ToList()), actions, _statesDictionary[level.start]);
        }


        private Dictionary<Coords, QStateGridWorldConsole> CreateStates()
        {
            var states = new Dictionary<Coords, QStateGridWorldConsole>();
            for (var i = 0; i < level.width; i++)
            {
                for (var j = 0; j < level.height; j++)
                {
                    var coords = new Coords(i, j);
                    var state = new QStateGridWorldConsole(coords);
                    states.Add(coords, state);
                }
            }

            return states;
        }

        public QStateGridWorldConsole GoUp()
        {
            return Move(new Coords(0, 1));
        }

        public QStateGridWorldConsole GoDown()
        {
            return Move(new Coords(0, -1));
        }

        public QStateGridWorldConsole GoLeft()
        {
            return Move(new Coords(-1, 0));
        }

        public QStateGridWorldConsole GoRight()
        {
            return Move(new Coords(1, 0));
        }

        private bool IsInBounds(Coords nextCoords)
        {
            return nextCoords.x.IsBetween(0, level.width - 1) && nextCoords.y.IsBetween(0, level.height - 1);
        }

        private QStateGridWorldConsole Move(Coords offset)
        {
            var currenState = Agent.CurrentState;
            var nextCoords = currenState.Coords + offset;
            if (IsInBounds(nextCoords))
            {
                player.transform.position = GetScreenPointFromLevelIndices(nextCoords.x, nextCoords.y, -0.02f);
                return _statesDictionary[nextCoords];
            }

            return Agent.CurrentState;
        }

        public float GetTileValue(Coords coords)
        {
            var factor = coords.Equals(Agent.OldState.Coords) ? 2 : 1;
            if (level.bombs.Contains(coords))
            {
                PGDebug.Message($"{coords} is bomb").Log();
                return -100f * factor;
            }

            if (level.energy.Contains(coords))
            {
                PGDebug.Message($"{coords} is energy").Log();
                return 5f;
            }

            if (level.end.Equals(coords))
            {
                PGDebug.Message($"{coords} is end").Log();
                return 1000f;
            }

            PGDebug.Message($"{coords} is blank").Log();
            return -1f * factor;
        }

        void ParseLevel()
        {
            cam.orthographicSize = level.height;
            rows = level.height;
            cols = level.width;
            levelData = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (i == 0 || j == 0 || i == rows - 1 || j == cols - 1)
                        levelData[i, j] = wallTile;
                    else
                        levelData[i, j] = groundTile;
                }
            }

            foreach (Coords coords in level.bombs)
            {
                levelData[coords.y, coords.x] = bombsTile;
            }

            foreach (Coords coords in level.energy)
            {
                levelData[coords.y, coords.x] = energyTile;
            }

            levelData[level.start.y, level.start.x] = playerTile;
            levelData[level.end.y, level.end.x] = endTile;

        }

        void CreateLevel()
        {
            middleOffset.x = cols * tileSize * 0.5f - tileSize * 0.5f;
            middleOffset.y = rows * tileSize * 0.5f - tileSize * 0.5f;
            GameObject tile;
            SpriteRenderer sr;
            GameObject energy;
            GameObject bombs;
            GameObject end;

            int destinationCount = 0;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    int val = levelData[i, j];

                    if (val != wallTile)
                    {
                        tile = Instantiate(groundPrefabs, new Vector3(0, 0, 0), Quaternion.identity);
                        tile.name = "tile" + i.ToString() + "_" + j.ToString();
                        tile.transform.localScale *= tileSize;
                        tile.transform.position = GetScreenPointFromLevelIndices(i, j, 0);
                        if (val == endTile)
                        {
                            end = Instantiate(endPrefabs, new Vector3(0, 0, -0.01f), Quaternion.identity);
                            end.transform.localScale *= tileSize;
                            end.transform.position = GetScreenPointFromLevelIndices(i, j, -0.1f);
                        }
                        else
                        {

                            if (val == playerTile)
                            {
                                player = Instantiate(playerPrefabs, new Vector3(0, 0, -0.01f), Quaternion.identity);
                                player.transform.localScale *= tileSize;
                                player.transform.position = GetScreenPointFromLevelIndices(i, j, -0.2f);
                            }
                            else if (val == bombsTile)
                            {
                                bombs = Instantiate(bombsPrefabs, new Vector3(0, 0, -0.01f), Quaternion.identity);
                                bombs.transform.localScale *= tileSize;
                                bombs.transform.position = GetScreenPointFromLevelIndices(i, j, -0.2f);
                            }

                            else if (val == energyTile)
                            {
                                energy = Instantiate(energyPrefabs, new Vector3(0, 0, -0.01f), Quaternion.identity);
                                energy.transform.localScale *= tileSize;
                                energy.transform.position = GetScreenPointFromLevelIndices(i, j, -0.2f);
                            }
                        }
                    }
                    else
                    {
                        tile = Instantiate(wallPrefabs, new Vector3(0, 0, 0), Quaternion.identity);
                        tile.transform.localScale *= tileSize;
                        tile.transform.position = GetScreenPointFromLevelIndices(i, j, 0);
                    }
                }
            }
        }

        Vector3 GetScreenPointFromLevelIndices(int row, int col, float z)
        {
            //converting indices to position values, col determines x & row determine y
            return new Vector3(col * tileSize - middleOffset.x, (row - level.height) * -tileSize + middleOffset.y, z);
        }

    }

}
