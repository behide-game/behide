namespace Behide.I18n.BOS.Errors;

using Behide.OnlineServices.Signaling;


public static class ErrorsExtensions {
    public static string ToLocalizedString(this Errors.StartConnectionAttemptError error) =>
        error switch
        {
            Errors.StartConnectionAttemptError.PlayerConnectionNotFound => "Failed to start a connection attempt: Player connection not found",
            Errors.StartConnectionAttemptError.FailedToCreateConnAttempt => "Failed to start a connection attempt: Failed to create a connection attempt",
            Errors.StartConnectionAttemptError.FailedToUpdatePlayerConnection => "Failed to start a connection attempt: Failed to update player connection",
            _ => "Failed to start a connection attempt: Unknown error"
        };

    public static string ToLocalizedString(this Errors.JoinConnectionAttemptError error) =>
        error switch
        {
            Errors.JoinConnectionAttemptError.PlayerConnectionNotFound => "Failed to join a connection attempt: Player connection not found",
            Errors.JoinConnectionAttemptError.OfferNotFound => "Failed to join a connection attempt: Offer not found",
            Errors.JoinConnectionAttemptError.OfferAlreadyAnswered => "Failed to join a connection attempt: Offer already answered",
            Errors.JoinConnectionAttemptError.InitiatorCannotJoin => "Failed to join a connection attempt: Initiator cannot join",
            Errors.JoinConnectionAttemptError.FailedToUpdateOffer => "Failed to join a connection attempt: Failed to update offer",
            _ => "Failed to join a connection attempt: Unknown error"
        };

    public static string ToLocalizedString(this Errors.SendAnswerError error) =>
        error switch
        {
            Errors.SendAnswerError.PlayerConnectionNotFound => "Failed to send an answer: Player connection not found",
            Errors.SendAnswerError.OfferNotFound => "Failed to send an answer: Offer not found",
            Errors.SendAnswerError.NotAnswerer => "Failed to send an answer: Not answerer",
            _ => "Failed to send an answer: Unknown error"
        };

    public static string ToLocalizedString(this Errors.SendIceCandidateError error) =>
        error switch
        {
            Errors.SendIceCandidateError.PlayerConnectionNotFound => "Failed to send an ice candidate: Player connection not found",
            Errors.SendIceCandidateError.OfferNotFound => "Failed to send an ice candidate: Offer not found",
            Errors.SendIceCandidateError.NotAnswerer => "Failed to send an ice candidate: Not answerer",
            Errors.SendIceCandidateError.NotParticipant => "Failed to send an ice candidate: Not participant",
            _ => "Failed to send an ice candidate: Unknown error"
        };

    public static string ToLocalizedString(this Errors.EndConnectionAttemptError error) =>
        error switch
        {
            Errors.EndConnectionAttemptError.PlayerConnectionNotFound => "Failed to end a connection attempt: Player connection not found",
            Errors.EndConnectionAttemptError.OfferNotFound => "Failed to end a connection attempt: Offer not found",
            Errors.EndConnectionAttemptError.NotParticipant => "Failed to end a connection attempt: Not participant",
            Errors.EndConnectionAttemptError.FailedToRemoveOffer => "Failed to end a connection attempt: Failed to remove offer",
            _ => "Failed to end a connection attempt: Unknown error"
        };


    public static string ToLocalizedString(this Errors.CreateRoomError error) =>
        error switch
        {
            Errors.CreateRoomError.PlayerConnectionNotFound => "Failed to create a room: Player connection not found",
            Errors.CreateRoomError.PlayerAlreadyInARoom => "Failed to create a room: Player already in a room",
            Errors.CreateRoomError.FailedToRegisterRoom => "Failed to create a room: Failed to register room",
            Errors.CreateRoomError.FailedToUpdatePlayerConnection => "Failed to create a room: Failed to update player connection",
            _ => "Failed to create a room: Unknown error"
        };

    public static string ToLocalizedString(this Errors.JoinRoomError error) =>
        error switch
        {
            Errors.JoinRoomError.PlayerConnectionNotFound => "Failed to join a room: Player connection not found",
            Errors.JoinRoomError.PlayerAlreadyInARoom => "Failed to join a room: Player already in a room",
            Errors.JoinRoomError.RoomNotFound => "Failed to join a room: Room not found",
            Errors.JoinRoomError.FailedToUpdateRoom => "Failed to join a room: Failed to update room",
            Errors.JoinRoomError.FailedToUpdatePlayerConnection => "Failed to join a room: Failed to update player connection",
            _ => "Failed to join a room: Unknown error"
        };

    public static string ToLocalizedString(this Errors.ConnectToRoomPlayersError error) =>
        error switch
        {
            Errors.ConnectToRoomPlayersError.PlayerConnectionNotFound => "Failed to connect to room players: Player connection not found",
            Errors.ConnectToRoomPlayersError.NotInARoom => "Failed to connect to room players: Room not found",
            Errors.ConnectToRoomPlayersError.PlayerNotInRoomPlayers => "Failed to connect to room players: Player not in room players",
            Errors.ConnectToRoomPlayersError.FailedToUpdateRoom => "Failed to connect to room players: Failed to update room",
            _ => "Failed to connect to room players: Unknown error"
        };

    public static string ToLocalizedString(this Errors.LeaveRoomError error) =>
        error switch
        {
            Errors.LeaveRoomError.PlayerConnectionNotFound => "Failed to leave a room: Player connection not found",
            Errors.LeaveRoomError.NotInARoom => "Failed to leave a room: Room not found",
            Errors.LeaveRoomError.FailedToUpdateRoom => "Failed to leave a room: Failed to update room",
            Errors.LeaveRoomError.FailedToUpdatePlayerConnection => "Failed to leave a room: Failed to update player connection",
            _ => "Failed to leave a room: Unknown error"
        };
}
