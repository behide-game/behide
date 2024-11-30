namespace Behide.Networking;

using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive;
using Behide.Types;
using Behide.OnlineServices.Signaling;
using Behide.OnlineServices.Client;
using Behide.I18n.BOS.Errors;
using Serilog;



/// <summary>
/// Extension of WebRtcPeerConnection TODO description
/// </summary>
partial class PeerConnection : WebRtcPeerConnection
{
    new public event Action<SdpDescription>? SessionDescriptionCreated;
    new public event Action<IceCandidate>? IceCandidateCreated;
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

    public PeerConnection()
    {
        (this as WebRtcPeerConnection).SessionDescriptionCreated += (type, sdp) => SessionDescriptionCreated?.Invoke(new SdpDescription(type, sdp));
        (this as WebRtcPeerConnection).IceCandidateCreated += (string media, long index, string name) =>
        {
            var ic = new IceCandidate(media, (int)index, name);
            if (IceCandidateCreated is null)
                createdIceCandidates = [..createdIceCandidates, ic];
            else
                IceCandidateCreated.Invoke(ic);
        };

        Initialize(iceServers);
    }

    public bool IsConnected() => GetConnectionState() == ConnectionState.Connected;

    public void SetRemoteSdpDescription(SdpDescription sdp) => SetRemoteDescription(sdp.type, sdp.sdp);
    public void AddIceCandidate(IceCandidate ice) => AddIceCandidate(ice.media, ice.index, ice.name);
}

class PeerConnector(Signaling signaling)
{
    protected readonly PeerConnection peerConnection = new();
    protected readonly Signaling signaling = signaling;

    protected void ExchangeIceCandidates(ConnAttemptId offerId)
    {
        // Receive
        // Handle new ice candidates
        signaling.Client.IceCandidateReceived_ += (receivedConnAttemptId, ice) =>
        {
            if (offerId.Equals(receivedConnAttemptId))
                peerConnection.AddIceCandidate(ice.media, ice.index, ice.name);
        };

        // Handle ice candidates before
        var receivedIceCandidates = signaling.Client.receivedIceCandidates.GetValueOrDefault(offerId);
        foreach (var ic in receivedIceCandidates ?? []) peerConnection.AddIceCandidate(ic);
        signaling.Client.receivedIceCandidates.Remove(offerId);

        // Send
        peerConnection.IceCandidateCreated += ic => _ = signaling.Hub.SendIceCandidate(offerId, ic);
        foreach (var ic in peerConnection.createdIceCandidates) _ = signaling.Hub.SendIceCandidate(offerId, ic);
    }

    public WebRtcPeerConnection GetConnection() => peerConnection;
}

/// <summary>
/// <para>This class manage a WebRTC peer connection.</para>
/// <para>It fetches connection attempt's offer, publishes answer and exchanges ice candidates.</para>
/// </summary>
class AnswerPeerConnector(Signaling signaling, ConnAttemptId connAttemptId) : PeerConnector(signaling)
{
    private readonly ConnAttemptId connAttemptId = connAttemptId;

    /// <summary>
    /// Connect the underlying RTC peer.
    /// </summary>
    /// <remarks>This method is awaitable.</remarks>
    public async Task<Result<Unit, string>> Connect()
    {
        // Get connection attempt offer
        var offerRes = await signaling.Hub.JoinConnectionAttempt(connAttemptId);
        if (offerRes.IsError)
        {
            return offerRes.ErrorValue.ToLocalizedString();
        }

        var offer = offerRes.ResultValue;

        // Await local sdp answer
        var sendAnswerResTcs = new TaskCompletionSource<Option<Errors.SendAnswerError>>();
        peerConnection.SessionDescriptionCreated += async sdp =>
        {
            // Answer generated. Send it to the other peer
            var res = await signaling.Hub.SendAnswer(connAttemptId, sdp);

            if (res.IsError)
                sendAnswerResTcs.TrySetResult(Option.Some(res.ErrorValue));
            else
            {
                sendAnswerResTcs.TrySetResult(Option.None<Errors.SendAnswerError>());
                ExchangeIceCandidates(connAttemptId);
            }
        };

        // Inject offer into local peer
        peerConnection.SetRemoteSdpDescription(offer);

        // Await local sdp answer to be sent
        var sendAnswerError = await sendAnswerResTcs.Task;
        if (sendAnswerError.HasValue(out var error))
        {
            return error.ToLocalizedString();
        }

        // Await connection
        await TaskEx.WaitUntil(peerConnection.IsConnected);
        return Unit.Default;
    }
}

/// <summary>
/// <para>This class manage a WebRTC peer connection.</para>
/// <para>It publishes offer, receives answer and exchanges ice candidates.</para>
/// </summary>
class OfferPeerConnector(Signaling signaling) : PeerConnector(signaling)
{
    /// <summary>
    /// Create a connection attempt
    /// Publish an offer
    /// and handle connection
    /// </summary>
    /// <returns>Return the ID of the offer published on the signaling server</returns>
    public async Task<Result<ConnAttemptId, string>> CreateConnectionAttempt()
    {
        var createConnAttemptResTcs = new TaskCompletionSource<Result<ConnAttemptId, Errors.StartConnectionAttemptError>>();

        // SDP offer created => Create a connection attempt by publishing the offer
        peerConnection.SessionDescriptionCreated += async sdp =>
        {
            var res = await signaling.Hub.StartConnectionAttempt(sdp);
            createConnAttemptResTcs.TrySetResult(res.ToResult());
        };

        peerConnection.CreateOffer(); // Create offer and trigger the event


        // Await the creation of the connection attempt
        var createConnAttemptRes = await createConnAttemptResTcs.Task;
        if (createConnAttemptRes.HasError(out var error))
        {
            return error.ToLocalizedString();
        }
        var connAttemptId = createConnAttemptRes.Value;

        // Handle answer
        signaling.Client.SdpAnswerReceived_ += async (receivedConnAttemptId, answer) =>
        {
            if (!receivedConnAttemptId.Equals(connAttemptId)) return;

            peerConnection.SetRemoteSdpDescription(answer);
            ExchangeIceCandidates(connAttemptId);

            await TaskEx.WaitUntil(peerConnection.IsConnected);

            var endConnAttemptRes = await signaling.Hub.EndConnectionAttempt(connAttemptId);
            if (endConnAttemptRes.HasError(out var error))
                Log.Error(error.ToLocalizedString());
        };

        return connAttemptId;
    }
}
