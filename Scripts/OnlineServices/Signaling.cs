namespace Behide.OnlineServices.Client;

using Godot;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FSharp.Core;
using TypedSignalR.Client;

using Behide.GodotInterop;
using Behide.Types;
using Behide.OnlineServices.Signaling;




public class SignalingHubClient : ISignalingClient
{
    public event Func<int, Task<Option<ConnAttemptId>>>? ConnAttemptIdCreationRequested;
    public event Action<ConnAttemptId, SdpDescription>? SdpAnswerReceived_;
    public event Action<ConnAttemptId, IceCandidate>? IceCandidateReceived_;
    public Dictionary<ConnAttemptId, IceCandidate[]> receivedIceCandidates = [];

    public async Task<FSharpOption<ConnAttemptId>> CreateOffer(int askingPeerId)
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
            receivedIceCandidates.Add(offerId, [..prevIceCandidates, iceCandidate]);
        }

        return Task.CompletedTask;
    }

}

public partial class Signaling : Node
{
    private HubConnection connection = null!;
    public ISignalingHub Hub { get; private set; } = null!;
    public SignalingHubClient Client { get; private set; } = null!;

    public override async void _EnterTree()
    {
        connection = new HubConnectionBuilder()
            .WithUrl(Secrets.SignalingHubUrl)
            .WithAutomaticReconnect()
            .ConfigureLogging(options =>
            {
                options.AddProvider(new GodotLoggerProvider());
                options.SetMinimumLevel(LogLevel.Warning);
            })
            .AddJsonProtocol(options =>
            {
                JsonFSharpOptions
                    .Default()
                    .AddToJsonSerializerOptions(options.PayloadSerializerOptions);
            })
            .Build();

        connection.Closed += error => Task.Run(() => GD.PrintErr("SignalR connection closed"));

        // Creating typed hub
        Hub = connection.CreateHubProxy<ISignalingHub>();

        // Connecting the client (the class that handle hub requests) to the hub
        Client = new SignalingHubClient();
        connection.Register(Client as ISignalingClient);

        // Starting the connection
        try
        {
            await connection.StartAsync();
            GD.Print("SignalR connected !");
        }
        catch (Exception exn)
        {
            GD.PrintErr($"Failed to connect to signaling hub: {exn.Message}");
        }
    }
}
