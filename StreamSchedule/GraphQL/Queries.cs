using GraphQL;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.GraphQL;

internal class Queries
{
    internal static GraphQLRequest RequestChattersByID(string userID) => new GraphQLRequest(_chattersByIDQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetChatters");

    private static readonly GraphQLQuery _chattersByIDQuery = new GraphQLQuery("""
        query GetChatters($id: ID, $type: UserLookupType) {
        	user(id: $id, lookupType: $type) {
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

    internal static GraphQLRequest RequestChattersByLogin(string userLogin) => new GraphQLRequest(_chattersByLoginQuery, new { login = userLogin, type = Enum.GetName(UserLookupType.ALL) }, "GetChatters");

    private static readonly GraphQLQuery _chattersByLoginQuery = new GraphQLQuery("""
        query GetChatters($login: String, $type: UserLookupType) {
        	user(login: $login, lookupType: $type) {
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

    internal static GraphQLRequest RequestChatSettings(string userID) => new GraphQLRequest(_userRulesQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetRules");

    private static readonly GraphQLQuery _userRulesQuery = new GraphQLQuery("""
        query GetRules($id: ID, $type: UserLookupType) {
            user(id:$id, lookupType: $type) {
                chatSettings{
                    rules
                    blockLinks
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
                artist{
                    login
                }
        	    bitsBadgeTierSummary {
                    threshold
                }
                subscriptionTier
                type
                assetType
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

    internal static GraphQLRequest RequestUserByID(string userID) => new GraphQLRequest(_userByIDQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetUser");

    private static readonly GraphQLQuery _userByIDQuery = new GraphQLQuery("""
        query GetUser($id:ID!, $type: UserLookupType) {
            userResultByID(id: $id) {
        	    ... on UserDoesNotExist {
        		    reason
        	    }
            }
            user(id:$id, lookupType: $type){
                login
                id
                chatColor
                primaryColorHex
                followers {
                    totalCount
                }
                roles {
                    isAffiliate
                    isPartner
                    isSiteAdmin
                    isGlobalMod
                    isStaff
                }
                channel {
                    chatters {
                        count
                    }
                    founderBadgeAvailability
                    hypeTrain {
                        execution {
                            isActive
                            config {
                                difficulty
                            }
                            progress {
                                level {
                                    value
                                }
                                goal
                                progression
                            }
                        }
                    }
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
            userResultByLogin(login: $login) {
        	    ... on UserDoesNotExist {
        		    reason
        	    }
            }
            user(login:$login, lookupType: $type){
                login
                id
                chatColor
                primaryColorHex
                followers {
                    totalCount
                }
                roles {
                    isAffiliate
                    isPartner
                    isSiteAdmin
                    isGlobalMod
                    isStaff
                }
                channel {
                    chatters {
                        count
                    }
                    founderBadgeAvailability
                    hypeTrain {
                        execution {
                            isActive
                            config {
                                difficulty
                            }
                            progress {
                                level {
                                    value
                                }
                                goal
                                progression
                            }
                        }
                    }
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
}
