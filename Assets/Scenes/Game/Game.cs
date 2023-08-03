using UnityEngine;

public class Game : MonoBehaviour
{
    private GameManager gameManager;

    void Awake()
    {
        gameManager = GameManager.instance;
    }

    void OnGUI()
    {
        if (gameManager.session.room == null) return;

        GUILayout.BeginArea(new Rect(50, 50, 200, 200));
        GUILayout.BeginVertical();

        if (GUILayout.Button("End game")) gameManager.party.EndGame();

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}