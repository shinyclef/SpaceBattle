using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameState CurrentState { get; private set; }
    public static GameState LastState { get; private set; }

    private static GameStateManager I;

    
    /* ----- */
    /* Setup */
    /* ----- */

    private void Awake()
    {
        CurrentState = GameState.Game;
        LastState = GameState.Game;
        I = this;
    }
}