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

class SignalingHub
{
    private HubConnection hub = null!;
    public event Action<SdpDescription>? SdpAnswerReceived;
    public event Action<IceCandidate>? IceCandidateReceived;
    public event Func<Task<OfferId?>>? OfferIdCreationRequested;

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

        hub.On<SdpDescription>("SdpAnswerReceived", answer => SdpAnswerReceived?.Invoke(answer));
        hub.On<IceCandidate>("IceCandidateReceived", iceCandidate => IceCandidateReceived?.Invoke(iceCandidate));

        hub.On("CreateOffer", async () =>
        {
            Task<OfferId?>? func = OfferIdCreationRequested?.Invoke();
            OfferId? res = func is null ? null : await func;

            return Option<OfferId>.FromNullable(res);
        });
    }

    private async Task<Result<T>> InvokeOption<T>(string methodName, object? arg = null)
    {
        // GD.Print($"Invoking \"{methodName}\" on hub with {arg} as parameter");
        var res = arg is null
            ? await hub.InvokeAsync<FSharpOption<T>>(methodName)
            : await hub.InvokeAsync<FSharpOption<T>>(methodName, arg);

        if (FSharpOption<T>.get_IsSome(res))
            return new Result<T>.Ok(res.Value);
        else
            return new Result<T>.Error("400 like error");
    }

    public Task<Result<RoomId>> CreateRoom() => InvokeOption<RoomId>("CreateRoom");
    public Task<Result<OfferId>> JoinRoom(RoomId roomId) => InvokeOption<OfferId>("JoinRoom", roomId);

    public Task<Result<OfferId>>        AddOffer(SdpDescription sdp) => InvokeOption<OfferId>("AddOffer", sdp);
    public Task<bool>                   DeleteOffer(OfferId offerId) => hub.InvokeAsync<bool>("DeleteOffer", offerId);
    public Task<Result<SdpDescription>> GetOffer(OfferId offerId)    => InvokeOption<SdpDescription>("GetOffer", offerId);

    public Task SendAnswer(OfferId offerId, SdpDescription sdp)     => hub.SendAsync("SendAnswer", offerId, sdp);
    public Task SendIceCandidate(OfferId offerId, IceCandidate ice) => hub.SendAsync("SendIceCandidate", offerId, ice);
}

public partial class Signaling : Node
{
    private readonly SignalingHub hub = new();
    public IceCandidate[] receivedIceCandidates = [];

    public event Action<SdpDescription>? SdpAnswerReceived;
    public event Action<IceCandidate>? IceCandidateReceived;
    public event Func<Task<OfferId>>? OfferIdCreationRequested;

    public override async void _EnterTree()
    {
        hub.SdpAnswerReceived += sdp => SdpAnswerReceived?.Invoke(sdp);
        hub.OfferIdCreationRequested += async () =>
        {
            Task<OfferId>? func = OfferIdCreationRequested?.Invoke();
            OfferId? res = func is null ? null : await func;
            return res;
        };
        hub.IceCandidateReceived += ic =>
        {
            if (IceCandidateReceived is null)
                receivedIceCandidates = [..receivedIceCandidates, ic];
            else
                IceCandidateReceived.Invoke(ic);
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
    public Task<Result<OfferId>> JoinRoom(RoomId roomId) => hub.JoinRoom(roomId);

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
