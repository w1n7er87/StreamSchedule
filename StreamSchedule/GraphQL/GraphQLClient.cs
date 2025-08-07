using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using StreamSchedule.Browsing;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.GraphQL;

public static class GraphQLClient
{
    private const string ClientID = "kimne78kx3ncx6brgo4mv6wki5h1ko";
    private const string Endpoint = "https://gql.twitch.tv/gql";
    private static readonly HttpClient httpClient = new(new SocketsHttpHandler() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });
    private static readonly HttpClient testHttpClient = new(new SocketsHttpHandler() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });
    private static readonly GraphQLHttpClient client = new GraphQLHttpClient(Endpoint, new NewtonsoftJsonSerializer(), httpClient);
    private static readonly GraphQLHttpClient testGqlClient = new GraphQLHttpClient(Endpoint, new NewtonsoftJsonSerializer(), testHttpClient);

    static GraphQLClient()
    {
        httpClient.DefaultRequestHeaders.Add("Client-Id", ClientID);
        httpClient.DefaultRequestHeaders.Add("accept", "*/*");
        httpClient.DefaultRequestHeaders.Add("Host", "gql.twitch.tv");
        
        testHttpClient.DefaultRequestHeaders.Add("Client-Id", ClientID);
        testHttpClient.DefaultRequestHeaders.Add("accept", "*/*");
        testHttpClient.DefaultRequestHeaders.Add("Host", "gql.twitch.tv");
    }

    public static void UpdateHeaders(Dictionary<string, string> headers)
    {
        foreach (KeyValuePair<string, string> h in headers)
        {
            httpClient.DefaultRequestHeaders.Remove(h.Key);
            httpClient.DefaultRequestHeaders.Add(h.Key, h.Value);
        }
    }

    public static void SetIntegrity(Integrity integrity)
    {
        httpClient.DefaultRequestHeaders.Remove("Client-Integrity");
        httpClient.DefaultRequestHeaders.Add("Client-Integrity", integrity.Token);
        httpClient.DefaultRequestHeaders.Remove("X-Device-Id");
        httpClient.DefaultRequestHeaders.Add("X-Device-Id", integrity.DeviceID);
    }
    
    public static async Task<bool> VerifyIntegrity(Integrity integrity)
    {
        testHttpClient.DefaultRequestHeaders.Remove("Client-Integrity");
        testHttpClient.DefaultRequestHeaders.Add("Client-Integrity", integrity.Token);
        
        testHttpClient.DefaultRequestHeaders.Remove("X-Device-Id");
        testHttpClient.DefaultRequestHeaders.Add("X-Device-Id", integrity.DeviceID);
        
        GraphQLResponse<QueryResponse?> r = await testGqlClient.SendQueryAsync<QueryResponse?>(Queries.IsUsernameAvailable("twitch"));

        return r.Errors is null || !r.Errors.Select(x => x.Message).Contains("failed integrity check");
    }
    
    public static async Task<(int, Chatter?[]?)> GetChattersCount(string userID, string? userLogin = null)
    {
        GraphQLResponse<QueryResponse?> result = (userLogin is null)
            ? await client.SendQueryAsync<QueryResponse?>(Queries.RequestChattersByID(userID))
            : await client.SendQueryAsync<QueryResponse?>(Queries.RequestChattersByLogin(userLogin));
        HandleErrors(result.Errors);
        return (result.Data?.User?.Channel?.Chatters?.Count ?? 0, result.Data?.User?.Channel?.Chatters?.Viewers ?? []);
    }

    public static async Task<Emote?> GetEmote(string emoteID)
    {
        GraphQLResponse<QueryResponse?> result = await client.SendQueryAsync<QueryResponse?>(Queries.RequestEmote(emoteID));
        HandleErrors(result.Errors);
        return result.Data?.Emote ?? null;
    }

    public static async Task<List<string?>> GetEmoteIDsFromMessage(string messageID)
    {
        GraphQLResponse<QueryResponse?> result = await client.SendQueryAsync<QueryResponse?>(Queries.RequestEmotesInMessage(messageID));
        HandleErrors(result.Errors);
        if (result.Data?.Message?.Content?.Fragments is null) return [];
        return [.. result.Data.Message.Content.Fragments.Where(x => x?.Content is not null).Select(e => e!.Content!.ID)];
    }

    public static async Task<(User?, GetUserErrorReason?, bool?)> GetUserByID(string userID)
    {
        GraphQLResponse<QueryResponse?> result = await client.SendQueryAsync<QueryResponse?>(Queries.RequestUserByID(userID));
        HandleErrors(result.Errors);
        return (result.Data?.User, result.Data?.UserResultByID?.Reason, false);
    }

    public static async Task<(User?, GetUserErrorReason?, bool?)> GetUserOrReasonByLogin(string userLogin)
    {
        GraphQLResponse<QueryResponse?> result = await client.SendQueryAsync<QueryResponse?>(Queries.RequestUserByLogin(userLogin));
        HandleErrors(result.Errors);
        return (result.Data?.User, result.Data?.UserResultByLogin?.Reason, result.Data?.IsUsernameAvailable);
    }

    public static async Task<ChatSettings?> GetChatSettings(string userID)
    {
        GraphQLResponse<QueryResponse?> result = await client.SendQueryAsync<QueryResponse?>(Queries.RequestChatSettings(userID));
        HandleErrors(result.Errors);
        return result?.Data?.User?.ChatSettings ?? null;
    }

    private static void HandleErrors(GraphQLError[]? errors)
    {
        if(errors is null || errors.Length == 0) return;

        if (errors.Select(x => x.Message).Contains("failed integrity check"))
        {
            BotCore.Nlog.Info("bad integrity");
            Browsing.Browsing.ScheduleUpdate();
        }
    }
}
