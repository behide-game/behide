namespace Behide.Networking;

using Godot;
using System;
using System.Threading.Tasks;
using Fractural.Tasks;
using Behide.OnlineServices;


/// <summary>
/// Interface of Peer.gd
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
                    { "username", "TO REPLACE WITH SECRETS" },
                    { "credential", "TO REPLACE WITH SECRETS" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:80?transport=tcp" },
                    { "username", "TO REPLACE WITH SECRETS" },
                    { "credential", "TO REPLACE WITH SECRETS" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:443" },
                    { "username", "TO REPLACE WITH SECRETS" },
                    { "credential", "TO REPLACE WITH SECRETS" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:443?transport=tcp" },
                    { "username", "TO REPLACE WITH SECRETS" },
                    { "credential", "TO REPLACE WITH SECRETS" }
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
    private readonly PeerConnection peer = new();

    /// <summary>
    /// Connect the underlying RTC peer.
    /// </summary>
    /// <remarks>This method is awaitable.</remarks>
    public async Task Connect()
    {
        // Fetch offer
        var offer = await signaling.GetOffer(offerId);

        // Await local sdp
        peer.SessionDescriptionCreated += async sdp =>
        {
            await signaling.SendAnswer(offerId, sdp);
            ExchangeIceCandidates(offerId);
        };

        // Inject offer into local peer
        peer.SetRemoteSdpDescription(offer);

        await GDTask.WaitUntil(peer.IsConnected);
    }

    private void ExchangeIceCandidates(OfferId offerId)
    {
        // Receive
        foreach (var ic in signaling.receivedIceCandidates) peer.AddIceCandidate(ic);
        signaling.IceCandidateReceived += peer.AddIceCandidate;
        signaling.receivedIceCandidates = [];

        // Send
        peer.IceCandidateCreated += ic => _ = signaling.SendIceCandidate(offerId, ic);
        foreach (var ic in peer.createdIceCandidates) _ = signaling.SendIceCandidate(offerId, ic);
    }

    public WebRtcPeerConnection GetPeerConnection() => peer.GetPeerConnection();
}

/// <summary>
/// <para>This class manage a WebRTC peer connection.</para>
/// <para>It publishes offer, receives answer and exchanges ice candidates.</para>
/// </summary>
class OfferPeerConnection(Signaling signaling)
{
    private readonly PeerConnection peer = new();
    public event Action? PeerConnected;

    /// <summary>
    /// Publish an offer and handle connection
    /// </summary>
    /// <returns>Return the ID of the offer published on the signaling server</returns>
    public async Task<OfferId> CreateOffer()
    {
        var offerIdTcs = new TaskCompletionSource<OfferId>();

        // SDP offer created => Publish offer
        peer.SessionDescriptionCreated += async sdp =>
        {
            var offerId = await signaling.AddOffer(sdp);
            offerIdTcs.TrySetResult(offerId);
        };

        // Handle answer
        signaling.SdpAnswerReceived += async answer =>
        {
            peer.SetRemoteSdpDescription(answer);
            ExchangeIceCandidates(await offerIdTcs.Task);

            await GDTask.WaitUntil(peer.IsConnected);
            PeerConnected?.Invoke();
        };

        peer.CreateOffer();
        return await offerIdTcs.Task;
    }

    private void ExchangeIceCandidates(OfferId offerId)
    {
        // Receive
        signaling.IceCandidateReceived += peer.AddIceCandidate;
        foreach (var ic in signaling.receivedIceCandidates) peer.AddIceCandidate(ic);
        signaling.receivedIceCandidates = [];

        // Send
        peer.IceCandidateCreated += ic => _ = signaling.SendIceCandidate(offerId, ic);
        foreach (var ic in peer.createdIceCandidates) _ = signaling.SendIceCandidate(offerId, ic);
    }

    public WebRtcPeerConnection GetPeerConnection() => peer.GetPeerConnection();
}
