using System;
using TMPro;
using UnityEngine;
using System.Security.Cryptography;
using JetBrains.Annotations;

public class CreateRoomUI : MonoBehaviour
{
    public MultiplayerSimulationManager simulationManager;
    public TextMeshProUGUI roomCodeDisplay;
    private const string AllowedChars = "ABCDEFGHIJKMNOPQRSTUVWXYZabcdefghjkmnopqrstuvwxyz0123456789";
    [CanBeNull] private string roomcode;
    
    private bool hasPushedButton = false;
    private void Start()
    {
        roomCodeDisplay.text = "";
    }

    private void OnEnable()
    {
        simulationManager.onConnectionFailed += createRoomFailed;    
        simulationManager.onCreatedRoom += showRoomCode;
    }
    
    private void OnDisable()
    {
        simulationManager.onConnectionFailed -= createRoomFailed;
        simulationManager.onCreatedRoom -= showRoomCode;

    }
    
    public void createRoom()
    {
        if (hasPushedButton) return;
        hasPushedButton = true;
        
        roomcode =  GenerateAlphanumericCode(); 
        Debug.Log("Attempting to create room code: "+ roomcode);
        simulationManager.HostGame(roomcode);
    }

    // inorder to retry again
    public void createRoomFailed()
    {
        hasPushedButton = false;
    }

    public void showRoomCode()
    {
        roomCodeDisplay.text = roomcode;
    }
    
    public static string GenerateAlphanumericCode(int length = 6)
    {
        char[] code = new char[length];
        for (int i = 0; i < length; i++)
        {
            code[i] = AllowedChars[RandomNumberGenerator.GetInt32(AllowedChars.Length)];
        }
        return new string(code);
        
    }
    
}
