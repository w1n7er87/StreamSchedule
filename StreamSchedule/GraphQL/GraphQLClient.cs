using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.GraphQL;

public class GraphQLClient
{
    private const string ClientID = "kimne78kx3ncx6brgo4mv6wki5h1ko";
    private const string Endpoint = "https://gql.twitch.tv/gql";
    private static readonly HttpClient _httpClient = new(new SocketsHttpHandler() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });
    private readonly GraphQLHttpClient _client;

    public GraphQLClient()
    {
        _httpClient.DefaultRequestHeaders.Add("Client-ID", ClientID);
        _client = new GraphQLHttpClient(Endpoint, new NewtonsoftJsonSerializer(), _httpClient);
    }

    public async Task<(int, Chatter?[]?)> GetChattersCount(string userID, string? userLogin = null)
    {
        GraphQLResponse<QueryResponse?> result = (userLogin is null) ?
            await _client.SendQueryAsync<QueryResponse?>(Queries.RequestChattersByID(userID)) :
            await _client.SendQueryAsync<QueryResponse?>(Queries.RequestChattersByLogin(userLogin));
        return (result.Data?.User?.Channel?.Chatters?.Count ?? 0, result.Data?.User?.Channel?.Chatters?.Viewers ?? []);
    }

    public async Task<Emote?> GetEmote(string emoteID)
    {
        GraphQLResponse<QueryResponse?> result = await _client.SendQueryAsync<QueryResponse?>(Queries.RequestEmote(emoteID));
        return result.Data?.Emote ?? null;
    }

    public async Task<List<string>> GetEmoteIDsFromMessage(string messageID)
    {
        GraphQLResponse<QueryResponse?> result = await _client.SendQueryAsync<QueryResponse?>(Queries.RequestEmotesInMessage(messageID));
        if (result.Data?.Message?.Content?.Fragments is null) return [];
        return [.. result.Data.Message.Content.Fragments.Where(x => x?.Content is not null).Select(e => e!.Content!.ID)];
    }

    public async Task<User?> GetUserByID(string userID)
    {
        GraphQLResponse<QueryResponse?> result = await _client.SendQueryAsync<QueryResponse?>(Queries.RequestUserByID(userID));
        return result.Data?.User;
    }

    public async Task<(User?, bool)> GetUserByLoginAndUsernameAvailability(string userLogin)
    {
        GraphQLResponse<QueryResponse?> result = await _client.SendQueryAsync<QueryResponse?>(Queries.RequestUserByLogin(userLogin));
        return (result.Data?.User, result.Data?.IsUsernameAvailable ?? false);
    }

    public async Task<ChatSettings?> GetChatSettings(string userID)
    {
        GraphQLResponse<QueryResponse?> result = await _client.SendQueryAsync<QueryResponse?>(Queries.RequestChatSettings(userID));
        return result?.Data?.User?.ChatSettings ?? null;
    }
}
