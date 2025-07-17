using Godot;
using System;
using System.Threading.Tasks;
using System.Reactive.Subjects;
using Behide.OnlineServices.Signaling;

namespace Behide.Networking;

/// <summary>
/// Extension of WebRtcPeerConnection
/// </summary>
public partial class PeerConnection : WebRtcPeerConnection
{
    private readonly ReplaySubject<IceCandidate> iceCandidates = new();
    private readonly Godot.Collections.Dictionary iceServers = new() {
        {
            "iceServers",
            new Godot.Collections.Array {
                new Godot.Collections.Dictionary {
                    { "urls", "stun:stun.relay.metered.ca:80" }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:standard.relay.metered.ca:80" },
                    { "username", Secrets.RelayUsername },
                    { "credential", Secrets.RelayPassword }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:standard.relay.metered.ca:80?transport=tcp" },
                    { "username", Secrets.RelayUsername },
                    { "credential", Secrets.RelayPassword }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:standard.relay.metered.ca:443" },
                    { "username", Secrets.RelayUsername },
                    { "credential", Secrets.RelayPassword }
                },
                new Godot.Collections.Dictionary {
                    { "urls", "turn:standard.relay.metered.ca:443?transport=tcp" },
                    { "username", Secrets.RelayUsername },
                    { "credential", Secrets.RelayPassword }
                }
            }
        }
    };

    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", "PeerConnection");

    public PeerConnection()
    {
        IceCandidateCreated += (media, index, name) =>
        {
            var ic = new IceCandidate(media, (int)index, name);
            iceCandidates.OnNext(ic);
        };

        Initialize(iceServers);
    }

    private bool IsConnected() => GetConnectionState() == ConnectionState.Connected;
    private void SetRemoteSdpDescription(SdpDescription sdp) => SetRemoteDescription(sdp.type, sdp.sdp);
    private void AddIceCandidate(IceCandidate ice) => AddIceCandidate(ice.media, ice.index, ice.name);

    /// <summary>
    /// Publish an offer on the signaling server, create a connection attempt and handle connection.
    /// </summary>
    /// <returns>Return a connection attempt id</returns>
    public Task<ConnectionAttemptId> PublishOffer(Signaling signaling)
    {
        var tcs = new TaskCompletionSource<ConnectionAttemptId>();

        // SDP offer created => Create a connection attempt by publishing the offer
        SessionDescriptionCreated += async (type, sdp) =>
        {
            var connAttempt = await signaling.StartConnectionAttempt(new SdpDescription(type, sdp));
            tcs.TrySetResult(connAttempt.Id);
            log.Debug("Connection attempt created: {ConnectionAttemptId}", connAttempt);

            var answer = await connAttempt.WaitAnswer();
            log.Debug("Answer received");
            SetRemoteSdpDescription(answer);

            // Exchange ice candidates
            var sub1 = connAttempt.Candidates.Subscribe(AddIceCandidate);
            var sub2 = iceCandidates.Subscribe(connAttempt.SendIceCandidate);
            log.Debug("Exchanging ice candidates");

            await TaskEx.WaitUntil(IsConnected);
            sub1.Dispose();
            sub2.Dispose();

            log.Information("Connection established, signaling the server");
            connAttempt.End();
        };

        CreateOffer(); // Create offer and trigger the event
        log.Debug("Creating offer");
        return tcs.Task;
    }

    /// <summary>
    /// Join connection attempt, send answer and handle ice candidate exchange
    /// </summary>
    /// <remarks>Returns only when connection is established</remarks>
    public async Task AnswerConnectionOffer(Signaling signaling, ConnectionAttemptId connectionAttemptId)
    {
        var tcs = new TaskCompletionSource();

        // Retrieve offer
        log.Debug("Joining connection attempt {ConnectionAttemptId}", connectionAttemptId);
        var connAttempt = await signaling.JoinConnectionAttempt(connectionAttemptId);

        // Send answer when created
        SessionDescriptionCreated += async (type, sdp) =>
        {
            var answer = new SdpDescription(type, sdp);
            connAttempt.SendAnswer(answer);
            log.Debug("Answer sent");

            // Exchange ice candidates
            var sub1 = connAttempt.Candidates.Subscribe(AddIceCandidate);
            var sub2 = iceCandidates.Subscribe(connAttempt.SendIceCandidate);
            log.Debug("Exchanging ice candidates");

            // Await connection
            await TaskEx.WaitUntil(IsConnected);
            sub1.Dispose();
            sub2.Dispose();
            tcs.SetResult();
        };

        SetRemoteSdpDescription(connAttempt.Offer);
        await tcs.Task;
        log.Information("Connection established");
    }
}
