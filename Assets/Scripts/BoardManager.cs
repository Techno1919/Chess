using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; set; }
    private bool[,] allowedMoves { get; set; }

    private const float TILE_SIZE = 1.0f;
    private const float TILE_OFFSET = 0.5f;

    private int selectionX = -1;
    private int selectionY = -1;

    public List<GameObject> chessmanPrefabs;
    private List<GameObject> activeChessman;

    private Quaternion whiteOrientation = Quaternion.Euler(0, 270, 0);
    private Quaternion blackOrientation = Quaternion.Euler(0, 90, 0);

    public Chessman[,] Chessmans { get; set; }
    private Chessman selectedChessman;

    public bool isWhiteTurn = true;

    private Material previousMat;
    public Material selectedMat;

    public int[] EnPassantMove { set; get; }

    public bool playChess960 = true;
    public bool placePieces = false;
    public bool piecesPlaced = false;
    public GameObject mainMenuScreen;

    public int index = 0;
    public int rook1Place;
    public int rook2Place;
    public int kingPlace;
    public int bishop1Place;
    public int bishop2Place;
    public int knight1Place;
    public int knight2Place;
    public int queenPlace;
    public List<int> places = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };

    // Use this for initialization
    void Start()
    {
        Instance = this;
        EnPassantMove = new int[2] { -1, -1 };
    }

    // Update is called once per frame
    void Update()
    {
        if(placePieces)
        {
            placePieces = false;
            piecesPlaced = true;
            SpawnAllChessmans();
        }

        UpdateSelection();

        if (Input.GetMouseButtonDown(0))
        {
            if (selectionX >= 0 && selectionY >= 0)
            {
                if (selectedChessman == null)
                {
                    // Select the chessman
                    SelectChessman(selectionX, selectionY);
                }
                else
                {
                    // Move the chessman
                    MoveChessman(selectionX, selectionY);
                }
            }
        }

        if (Input.GetKey("escape"))
            Application.Quit();
    }

    private void SelectChessman(int x, int y)
    {
        if(piecesPlaced)
        {
            if (Chessmans[x, y] == null) return;

            if (Chessmans[x, y].isWhite != isWhiteTurn) return;

            bool hasAtLeastOneMove = false;

            allowedMoves = Chessmans[x, y].PossibleMoves();
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (allowedMoves[i, j])
                    {
                        hasAtLeastOneMove = true;
                        i = 8;
                        break;
                    }
                }
            }

            if (!hasAtLeastOneMove)
                return;

            selectedChessman = Chessmans[x, y];
            previousMat = selectedChessman.GetComponent<MeshRenderer>().material;
            selectedMat.mainTexture = previousMat.mainTexture;
            selectedChessman.GetComponent<MeshRenderer>().material = selectedMat;

            BoardHighlights.Instance.HighLightAllowedMoves(allowedMoves);
        }
    }

    private void MoveChessman(int x, int y)
    {
        if (allowedMoves[x, y])
        {
            Chessman c = Chessmans[x, y];

            if (c != null && c.isWhite != isWhiteTurn)
            {
                // Capture a piece

                if (c.GetType() == typeof(King))
                {
                    // End the game
                    EndGame();
                    return;
                }

                activeChessman.Remove(c.gameObject);
                Destroy(c.gameObject);
            }
            if (x == EnPassantMove[0] && y == EnPassantMove[1])
            {
                if (isWhiteTurn)
                    c = Chessmans[x, y - 1];
                else
                    c = Chessmans[x, y + 1];

                activeChessman.Remove(c.gameObject);
                Destroy(c.gameObject);
            }
            EnPassantMove[0] = -1;
            EnPassantMove[1] = -1;
            if (selectedChessman.GetType() == typeof(Pawn))
            {
                if(y == 7) // White Promotion
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    SpawnChessman(1, x, y, true);
                    selectedChessman = Chessmans[x, y];
                }
                else if (y == 0) // Black Promotion
                {
                    activeChessman.Remove(selectedChessman.gameObject);
                    Destroy(selectedChessman.gameObject);
                    SpawnChessman(7, x, y, false);
                    selectedChessman = Chessmans[x, y];
                }
                EnPassantMove[0] = x;
                if (selectedChessman.CurrentY == 1 && y == 3)
                    EnPassantMove[1] = y - 1;
                else if (selectedChessman.CurrentY == 6 && y == 4)
                    EnPassantMove[1] = y + 1;
            }

            Chessmans[selectedChessman.CurrentX, selectedChessman.CurrentY] = null;
            selectedChessman.transform.position = GetTileCenter(x, y);
            selectedChessman.SetPosition(x, y);
            Chessmans[x, y] = selectedChessman;
            isWhiteTurn = !isWhiteTurn;
        }

        selectedChessman.GetComponent<MeshRenderer>().material = previousMat;

        BoardHighlights.Instance.HideHighlights();
        selectedChessman = null;
    }

    private void UpdateSelection()
    {
        if (!Camera.main) return;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 50.0f, LayerMask.GetMask("ChessPlane")))
        {
            selectionX = (int)hit.point.x;
            selectionY = (int)hit.point.z;
        }
        else
        {
            selectionX = -1;
            selectionY = -1;
        }
    }

    private void SpawnChessman(int index, int x, int y, bool isWhite)
    {
        Vector3 position = GetTileCenter(x, y);
        GameObject go;

        if (isWhite)
        {
            go = Instantiate(chessmanPrefabs[index], position, whiteOrientation) as GameObject;
        }
        else
        {
            go = Instantiate(chessmanPrefabs[index], position, blackOrientation) as GameObject;
        }

        go.transform.SetParent(transform);
        Chessmans[x, y] = go.GetComponent<Chessman>();
        Chessmans[x, y].SetPosition(x, y);
        activeChessman.Add(go);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += (TILE_SIZE * x) + TILE_OFFSET;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET;

        return origin;
    }

    private void SpawnAllChessmans()
    {
        activeChessman = new List<GameObject>();
        Chessmans = new Chessman[8, 8];


        if(playChess960)
        {
            System.Random rand = new System.Random();
            /////// White ///////

            // Rooks
            int maxNum = places.Count;
            index = rand.Next(0, maxNum);
            rook1Place = places[index];
            maxNum--;
            places.RemoveAt(index);
            SpawnChessman(2, rook1Place, 0, true);
            bool placeSecondRook = false;
            do
            {
                placeSecondRook = PlaceSecondRook(rand, maxNum);
            } while (!placeSecondRook);
            maxNum--;
            SpawnChessman(2, rook2Place, 0, true);

            // King
            bool placeKing = false;
            do
            {
                placeKing = PlaceKing(rand, maxNum);
            } while (!placeKing);
            maxNum--;
            SpawnChessman(0, kingPlace, 0, true);

            // Bishops
            index = rand.Next(0, maxNum);
            maxNum--;
            bishop1Place = places[index];
            places.RemoveAt(index);
            bool bishop1Even = false;
            if (bishop1Place % 2 == 0) bishop1Even = true;
            SpawnChessman(3, bishop1Place, 0, true);
            bool placeSecondBishop;
            do
            {
                placeSecondBishop = PlaceBishop(rand, maxNum, bishop1Even);
            } while (!placeSecondBishop);
            maxNum--;
            SpawnChessman(3, bishop2Place, 0, true);

            // Queen
            index = rand.Next(0, maxNum);
            queenPlace = places[index];
            maxNum--;
            places.RemoveAt(index);
            SpawnChessman(1, queenPlace, 0, true);

            // Knights
            index = rand.Next(0, maxNum);
            knight1Place = places[index];
            maxNum--;
            places.RemoveAt(index);
            SpawnChessman(4, knight1Place, 0, true);
            index = rand.Next(0, maxNum);
            knight2Place = places[index];
            places.RemoveAt(index);
            SpawnChessman(4, knight2Place, 0, true);

            /////// Black ///////

            // Rooks
            SpawnChessman(8, rook1Place, 7, false);
            SpawnChessman(8, rook2Place, 7, false);

            // King
            SpawnChessman(6, kingPlace, 7, false);

            // Bishop
            SpawnChessman(9, bishop1Place, 7, false);
            SpawnChessman(9, bishop2Place, 7, false);

            // Queen
            SpawnChessman(7, queenPlace, 7, false);

            // Knights
            SpawnChessman(10, knight1Place, 7, false);
            SpawnChessman(10, knight2Place, 7, false);

        }
        else
        {
            // Rooks
            SpawnChessman(2, 0, 0, true);
            SpawnChessman(2, 7, 0, true);

            // King
            SpawnChessman(0, 3, 0, true);

            // Bishops
            SpawnChessman(3, 2, 0, true);
            SpawnChessman(3, 5, 0, true);

            // Queen
            SpawnChessman(1, 4, 0, true);

            // Knights
            SpawnChessman(4, 1, 0, true);
            SpawnChessman(4, 6, 0, true);

            /////// Black ///////

            // Rooks
            SpawnChessman(8, 0, 7, false);
            SpawnChessman(8, 7, 7, false);

            // King
            SpawnChessman(6, 4, 7, false);

            // Bishop
            SpawnChessman(9, 2, 7, false);
            SpawnChessman(9, 5, 7, false);

            // Queen
            SpawnChessman(7, 3, 7, false);

            // Knights
            SpawnChessman(10, 1, 7, false);
            SpawnChessman(10, 6, 7, false);
        }

        //White Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(5, i, 1, true);
        }

        //Black Pawns
        for (int i = 0; i < 8; i++)
        {
            SpawnChessman(11, i, 6, false);
        }
    }

    private void EndGame()
    {
        if (isWhiteTurn)
            Debug.Log("White wins");
        else
            Debug.Log("Black wins");

        foreach (GameObject go in activeChessman)
        {
            Destroy(go);
        }

        isWhiteTurn = true;
        BoardHighlights.Instance.HideHighlights();
        SpawnAllChessmans();
    }

    public void PlayChess()
    {
        if (EventSystem.current.currentSelectedGameObject.name == "RegularGame")
        {
            playChess960 = false;
            placePieces = true;
        }
        else if(EventSystem.current.currentSelectedGameObject.name == "Chess960")
        {
            playChess960 = true;
            placePieces = true;
        }
        mainMenuScreen.SetActive(false);
    }

    public bool PlaceSecondRook(System.Random rand, int maxNum)
    {
        index = rand.Next(0, maxNum);
        rook2Place = places[index];
        if(!(rook2Place + 1 == rook1Place) && !(rook2Place - 1 == rook1Place))
        {
            places.RemoveAt(index);
            return true;
        }
        return false;
    }

    public bool PlaceKing(System.Random rand, int maxNum)
    {
        index = rand.Next(0, maxNum);
        kingPlace = places[index];
        if (kingPlace > rook1Place && kingPlace < rook2Place)
        {
            places.RemoveAt(index);
            return true;

        }
        else if (kingPlace < rook1Place && kingPlace > rook2Place)
        {
            places.RemoveAt(index);
            return true;
        }

        return false;
    }

    public bool PlaceBishop(System.Random rand, int maxNum, bool bishop1Even)
    {
        index = rand.Next(0, maxNum);
        bishop2Place = places[index];
        if(!((bishop2Place % 2 == 0) == bishop1Even))
        {
            places.RemoveAt(index);
            return true;
        }

        return false;
    }



    //Checks to make sure the rooks are not placed next to each other
    public void TestRook()
    {
        bool firstRook = true;
        int rook1Pos = -1;
        int rook2Pos = -1;
        for(int i = 0; i < 7; i++)
        {
            if(Chessmans[i, 0].name.Substring(6, 4) == "Rook")
            {
                if(firstRook)
                {
                    rook1Pos = i;
                    firstRook = false;
                }
                else
                {
                    rook2Pos = i;
                }
            }
        }
        if(rook1Pos-1 == rook2Pos && rook1Pos+1 == rook2Pos)
        {
            Debug.Log("Test Failed: Rooks should not be next to each other");
        }
        else
        {
            Debug.Log("Test Passed: Rooks are seperated");
        }
    }

    //Checks if the king is in between the rooks
    public void TestKing()
    {
        bool firstRook = true;
        int rook1Pos = -1;
        int rook2Pos = -1;
        int kingPos = -1;
        for (int i = 0; i < 7; i++)
        {
            if (Chessmans[i, 0].name.Substring(6, 4) == "Rook")
            {
                if (firstRook)
                {
                    rook1Pos = i;
                    firstRook = false;
                }
                else
                {
                    rook2Pos = i;
                }
            }
            else if(Chessmans[i, 0].name.Substring(6, 4) == "King")
            {
                kingPos = i;
            }
        }
        if(!(kingPos > rook1Pos && kingPos < rook2Pos))
        {
            Debug.Log("Test Passed: King is in between the rooks");
        }
        else if(!(kingPos > rook2Pos && kingPos < rook1Pos))
        {
            Debug.Log("Test Passed: King is in between the rooks");
        }
        else
        {
            Debug.Log("Test Failed: King needs to be between the rooks");
        }

    }

    //Checks if the bishops are on opposite colored tiles 
    public void TestBishops()
    {
        bool firstBishop = true;
        int bishop1Pos = -1;
        int bishop2Pos = -1;
        for (int i = 0; i < 7; i++)
        {
            if (Chessmans[i, 0].name.Substring(6, 6) == "Bishop")
            {
                if (firstBishop)
                {
                    bishop1Pos = i;
                    firstBishop = false;
                }
                else
                {
                    bishop2Pos = i;
                }
            }
        }
        if((bishop1Pos % 2 == 0) == (bishop2Pos % 2 == 0))
        {
            Debug.Log("Test Failed: Bishops need to be on different colored tiles");
        }
        else
        {
            Debug.Log("Test Passed: Bishops are on different colored tiles");
        }
    }

    //Checks if the board is mirrored
    public void TestBoardsIsMirrored()
    {
        bool firstBishop = true;
        int bishop1Pos = -1;
        int bishop2Pos = -1;
        bool firstRook = true;
        int rook1Pos = -1;
        int rook2Pos = -1;
        bool firstKnight = true;
        int knigt1Pos = -1;
        int knigt2Pos = -1;
        int queenPos = -1;
        int kingPos = -1;
        for (int i = 0; i < 8; i++)
        {
            
            if (Chessmans[i, 0].name.Substring(6, 4) == "Rook")
            {
                if (firstRook)
                {
                    rook1Pos = i;
                    firstRook = false;
                }
                else
                {
                    rook2Pos = i;
                }
            }
            else if (Chessmans[i, 0].name.Substring(6, 4) == "King")
            {
                kingPos = i;
            }
            else if (Chessmans[i, 0].name.Substring(6, 6) == "Knight")
            {
                if (firstKnight)
                {
                    knigt1Pos = i;
                    firstKnight = false;
                }
                else
                {
                    knigt2Pos = i;
                }
            }
            else if (Chessmans[i, 0].name.Substring(6, 5) == "Queen")
            {
                queenPos = i;
            }
            else if (Chessmans[i, 0].name.Substring(6, 6) == "Bishop")
            {
                
                if (firstBishop)
                {
                    bishop1Pos = i;
                    firstBishop = false;
                }
                else
                {
                    bishop2Pos = i;
                }
            }
        }
        
        if(Chessmans[bishop1Pos, 0].name.Substring(6, 6) == Chessmans[bishop1Pos, 7].name.Substring(6, 6))
        {
            if(Chessmans[bishop2Pos, 0].name.Substring(6, 6) == Chessmans[bishop2Pos, 7].name.Substring(6, 6))
            {
                if (Chessmans[rook1Pos, 0].name.Substring(6, 4) == Chessmans[rook1Pos, 7].name.Substring(6, 4))
                {
                    if (Chessmans[rook2Pos, 0].name.Substring(6, 4) == Chessmans[rook2Pos, 7].name.Substring(6, 4))
                    {
                        if (Chessmans[knigt1Pos, 0].name.Substring(6, 6) == Chessmans[knigt1Pos, 7].name.Substring(6, 6))
                        {
                            if (Chessmans[knigt2Pos, 0].name.Substring(6, 6) == Chessmans[knigt2Pos, 7].name.Substring(6, 6))
                            {
                                if (Chessmans[queenPos, 0].name.Substring(6, 5) == Chessmans[queenPos, 7].name.Substring(6, 5))
                                {
                                    if (Chessmans[kingPos, 0].name.Substring(6, 4) == Chessmans[kingPos, 7].name.Substring(6, 4))
                                    {
                                        Debug.Log("Test Passed: Board is correctly mirrored");
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        Debug.Log("Test Failed: Board is not mirrored");
    }

    //Checks to see if the pieces for a regular chess
    //game are place properly
    public void TestRegularChess()
    {
        bool firstBishop = true;
        int bishop1Pos = -1;
        int bishop2Pos = -1;
        bool firstRook = true;
        int rook1Pos = -1;
        int rook2Pos = -1;
        bool firstKnight = true;
        int knigt1Pos = -1;
        int knigt2Pos = -1;
        int queenPos = -1;
        int kingPos = -1;
        for (int i = 0; i < 8; i++)
        {

            if (Chessmans[i, 0].name.Substring(6, 4) == "Rook")
            {
                if (firstRook)
                {
                    rook1Pos = i;
                    firstRook = false;
                }
                else
                {
                    rook2Pos = i;
                }
            }
            else if (Chessmans[i, 0].name.Substring(6, 4) == "King")
            {
                kingPos = i;
            }
            else if (Chessmans[i, 0].name.Substring(6, 6) == "Knight")
            {
                if (firstKnight)
                {
                    knigt1Pos = i;
                    firstKnight = false;
                }
                else
                {
                    knigt2Pos = i;
                }
            }
            else if (Chessmans[i, 0].name.Substring(6, 5) == "Queen")
            {
                queenPos = i;
            }
            else if (Chessmans[i, 0].name.Substring(6, 6) == "Bishop")
            {

                if (firstBishop)
                {
                    bishop1Pos = i;
                    firstBishop = false;
                }
                else
                {
                    bishop2Pos = i;
                }
            }
        }

        if(rook1Pos == 0)
        {
            if(knigt1Pos == 1)
            {
                if(bishop1Pos == 2)
                { 
                    if(kingPos == 3)
                    {
                        if(queenPos == 4)
                        {
                            if(bishop2Pos == 5)
                            {
                                if(knigt2Pos == 6)
                                {
                                    if(rook2Pos == 7)
                                    {
                                        Debug.Log("Test Passed: The Pieces are placed properly for regular chess");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log("Test Failed: The pieces are not placed properly for regular chess");
        }
    }
}


