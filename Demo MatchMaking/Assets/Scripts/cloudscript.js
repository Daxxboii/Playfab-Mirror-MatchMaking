handlers.storePlayFabIdWithMatchId = function(args, context) {
    var matchId = args.matchId;
    var playFabId = currentPlayerId;

    var data = server.GetTitleData({
        Keys: [matchId]
    });

    // If data exists for the matchId, update the array otherwise, create a new array.
    var playFabIds = data.Data && data.Data[matchId] ? JSON.parse(data.Data[matchId]) : [];
    playFabIds.push(playFabId);

    server.UpdateTitleData({
        Key: matchId,
        Value: JSON.stringify(playFabIds)
    });
};

handlers.getPlayFabIdWithMatchId = function(args, context) {
    var matchId = args.matchId;

    var data = server.GetTitleData({
        Keys: [matchId]
    });

    if (data.Data && data.Data[matchId]) {
        return JSON.parse(data.Data[matchId]);
    } else {
        throw "No data found for matchId: " + matchId;
    }
};
