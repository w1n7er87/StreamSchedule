using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using StreamSchedule.VedalPlush.Data;

namespace StreamSchedule.VedalPlush;

public class VedalPlushClient
{
    private const string Endpoint = "https://junipersales.myshopify.com/api/2025-01/graphql";
    private static readonly HttpClient _httpClient = new(new SocketsHttpHandler() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });
    private readonly GraphQLHttpClient _client;

    public VedalPlushClient()
    {
        _httpClient.DefaultRequestHeaders.Add("x-shopify-storefront-access-token", Credentials.shopifyAccessToken);
        _client = new GraphQLHttpClient(Endpoint, new NewtonsoftJsonSerializer(), _httpClient);
    }
    
    internal async Task<Response?> GetPlushCount()
    {
        try
        {
            GraphQLResponse<Response?> result = await _client.SendQueryAsync<Response?>(GetPlushCountRequest());
            return result.Data;
        }
        catch (Exception e)
        {
            BotCore.Nlog.Error(e);
            return null;
        }
    }
    
    private static GraphQLRequest GetPlushCountRequest() => new GraphQLRequest(GetPlushCountQuery,
        new { id = "gid://shopify/Product/8020222607551" }, operationName: "");
    
    private static readonly GraphQLQuery GetPlushCountQuery = new GraphQLQuery(
        """
            query ($id:ID!)  { node (id: $id) { __typename, ...ProductFragment } }
                fragment MetafieldWithReferenceFragment on Metafield  {key,type,value},
                fragment ProductFragment on Product  { 
                    id,
                    availableForSale,
                    quantity: totalInventory,
                    metafieldsWithReference: metafields (identifiers: 
                    [				
        	            {namespace: "productListing" key: "campaignEnd"}
                    ]) { ...MetafieldWithReferenceFragment }
                }
        """);
}
