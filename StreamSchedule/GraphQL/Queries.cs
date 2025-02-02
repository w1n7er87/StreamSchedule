using GraphQL;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.GraphQL;

internal class Queries
{
    private static readonly GraphQLQuery _chattersCountQuery = new GraphQLQuery("""
        query GetChattersCount($id: ID, $login: String, $type: UserLookupType) {
        	user(id: $id, login: $login, lookupType: $type) {
        		channel {
        			chatters {
        				count
        			}
        		}
        	}
        }
        """);

    internal static GraphQLRequest ChattersCountQuery(string userID)
    {
        return new GraphQLRequest(_chattersCountQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL)}, "GetChattersCount");
    }
}
