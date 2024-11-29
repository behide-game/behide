namespace Behide.OnlineServices.Client;

using Godot;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FSharp.Core;
using TypedSignalR.Client;

using Behide.Types;
using Behide.OnlineServices.Signaling;
using Serilog;

public class SignalingHubClient : ISignalingClient
{
    public event Func<int, Task<Option<ConnAttemptId>>>? ConnAttemptIdCreationRequested;
    public event Action<ConnAttemptId, SdpDescription>? SdpAnswerReceived_;
    public event Action<ConnAttemptId, IceCandidate>? IceCandidateReceived_;
    public Dictionary<ConnAttemptId, IceCandidate[]> receivedIceCandidates = [];

    public async Task<FSharpOption<ConnAttemptId>> CreateConnAttempt(int askingPeerId)
    {
        return ConnAttemptIdCreationRequested is null
            ? Option.None<ConnAttemptId>()
            : await ConnAttemptIdCreationRequested.Invoke(askingPeerId);
    }

    public Task SdpAnswerReceived(ConnAttemptId offerId, SdpDescription sdpDescription)
    {
        SdpAnswerReceived_?.Invoke(offerId, sdpDescription);
        return Task.CompletedTask;
    }

    public Task IceCandidateReceived(ConnAttemptId offerId, IceCandidate iceCandidate)
    {
        if (IceCandidateReceived_ is not null)
        {
            IceCandidateReceived_.Invoke(offerId, iceCandidate);
            return Task.CompletedTask;
        }

        var succeed = receivedIceCandidates.TryAdd(offerId, [iceCandidate]);
        if (!succeed)
        {
            receivedIceCandidates.Remove(offerId, out var prevIceCandidates);
            receivedIceCandidates.Add(offerId, [..prevIceCandidates ?? [], iceCandidate]);
        }

        return Task.CompletedTask;
    }
}

public partial class Signaling : Node
{
    private HubConnection connection = null!;
    public ISignalingHub Hub { get; private set; } = null!;
    public SignalingHubClient Client { get; private set; } = null!;

    /// <summary>
    /// Get the SignalR connection state
    /// </summary>
    public HubConnectionState GetConnectionState() => connection.State;

    private ILogger Log = null!;

    public override async void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", "Signaling/SignalR");

        connection = new HubConnectionBuilder()
            .WithUrl(Secrets.SignalingHubUrl)
            .WithAutomaticReconnect()
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(Log);
            })
            .AddJsonProtocol(options =>
            {
                // Enable F# types serialization
                JsonFSharpOptions
                    .Default()
                    .AddToJsonSerializerOptions(options.PayloadSerializerOptions);
            })
            .Build();

        connection.Closed += error => Task.Run(() => Log.Error("SignalR connection closed"));

        // Creating typed hub
        Hub = connection.CreateHubProxy<ISignalingHub>();

        // Connecting the client (the class that handle hub requests) to the hub
        Client = new SignalingHubClient();
        connection.Register(Client as ISignalingClient);

        // Starting the connection
        try
        {
            await connection.StartAsync();
            Log.Information("SignalR connected !");
        }
        catch (Exception exn)
        {
            Log.Error($"Failed to connect to signaling hub: {exn.Message}");
        }
    }
}
