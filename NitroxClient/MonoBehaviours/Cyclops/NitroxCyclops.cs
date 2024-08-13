using System.Collections.Generic;
using NitroxClient.GameLogic;
using NitroxClient.GameLogic.PlayerLogic;
using NitroxClient.GameLogic.PlayerLogic.PlayerModel.Abstract;
using UnityEngine;

namespace NitroxClient.MonoBehaviours.Cyclops;

/// <summary>
/// Script responsible for managing all player movement-related interactions.
/// </summary>
public class NitroxCyclops : MonoBehaviour
{
    public VirtualCyclops Virtual;
    private CyclopsMotor cyclopsMotor;
    private SubRoot subRoot;
    private SubControl subControl;
    private Rigidbody rigidbody;
    private WorldForces worldForces;
    private Stabilizer stabilizer;
    private CharacterController controller;
    private int ballasts;

    public readonly Dictionary<INitroxPlayer, CyclopsPawn> Pawns = [];

    public void Start()
    {
        cyclopsMotor = Player.mainObject.GetComponent<CyclopsMotor>();
        subRoot = GetComponent<SubRoot>();
        subControl = GetComponent<SubControl>();
        rigidbody = GetComponent<Rigidbody>();
        worldForces = GetComponent<WorldForces>();
        stabilizer = GetComponent<Stabilizer>();
        controller = cyclopsMotor.controller;
        ballasts = GetComponentsInChildren<BallastWeight>(true).Length;

        UWE.Utils.SetIsKinematicAndUpdateInterpolation(rigidbody, false, true);

        GetComponent<SubFire>().enabled = false;

        WorkaroundColliders();
    }

    public void Update()
    {
        MaintainPawns();
    }

    /// <summary>
    /// Triggers required on-remove callbacks on children player objects, including the local player.
    /// </summary>
    public void RemoveAllPlayers()
    {
        foreach (RemotePlayerIdentifier remotePlayerIdentifier in GetComponentsInChildren<RemotePlayerIdentifier>(true))
        {
            remotePlayerIdentifier.RemotePlayer.ResetStates();
            OnPlayerExit(remotePlayerIdentifier.RemotePlayer);
        }
        OnLocalPlayerExit();
    }

    /// <summary>
    /// Parents local player to the cyclops and registers it in the current cyclops.
    /// </summary>
    public void OnLocalPlayerEnter()
    {
        Virtual = VirtualCyclops.Instance;
        Virtual.SetCurrentCyclops(this);

        Player.mainObject.transform.parent = subRoot.transform;
        CyclopsPawn pawn = AddPawnForPlayer(this.Resolve<ILocalNitroxPlayer>());
        cyclopsMotor.SetCyclops(this, subRoot, pawn);
        cyclopsMotor.ToggleCyclopsMotor(true);
    }

    /// <summary>
    /// Unregisters the local player from the current cyclops. Ensures the player is not weirdly rotated when it leaves the cyclops.
    /// </summary>
    public void OnLocalPlayerExit()
    {
        RemovePawnForPlayer(this.Resolve<ILocalNitroxPlayer>());
        Player.main.transform.parent = null;
        Player.main.transform.rotation = Quaternion.identity;
        cyclopsMotor.ToggleCyclopsMotor(false);
        cyclopsMotor.Pawn = null;

        Virtual.SetCurrentCyclops(null);
    }

    /// <summary>
    /// Registers a remote player for it to get a pawn in the current cyclops.
    /// </summary>
    public void OnPlayerEnter(RemotePlayer remotePlayer)
    {
        remotePlayer.Pawn = AddPawnForPlayer(remotePlayer);
    }

    /// <summary>
    /// Unregisters a remote player from the current cyclops.
    /// </summary>
    public void OnPlayerExit(RemotePlayer remotePlayer)
    {
        RemovePawnForPlayer(remotePlayer);
        remotePlayer.Pawn = null;
    }

    public void MaintainPawns()
    {
        foreach (CyclopsPawn pawn in Pawns.Values)
        {
            if (pawn.MaintainPredicate())
            {
                pawn.MaintainPosition();
            }
        }
    }

    public CyclopsPawn AddPawnForPlayer(INitroxPlayer player)
    {
        if (!Pawns.TryGetValue(player, out CyclopsPawn pawn))
        {
            pawn = new(player, this);
            Pawns.Add(player, pawn);
        }
        return pawn;
    }

    public void RemovePawnForPlayer(INitroxPlayer player)
    {
        if (Pawns.TryGetValue(player, out CyclopsPawn pawn))
        {
            pawn.Terminate();
        }
        Pawns.Remove(player);
    }

    // TODO: Use SetBroadcasting and SetReceiving when we finally have a cyclops movements rework
    public void SetBroadcasting()
    {
        worldForces.enabled = true;
        stabilizer.stabilizerEnabled = true;
    }

    public void SetReceiving()
    {
        worldForces.enabled = false;
        stabilizer.stabilizerEnabled = false;
    }

    private void WorkaroundColliders()
    {
        CyclopsSubNameScreen cyclopsSubNameScreen = transform.GetComponentInChildren<CyclopsSubNameScreen>(true);
        TriggerWorkaround subNameTriggerWorkaround = cyclopsSubNameScreen.gameObject.AddComponent<TriggerWorkaround>();
        subNameTriggerWorkaround.Initialize(this,cyclopsSubNameScreen.animator, cyclopsSubNameScreen.ContentOn, nameof(CyclopsSubNameScreen.ContentOff), cyclopsSubNameScreen);

        CyclopsLightingPanel cyclopsLightingPanel = transform.GetComponentInChildren<CyclopsLightingPanel>(true);
        TriggerWorkaround lightingTriggerWorkaround = cyclopsLightingPanel.gameObject.AddComponent<TriggerWorkaround>();
        lightingTriggerWorkaround.Initialize(this, cyclopsLightingPanel.uiPanel, cyclopsLightingPanel.ButtonsOn, nameof(CyclopsLightingPanel.ButtonsOff), cyclopsLightingPanel);
    }
}