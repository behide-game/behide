namespace Behide.Networking;

using Godot;
using System;
using System.Threading.Tasks;
using Fractural.Tasks;
using Behide.OnlineServices;


/// <summary>
/// Interface of Peer.gd
/// </summary>
partial class PeerConnection : GodotObject
{
    public event Action<SdpDescription>? SessionDescriptionCreated;
    public event Action<IceCandidate>? IceCandidateCreated;
    public IceCandidate[] createdIceCandidates = [];

    private Godot.Collections.Dictionary iceServers = new() {
        {
            "iceServers",
            new Godot.Collections.Array {
                new Godot.Collections.Dictionary {
                    { "urls", "stun:stun.relay.metered.ca:80" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:80" },
                    { "username", "11822db4820edb041e79dfac" },
                    { "credential", "Y1jxzNaC9poLjl7t" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:80?transport=tcp" },
                    { "username", "11822db4820edb041e79dfac" },
                    { "credential", "Y1jxzNaC9poLjl7t" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:443" },
                    { "username", "11822db4820edb041e79dfac" },
                    { "credential", "Y1jxzNaC9poLjl7t" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:a.relay.metered.ca:443?transport=tcp" },
                    { "username", "11822db4820edb041e79dfac" },
                    { "credential", "Y1jxzNaC9poLjl7t" }
                }
            }
        }
    };

    private WebRtcPeerConnection peer;

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
            createdIceCandidates = [.. createdIceCandidates, ic];
            IceCandidateCreated?.Invoke(ic);
        };

        peer.Initialize(iceServers);
    }

    // To check (use channel state instead)
    public bool IsConnected() => peer.GetConnectionState() == WebRtcPeerConnection.ConnectionState.Connected;

    public Error Poll() => peer.Poll();
    public Error CreateOffer() => peer.CreateOffer();
    public void SetRemoteSdpDescription(SdpDescription sdp) => peer.SetRemoteDescription(sdp.type, sdp.sdp);
    public void AddIceCandidate(IceCandidate ice) => peer.AddIceCandidate(ice.media, ice.index, ice.name);

    public WebRtcPeerConnection GetPeerConnection() => peer;
}


/// <summary>
/// <para>This class manage a WebRTC peer connection.</para>
/// <para>TLDR: It connects WebRTC using a Signaling instance.</para>
/// <para>It fetches offer, publishes answer and exchanges ice candidates using the Signaling instance passed in his constructor.</para>
/// </summary>
class ClientPeerConnection(Signaling signaling, OfferId offerId)
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

        // Send
        peer.IceCandidateCreated += ic => _ = signaling.SendIceCandidate(offerId, ic);
        foreach (var ic in peer.createdIceCandidates) _ = signaling.SendIceCandidate(offerId, ic);
    }

    public WebRtcPeerConnection GetPeerConnection() => peer.GetPeerConnection();
}

/// <summary>
/// <para>This class manage a WebRTC peer connection.</para>
/// <para>It publishes offer and exchanges ice candidates using the Signaling instance passed in it's constructor.</para>
/// </summary>
class HostPeerConnection(Signaling signaling)
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

        // Send
        peer.IceCandidateCreated += ic => _ = signaling.SendIceCandidate(offerId, ic);
        foreach (var ic in peer.createdIceCandidates) _ = signaling.SendIceCandidate(offerId, ic);
    }

    public WebRtcPeerConnection GetPeerConnection() => peer.GetPeerConnection();
}
