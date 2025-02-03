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

    public async Task<int> GetChattersCount(string userID)
    {
        GraphQLResponse<QueryResponse> result = await _client.SendQueryAsync<QueryResponse>(Queries.RequestChattersCount(userID));
        return result.Data.User?.Channel.Chatters.Count ?? 0;
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
}
