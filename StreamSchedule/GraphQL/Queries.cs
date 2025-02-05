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
                        viewers{
                        login
                        }
        			}
        		}
        	}
        }
        """);

    internal static GraphQLRequest RequestChattersCount(string userID)
    {
        return new GraphQLRequest(_chattersCountQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL)}, "GetChattersCount");
    }

    private static readonly GraphQLQuery _emoteQuery = new GraphQLQuery("""
        query GetEmote($id: ID!) {
        	emote(id: $id) {
                owner{
                    login
                }
        	    bitsBadgeTierSummary{
                    threshold
                }
                subscriptionTier
                type
                suffix
                token
        	}
        }
        """);

    internal static GraphQLRequest RequestEmote(string emoteID)
    {
        return new GraphQLRequest(_emoteQuery, new { id = emoteID }, "GetEmote");
    }

    private static readonly GraphQLQuery _emotesInMessageQuery = new GraphQLQuery("""
        query GetEmotesInMessage($id: ID!) {
            message(id: $id) {
                content {
                    fragments {
                        content{
                            ... on Emote{
                                id
                            }
                        }
                    }
                }
            }
        }
        """);

    internal static GraphQLRequest RequestEmotesInMessage(string messageID)
    {
        return new GraphQLRequest(_emotesInMessageQuery, new { id = messageID }, "GetEmotesInMessage");
    }
}
