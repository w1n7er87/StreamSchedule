using GraphQL;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.GraphQL;

internal class Queries
{
    internal static GraphQLRequest RequestChattersCount(string userID) => new GraphQLRequest(_chattersCountQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL)}, "GetChattersCount");
    
    private static readonly GraphQLQuery _chattersCountQuery = new GraphQLQuery("""
        query GetChattersCount($id: ID, $login: String, $type: UserLookupType) {
        	user(id: $id, login: $login, lookupType: $type) {
        		channel {
        			chatters {
        				count
                        viewers {
                            login
                        }
        			}
        		}
        	}
        }
        """);

    
    internal static GraphQLRequest RequestEmote(string emoteID) => new GraphQLRequest(_emoteQuery, new { id = emoteID }, "GetEmote");
    
    private static readonly GraphQLQuery _emoteQuery = new GraphQLQuery("""
        query GetEmote($id: ID!) {
        	emote(id: $id) {
                owner {
                    login
                }
        	    bitsBadgeTierSummary {
                    threshold
                }
                subscriptionTier
                type
                suffix
                token
        	}
        }
        """);

    
    internal static GraphQLRequest RequestEmotesInMessage(string messageID) => new GraphQLRequest(_emotesInMessageQuery, new { id = messageID }, "GetEmotesInMessage");

    private static readonly GraphQLQuery _emotesInMessageQuery = new GraphQLQuery("""
        query GetEmotesInMessage($id: ID!) {
            message(id: $id) {
                content {
                    fragments {
                        content {
                            ... on Emote{
                                id
                            }
                        }
                    }
                }
            }
        }
        """);

    
    internal static GraphQLRequest RequestStream(string userID) => new GraphQLRequest(_streamQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetStream");

    private static readonly GraphQLQuery _streamQuery = new GraphQLQuery("""
        query GetStream($id: ID!, $type: UserLookupType) {
            user(id: $id, lookupType: $type) {
                stream {
                    averageFPS
                    bitrate
                    viewersCount
                    clipCount
                    createdAt
                }
            }
        }
        """);

    
    internal static GraphQLRequest RequestPastBroadcast(string userID) => new GraphQLRequest(_pastBroadcastQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetPastBroadcast");

    private static readonly GraphQLQuery _pastBroadcastQuery = new GraphQLQuery("""
        query GetPastBroadcast($id: ID!, $type: UserLookupType) {
            user(id: $id, lookupType: $type) {
                lastBroadcast {
                    game {
                        displayName
                    }
                    title
                    startedAt
                }
            }
        }
        """);


    internal static GraphQLRequest RequestUserByID(string userID) => new GraphQLRequest(_userByIDQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetUser");

    private static readonly GraphQLQuery _userByIDQuery = new GraphQLQuery("""
        query GetUser($id:ID!, $type: UserLookupType) {
            user(id:$id, lookupType: $type){
                login
                id
                chatColor
                primaryColorHex
                followers{
                    totalCount
                }
                roles{
                    isAffiliate
                    isPartner
                    isSiteAdmin
                    isGlobalMod
                    isStaff
                }
                channel {
                    founderBadgeAvailability
                }
                lastBroadcast {
                    id
                    startedAt
                    title
                    game {
                        displayName
                    }
                }
                broadcastSettings {
                    title
                    isMature
                }
                stream {
                    game {
                        displayName
                    }
                    averageFPS
                    bitrate
                    viewersCount
                    clipCount
                    createdAt
                }
                createdAt
                deletedAt
            }
        }
        """);


    internal static GraphQLRequest RequestUserByLogin(string userLogin) => new GraphQLRequest(_userByLoginQuery, new { login = userLogin, type = Enum.GetName(UserLookupType.ALL) }, "GetUser");
    
    private static readonly GraphQLQuery _userByLoginQuery = new GraphQLQuery("""
        query GetUser($login:String!, $type: UserLookupType) {
            user(login:$login, lookupType: $type){
                login
                id
                chatColor
                primaryColorHex
                followers{
                    totalCount
                }
                roles{
                    isAffiliate
                    isPartner
                    isSiteAdmin
                    isGlobalMod
                    isStaff
                }
                channel {
                    founderBadgeAvailability
                }
                lastBroadcast {
                    id
                    startedAt
                    title
                    game {
                        displayName
                    }
                }
                broadcastSettings {
                    title
                    isMature
                }
                stream {
                    game {
                        displayName
                    }
                    averageFPS
                    bitrate
                    viewersCount
                    clipCount
                    createdAt
                }
                createdAt
                deletedAt
            }
            isUsernameAvailable(username:$login)
        }
        """);


}
