using System;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class MapSceneRPCController : NetworkBehaviour
{
    //public override string prettyName => "Multiplayer RPC Controller";

    //public override string description =>
        //"Interprocess communication via Remote Procedure Calls\n lets you call another PC's c# functions";
        
    private void Update()
    {
        // only client
        if (Object.HasInputAuthority &&
            Keyboard.current.digit1Key.wasPressedThisFrame)
            RPC_toHost("Hello this is coming from  a client!");
        
        // Only host
        if (Object.HasStateAuthority &&
            Keyboard.current.digit2Key.wasPressedThisFrame)
            RPC_toClient("Hello this is coming from your host!");
        
        // only client
        if (Object.HasInputAuthority &&
            Keyboard.current.digit3Key.wasPressedThisFrame)
            RPC_SendMessage("Hello this is coming from  a client!");

        
    }


    // Client -> Host
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_toHost(string message, RpcInfo info = default)
    {
        Debug.Log($"message recieved from client: {info.Source.PlayerId}");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_toClient(string message)
    {
        Debug.Log($"message recieved from host!");
    }

    
    
    // Message relay, client -> via host -> all clients
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_SendMessage(string message, RpcInfo info = default)
    {
        RPC_RelayMessage(message, info.Source);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_RelayMessage(string message, PlayerRef messageSource)
    {
        if (messageSource == Runner.LocalPlayer)
        {
            message = $"You said: {message}\n";
        }
        else
        {
            message = $"Some other player said: {message}\n";
        }
    }
}
