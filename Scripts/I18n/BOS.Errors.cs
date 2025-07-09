namespace Behide.I18n.BOS.Errors;

using OnlineServices.Signaling;

public static class ErrorsExtensions {
    public static string ToLocalizedString(this Errors.StartConnectionAttemptError error) =>
        error switch
        {
            Errors.StartConnectionAttemptError.PlayerNotFound => "Failed to start a connection attempt: Player not registered on signaling server",
            Errors.StartConnectionAttemptError.FailedToCreateConnectionAttempt => "Failed to start a connection attempt: Failed to create a connection attempt",
            Errors.StartConnectionAttemptError.FailedToUpdatePlayer => "Failed to start a connection attempt: Failed to update player info",
            _ => "Failed to start a connection attempt: Unknown error"
        };

    public static string ToLocalizedString(this Errors.JoinConnectionAttemptError error) =>
        error switch
        {
            Errors.JoinConnectionAttemptError.PlayerNotFound => "Failed to join a connection attempt: Player not registered on signaling server",
            Errors.JoinConnectionAttemptError.ConnectionAttemptNotFound => "Failed to join a connection attempt: Connection attempt not found",
            Errors.JoinConnectionAttemptError.ConnectionAttemptAlreadyAnswered => "Failed to join a connection attempt: Connection attempt already answered",
            Errors.JoinConnectionAttemptError.InitiatorCannotJoin => "Failed to join a connection attempt: Initiator cannot join",
            Errors.JoinConnectionAttemptError.FailedToUpdateConnectionAttempt => "Failed to join a connection attempt: Failed to update connection attempt",
            _ => "Failed to join a connection attempt: Unknown error"
        };

    public static string ToLocalizedString(this Errors.SendAnswerError error) =>
        error switch
        {
            Errors.SendAnswerError.PlayerNotFound => "Failed to send an answer: Player not registered on signaling server",
            Errors.SendAnswerError.ConnectionAttemptNotFound => "Failed to send an answer: Connection attempt not found",
            Errors.SendAnswerError.NotAnswerer => "Failed to send an answer: Not answerer",
            _ => "Failed to send an answer: Unknown error"
        };

    public static string ToLocalizedString(this Errors.SendIceCandidateError error) =>
        error switch
        {
            Errors.SendIceCandidateError.PlayerNotFound => "Failed to send an ice candidate: Player not registered on signaling server",
            Errors.SendIceCandidateError.ConnectionAttemptNotFound => "Failed to send an ice candidate: Connection attempt not found",
            Errors.SendIceCandidateError.NoAnswerer => "Failed to send an ice candidate: No answerer",
            Errors.SendIceCandidateError.NotParticipant => "Failed to send an ice candidate: Not participant",
            _ => "Failed to send an ice candidate: Unknown error"
        };

    public static string ToLocalizedString(this Errors.EndConnectionAttemptError error) =>
        error switch
        {
            Errors.EndConnectionAttemptError.PlayerNotFound => "Failed to end a connection attempt: Player not registered on signaling server",
            Errors.EndConnectionAttemptError.ConnectionAttemptNotFound => "Failed to end a connection attempt: Connection attempt not found",
            Errors.EndConnectionAttemptError.NotParticipant => "Failed to end a connection attempt: Not participant",
            Errors.EndConnectionAttemptError.FailedToRemoveConnectionAttempt => "Failed to end a connection attempt: Failed to remove connection attempt",
            _ => "Failed to end a connection attempt: Unknown error"
        };


    public static string ToLocalizedString(this Errors.CreateRoomError error) =>
        error switch
        {
            Errors.CreateRoomError.PlayerNotFound => "Failed to create a room: Player not registered on signaling server",
            Errors.CreateRoomError.PlayerAlreadyInARoom => "Failed to create a room: Player already in a room",
            Errors.CreateRoomError.FailedToRegisterRoom => "Failed to create a room: Failed to register room",
            Errors.CreateRoomError.FailedToUpdatePlayer => "Failed to create a room: Failed to update player info",
            _ => "Failed to create a room: Unknown error"
        };

    public static string ToLocalizedString(this Errors.JoinRoomError error) =>
        error switch
        {
            Errors.JoinRoomError.PlayerNotFound => "Failed to join a room: Player not registered on signaling server",
            Errors.JoinRoomError.PlayerAlreadyInARoom => "Failed to join a room: Player already in a room",
            Errors.JoinRoomError.RoomNotFound => "Failed to join a room: Room not found",
            Errors.JoinRoomError.FailedToUpdatePlayer => "Failed to join a room: Failed to update player info",
            _ => "Failed to join a room: Unknown error"
        };

    public static string ToLocalizedString(this Errors.ConnectToRoomPlayersError error) =>
        error switch
        {
            Errors.ConnectToRoomPlayersError.PlayerNotFound => "Failed to connect to room players: Player not registered on signaling server",
            Errors.ConnectToRoomPlayersError.NotInARoom => "Failed to connect to room players: Room not found",
            Errors.ConnectToRoomPlayersError.PlayerNotInRoomPlayers => "Failed to connect to room players: Player not in room players",
            _ => "Failed to connect to room players: Unknown error"
        };

    public static string ToLocalizedString(this Errors.LeaveRoomError error) =>
        error switch
        {
            Errors.LeaveRoomError.PlayerNotFound => "Failed to leave a room: Player not registered on signaling server",
            Errors.LeaveRoomError.NotInARoom => "Failed to leave a room: Room not found",
            Errors.LeaveRoomError.FailedToUpdateRoom => "Failed to leave a room: Failed to update room",
            Errors.LeaveRoomError.FailedToUpdatePlayer => "Failed to leave a room: Failed to update player info",
            _ => "Failed to leave a room: Unknown error"
        };
}
