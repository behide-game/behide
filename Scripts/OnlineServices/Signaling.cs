using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Reactive.Subjects;
using Behide.I18n.BOS.Errors;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using TypedSignalR.Client;
using Behide.Types;
using Serilog;

// ReSharper disable once CheckNamespace
namespace Behide.OnlineServices.Signaling;

public abstract class ConnectionAttempt(ConnAttemptId id, ISignalingHub hub)
{
    public readonly ConnAttemptId Id = id;
    protected readonly ISignalingHub Hub = hub;
    protected readonly ILogger Log = Serilog.Log.ForContext("Tag", $"Signaling/ConnectionAttempt({id})");
    public readonly ReplaySubject<IceCandidate> Candidates = new();

    public void SendIceCandidate(IceCandidate candidate)
    {
        Task.Run(async () =>
        {
            var res = await Hub.SendIceCandidate(Id, candidate);
            if (res.HasError(out var error)) Log.Warning("{error}", error.ToLocalizedString());
        });
    }

    public void End()
    {
        Task.Run(async () =>
        {
            var res = await Hub.EndConnectionAttempt(Id);
            if (res.HasError(out var error)) Log.Error("{error}", error.ToLocalizedString());
        });
    }
}

public class OffererConnectionAttempt(ConnAttemptId id, ISignalingHub hub) : ConnectionAttempt(id, hub)
{
    public readonly TaskCompletionSource<SdpDescription> Answer = new();
    public Task<SdpDescription> WaitAnswer() => Answer.Task;
}

public class AnswererConnectionAttempt(ConnAttemptId id, ISignalingHub hub, SdpDescription offer) : ConnectionAttempt(id, hub)
{
    public readonly SdpDescription Offer = offer;
    public void SendAnswer(SdpDescription answer)
    {
        Task.Run(async () =>
        {
            var res = await Hub.SendAnswer(Id, answer);
            if (res.HasError(out var error)) Log.Error("{error}", error.ToLocalizedString());
        });
    }
}

/// <summary>
/// Interfaces the signaling hub for easier usage.
/// Also connects remote peers.
/// </summary>
public partial class Signaling : Node
{
    private HubConnection connection = null!;
    private ISignalingHub hub = null!;
    private ILogger log = null!;
    private HubReceiver receiver = null!;

    /// <summary>
    /// Receive messages from signaling hub and populate connection attempts
    /// </summary>
    private class HubReceiver(MultiplayerApi multiplayer, Signaling signaling) : ISignalingClient
    {
        public readonly Dictionary<ConnAttemptId, ConnectionAttempt> ConnectionAttempts = [];

        public async Task<ConnAttemptId?> ConnectionRequested(int askingPeerId)
        {
            var peer = new Networking.PeerConnection();
            var multiplayerPeer = (WebRtcMultiplayerPeer)multiplayer.MultiplayerPeer;
            multiplayerPeer.AddPeer(peer, askingPeerId);

            return await peer.PublishOffer(signaling);
        }

        public Task SdpAnswerReceived(ConnAttemptId offerId, SdpDescription answer)
        {
            if (ConnectionAttempts[offerId] is OffererConnectionAttempt o) o.Answer.TrySetResult(answer);
            return Task.CompletedTask;
        }

        public Task IceCandidateReceived(ConnAttemptId offerId, IceCandidate iceCandidate)
        {
            ConnectionAttempts[offerId].Candidates.OnNext(iceCandidate);
            return Task.CompletedTask;
        }
    }

    public override void _EnterTree()
    {
        log = Log.ForContext("Tag", "Signaling");

        connection = new HubConnectionBuilder()
            .WithUrl(Secrets.SignalingHubUrl)
            .WithAutomaticReconnect()
            .ConfigureLogging(loggingBuilder => { loggingBuilder.AddSerilog(log); })
            .AddJsonProtocol(options =>
            {
                // Enable F# types serialization
                JsonFSharpOptions
                    .Default()
                    .AddToJsonSerializerOptions(options.PayloadSerializerOptions);
            })
            .Build();

        connection.Closed += _ => Task.Run(() => log.Error("SignalR connection closed"));

        // Creating typed hub
        hub = connection.CreateHubProxy<ISignalingHub>();

        // Set up hub request handlers
        receiver = new HubReceiver(Multiplayer, this);
        connection.Register<ISignalingClient>(receiver);

        // Starting the connection
        Task.Run(async () =>
        {
            try
            {
                await connection.StartAsync();
                log.Information("SignalR connected !");
            }
            catch (Exception exn)
            {
                log.Error("Failed to connect to signaling hub: {ExnMessage}", exn.Message);
            }
        });
    }

    public async Task<OffererConnectionAttempt> StartConnectionAttempt(SdpDescription offer)
    {
        var res = (await hub.StartConnectionAttempt(offer));
        if (res.HasError(out var error))
        {
            log.Error("Failed to publish offer: {error}", error.ToLocalizedString());
            throw new Exception(error.ToLocalizedString());
        }

        var connAttempt = new OffererConnectionAttempt(res.ResultValue, hub);
        receiver.ConnectionAttempts.Add(connAttempt.Id, connAttempt);

        return connAttempt;
    }

    public async Task<AnswererConnectionAttempt> JoinConnectionAttempt(ConnAttemptId connAttemptId)
    {
        var res = (await hub.JoinConnectionAttempt(connAttemptId));
        if (res.HasError(out var error))
        {
            log.Error("{error}", error.ToLocalizedString());
            throw new Exception(error.ToLocalizedString());
        }

        var connAttempt = new AnswererConnectionAttempt(connAttemptId, hub, res.ResultValue);
        receiver.ConnectionAttempts.Add(connAttempt.Id, connAttempt);

        return connAttempt;
    }

    public async Task<RoomId> CreateRoom()
    {
        var res = await hub.CreateRoom();
        if (!res.HasError(out var error)) return res.ResultValue;

        log.Error("{error}", error.ToLocalizedString());
        throw new Exception(error.ToLocalizedString());
    }

    public async Task<int> JoinRoom(RoomId roomId)
    {
        var res = await hub.JoinRoom(roomId);
        if (!res.HasError(out var error)) return res.ResultValue;

        log.Error("{error}", error.ToLocalizedString());
        throw new Exception(error.ToLocalizedString());
    }

    public async Task<RoomConnectionInfo> ConnectToRoomPlayers()
    {
        var res = await hub.ConnectToRoomPlayers();
        if (!res.HasError(out var error)) return res.ResultValue;

        log.Error("{error}", error.ToLocalizedString());
        throw new Exception(error.ToLocalizedString());
    }

    public async Task LeaveRoom()
    {
        var res = await hub.LeaveRoom();
        if (!res.HasError(out var error)) return;

        log.Error("{error}", error.ToLocalizedString());
        throw new Exception(error.ToLocalizedString());
    }
}
