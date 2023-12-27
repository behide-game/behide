namespace Behide.OnlineServices;

using Godot;

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FSharp.Core;

using Behide.GodotInterop;
using System.Collections.Generic;


class SignalingHub
{
    private HubConnection hub = null!;
    public event Action<OfferId, SdpDescription>? SdpAnswerReceived;
    public event Action<OfferId, IceCandidate>? IceCandidateReceived;
    public event Func<int, Task<OfferId?>>? OfferIdCreationRequested;

    public async Task<Result> Start()
    {
        try
        {
            hub = new HubConnectionBuilder()
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

            SubscribeSignalREvents();

            await hub.StartAsync();
            return new Result.Ok();
        }
        catch (Exception exn)
        {
            return new Result.Error($"Failed to connect to signaling hub: {exn.Message}");
        }
    }

    private void SubscribeSignalREvents()
    {
        hub.Closed += error => Task.Run(() => GD.PrintErr("SignalR connection closed"));

        hub.On<OfferId, SdpDescription>("SdpAnswerReceived", (offerId, answer) => SdpAnswerReceived?.Invoke(offerId, answer));
        hub.On<OfferId, IceCandidate>("IceCandidateReceived", (offerId, iceCandidate) => IceCandidateReceived?.Invoke(offerId, iceCandidate));

        hub.On<int, FSharpOption<OfferId>>("CreateOffer", async askingPeerId =>
        {
            var res = OfferIdCreationRequested is null
                ? null
                : await OfferIdCreationRequested.Invoke(askingPeerId);

            return Option<OfferId>.FromNullable(res);
        });
    }

    private async Task<Result<T>> InvokeResult<T>(string methodName, object? arg = null)
    {
        var res = arg is null
            ? await hub.InvokeAsync<FSharpResult<T, string>>(methodName)
            : await hub.InvokeAsync<FSharpResult<T, string>>(methodName, arg);

        return res.IsOk
            ? new Result<T>.Ok(res.ResultValue)
            : new Result<T>.Error(res.ErrorValue);
    }

    public Task<Result<RoomId>> CreateRoom() => InvokeResult<RoomId>("CreateRoom");
    public Task<Result<JoinRoomRes>> JoinRoom(RoomId roomId) => InvokeResult<JoinRoomRes>("JoinRoom", roomId);

    public Task<Result<OfferId>>        AddOffer(SdpDescription sdp) => InvokeResult<OfferId>("AddOffer", sdp);
    // public Task<bool>                   DeleteOffer(OfferId offerId) => hub.InvokeAsync<bool>("DeleteOffer", offerId);
    public Task<Result<SdpDescription>> GetOffer(OfferId offerId)    => InvokeResult<SdpDescription>("GetOffer", offerId);

    public Task SendAnswer(OfferId offerId, SdpDescription sdp)     => hub.SendAsync("SendAnswer", offerId, sdp);
    public Task SendIceCandidate(OfferId offerId, IceCandidate ice) => hub.SendAsync("SendIceCandidate", offerId, ice);
}

public partial class Signaling : Node
{
    private readonly SignalingHub hub = new();
    public Dictionary<OfferId, IceCandidate[]> receivedIceCandidates = [];

    public event Action<OfferId, SdpDescription>? SdpAnswerReceived;
    public event Action<OfferId, IceCandidate>? IceCandidateReceived;
    public event Func<int, Task<OfferId>>? OfferIdCreationRequested;

    public override async void _EnterTree()
    {
        hub.SdpAnswerReceived += (offerId, sdp) => SdpAnswerReceived?.Invoke(offerId, sdp);

        hub.OfferIdCreationRequested += async askingPeerId =>
            OfferIdCreationRequested is null
                ? null
                : await OfferIdCreationRequested.Invoke(askingPeerId);

        hub.IceCandidateReceived += (offerId, iceCandidate) =>
        {
            if (IceCandidateReceived is not null)
            {
                IceCandidateReceived.Invoke(offerId, iceCandidate);
                return;
            }

            var succeed = receivedIceCandidates.TryAdd(offerId, [iceCandidate]);
            if (!succeed)
            {
                receivedIceCandidates.Remove(offerId, out var prevIceCandidates);
                receivedIceCandidates.Add(offerId, [..prevIceCandidates, iceCandidate]);
            }
        };


        switch (await hub.Start())
        {
            case Result.Ok: GD.Print("SignalR connected !"); break;
            case Result.Error res:
                GD.PrintErr("Failed to connect to behide online services: " + res.Failure);
                break;
        };
    }


    public Task<Result<RoomId>> CreateRoom() => hub.CreateRoom();
    public Task<Result<JoinRoomRes>> JoinRoom(RoomId roomId) => hub.JoinRoom(roomId);

    public async Task<OfferId> AddOffer(SdpDescription sdp)
    {
        switch (await hub.AddOffer(sdp)) {
            case Result<OfferId>.Ok offerId: return offerId.Value;
            case Result<OfferId>.Error error:
                var msg = $"Failed to add offer: {error.Failure}";
                GD.PrintErr(msg);
                throw new Exception(msg);
            default: throw new Exception("Result type not valid");
        }
    }

    public async Task<SdpDescription> GetOffer(OfferId offerId)
    {
        switch (await hub.GetOffer(offerId)) {
            case Result<SdpDescription>.Ok sdp: return sdp.Value;
            case Result<SdpDescription>.Error error:
                var msg = $"Failed to retrieve offer {offerId}: {error.Failure}";
                GD.PrintErr(msg);
                throw new Exception(msg);
            default: throw new Exception("Result type not valid");
        }
    }

    public Task SendAnswer(OfferId offerId, SdpDescription sdpAnswer) => hub.SendAnswer(offerId, sdpAnswer);
    public Task SendIceCandidate(OfferId offerId, IceCandidate iceCandidate) => hub.SendIceCandidate(offerId, iceCandidate);
}
