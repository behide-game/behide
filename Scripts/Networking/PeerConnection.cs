namespace Behide.Networking;

using Godot;
using System;
using System.Threading.Tasks;
using Fractural.Tasks;
using Behide.OnlineServices;
using System.Collections.Generic;



/// <summary>
/// Use Peer.gd to instantiate a WebRtcPeerConnection
/// See https://github.com/godotengine/webrtc-native/issues/116
/// Wait https://github.com/godotengine/godot/pull/84947 to be merged
/// </summary>
class PeerConnection
{
    public event Action<SdpDescription>? SessionDescriptionCreated;
    public event Action<IceCandidate>? IceCandidateCreated;
    public IceCandidate[] createdIceCandidates = [];

    private readonly Godot.Collections.Dictionary iceServers = new() {
        {
            "iceServers",
            new Godot.Collections.Array {
                new Godot.Collections.Dictionary {
                    { "urls", "stun:stun.relay.metered.ca:80" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:80" },
                    { "username", Secrets.RelayUsername },
                    { "credential", Secrets.RelayPassword }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:80?transport=tcp" },
                    { "username", Secrets.RelayUsername },
                    { "credential", Secrets.RelayPassword }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:443" },
                    { "username", Secrets.RelayUsername },
                    { "credential", Secrets.RelayPassword }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:443?transport=tcp" },
                    { "username", Secrets.RelayUsername },
                    { "credential", Secrets.RelayPassword }
                }
            }
        }
    };

    private readonly WebRtcPeerConnection peer;

    public PeerConnection()
    {
        peer = GD.Load<GDScript>("res://Scripts/Networking/Peer.gd")
            .New()
            .AsGodotObject()
            .Get("peer")
            .As<WebRtcPeerConnection>();

        peer.SessionDescriptionCreated += (type, sdp) => SessionDescriptionCreated?.Invoke(new SdpDescription(type, sdp));
        peer.IceCandidateCreated += (string media, long index, string name) =>
        {
            var ic = new IceCandidate(media, (int)index, name);
            if (IceCandidateCreated is null)
                createdIceCandidates = [..createdIceCandidates, ic];
            else
                IceCandidateCreated.Invoke(ic);
        };

        peer.Initialize(iceServers);
    }

    public bool IsConnected() => peer.GetConnectionState() == WebRtcPeerConnection.ConnectionState.Connected;

    public Error Poll() => peer.Poll();
    public Error CreateOffer() => peer.CreateOffer();
    public void SetRemoteSdpDescription(SdpDescription sdp) => peer.SetRemoteDescription(sdp.type, sdp.sdp);
    public void AddIceCandidate(IceCandidate ice) => peer.AddIceCandidate(ice.media, ice.index, ice.name);

    public WebRtcPeerConnection GetPeerConnection() => peer;
}


/// <summary>
/// <para>This class manage a WebRTC peer connection.</para>
/// <para>It fetches offer, publishes answer and exchanges ice candidates.</para>
/// </summary>
class AnswerPeerConnection(Signaling signaling, OfferId offerId)
{
    private readonly PeerConnection peerConnection = new();

    /// <summary>
    /// Connect the underlying RTC peer.
    /// </summary>
    /// <remarks>This method is awaitable.</remarks>
    public async Task Connect()
    {
        // Fetch offer
        var offer = await signaling.GetOffer(offerId);

        // Await local sdp
        peerConnection.SessionDescriptionCreated += async sdp =>
        {
            await signaling.SendAnswer(offerId, sdp);
            ExchangeIceCandidates(offerId);
        };

        // Inject offer into local peer
        peerConnection.SetRemoteSdpDescription(offer);

        await GDTask.WaitUntil(peerConnection.IsConnected);
    }

    private void ExchangeIceCandidates(OfferId offerId)
    {
        // Receive
        signaling.IceCandidateReceived += (receivedOfferId, iceCandidate) =>
        {
            if (offerId.Equals(receivedOfferId))
                peerConnection.AddIceCandidate(iceCandidate);
        };

        var receivedIceCandidates = signaling.receivedIceCandidates.GetValueOrDefault(offerId);
        foreach (var ic in receivedIceCandidates ?? []) peerConnection.AddIceCandidate(ic);
        signaling.receivedIceCandidates.Remove(offerId);

        // Send
        peerConnection.IceCandidateCreated += ic => _ = signaling.SendIceCandidate(offerId, ic);
        foreach (var ic in peerConnection.createdIceCandidates) _ = signaling.SendIceCandidate(offerId, ic);
    }

    public WebRtcPeerConnection GetPeerConnection() => peerConnection.GetPeerConnection();
}

/// <summary>
/// <para>This class manage a WebRTC peer connection.</para>
/// <para>It publishes offer, receives answer and exchanges ice candidates.</para>
/// </summary>
class OfferPeerConnection(Signaling signaling)
{
    private readonly PeerConnection peerConnection = new();
    public event Action? PeerConnected;

    /// <summary>
    /// Publish an offer and handle connection
    /// </summary>
    /// <returns>Return the ID of the offer published on the signaling server</returns>
    public async Task<OfferId> CreateOffer()
    {
        var offerIdTcs = new TaskCompletionSource<OfferId>();

        // SDP offer created => Publish offer
        peerConnection.SessionDescriptionCreated += async sdp =>
        {
            var offerId = await signaling.AddOffer(sdp);
            offerIdTcs.TrySetResult(offerId);
        };

        // Handle answer
        signaling.SdpAnswerReceived += async (offerId, answer) =>
        {
            if (!offerId.Equals(await offerIdTcs.Task)) return;

            peerConnection.SetRemoteSdpDescription(answer);
            ExchangeIceCandidates(await offerIdTcs.Task);

            await GDTask.WaitUntil(peerConnection.IsConnected);
            PeerConnected?.Invoke();
        };

        peerConnection.CreateOffer();
        return await offerIdTcs.Task;
    }

    private void ExchangeIceCandidates(OfferId offerId)
    {
        // Receive
        signaling.IceCandidateReceived += (receivedOfferId, iceCandidate) =>
        {
            if (offerId.Equals(receivedOfferId))
                peerConnection.AddIceCandidate(iceCandidate);
        };

        var receivedIceCandidates = signaling.receivedIceCandidates.GetValueOrDefault(offerId);
        foreach (var ic in receivedIceCandidates ?? []) peerConnection.AddIceCandidate(ic);
        signaling.receivedIceCandidates.Remove(offerId);

        // Send
        peerConnection.IceCandidateCreated += ic => _ = signaling.SendIceCandidate(offerId, ic);
        foreach (var ic in peerConnection.createdIceCandidates) _ = signaling.SendIceCandidate(offerId, ic);
    }

    public WebRtcPeerConnection GetPeerConnection() => peerConnection.GetPeerConnection();
}
