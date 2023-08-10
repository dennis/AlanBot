using Aydsko.iRacingData;
using Aydsko.iRacingData.Results;

using Discord.Interactions;

namespace AlanBot;

public class RaceSummaryModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IDataClient _dataClient;

    public RaceSummaryModule(IDataClient dataClient)
    {
        _dataClient = dataClient;
    }

    [SlashCommand("race-summary", "Shows race summary")]
    public async Task RaceSummary(int subSessionId)
    {
        await RespondAsync($"Releasing the hamsters");

        var output = new List<string>();

        var result = (await _dataClient.GetSubSessionResultAsync(subSessionId, false).ConfigureAwait(false)).Data;

        output.Add($"Race results for {result.Track.TrackName}\n");

        var races = result.SessionResults
            .Where(a => a.SimSessionType == 6)
            .OrderBy(a => a.SimSessionNumber);

        foreach (var race in races)
        {
            if (race.SimSessionNumber == 0)
            {
                output.Add("Feature race");
            }
            else
            {
                output.Add("Heat race");
            }

            var driverResult = race.Results
                .Where(a => a.CustomerId is not null)
                .OrderBy(a => a.FinishPosition)
                .ToList();

            if (driverResult.Count > 0)
                output.Add(FormatLine(1, driverResult[0]));
            if (driverResult.Count > 1)
                output.Add(FormatLine(2, driverResult[1]));
            if (driverResult.Count > 2)
                output.Add(FormatLine(3, driverResult[2]));

            output.Add("");
        }

        if (result.LeagueId is not null && result.LeagueSeasonId is not null)
        {
            var standings = (await _dataClient.GetSeasonStandingsAsync(result.LeagueId.Value, result.LeagueSeasonId.Value).ConfigureAwait(false)).Data.Standings.DriverStandings.OrderBy(a => a.Position);
            

            if (standings.Any())
            {
                output.Add($"Standings for \"{result.LeagueName}\"");

                foreach (var driver in standings)
                {
                    output.Add($"{driver.Position} {driver.DriverNickname ?? driver.Driver.DisplayName} {driver.TotalPoints}");
                }
            }
        }


        // output

        var block = "";

        foreach (var line in output)
        {
            if (block.Length + line.Length >= 2000)
            {
                await ReplyAsync(block);
                block = "";
            }

            block += line + "\n";
        }

        if (block.Length > 0)
        {
            await ReplyAsync(block);
        }
    }

    private static string FormatLine(int position, Result result)
    {
        return $"{position} {result.DisplayName}";
    }
}