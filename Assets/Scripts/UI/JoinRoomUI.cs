using TMPro;
using UnityEngine;
using WebSocketSharp;

public class JoinRoomUI : MonoBehaviour
{
    public MultiplayerSimulationManager simulationManager;
    public TMP_InputField  inputField;

    public void joinRoom()
    {
        if (inputField.text.IsNullOrEmpty()) return;
        
        simulationManager.JoinGame(inputField.text);
    }
    
}
