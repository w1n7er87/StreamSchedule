using GraphQL;
using StreamSchedule.GraphQL.Data;

namespace StreamSchedule.GraphQL;

internal static class Queries
{
    internal static GraphQLRequest IsUsernameAvailable(string username) => new(_IsUsernameAvailableQuery, new { login = username });

    private static readonly GraphQLQuery _IsUsernameAvailableQuery = new GraphQLQuery(
        """
        query UsernameValidator_User($login: String!) {
        	isUsernameAvailable(username:$login)
        }
        """);

    internal static GraphQLRequest RequestChattersByID(string userID) => new(_chattersByIDQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetChatters");

    private static readonly GraphQLQuery _chattersByIDQuery = new GraphQLQuery(
        """
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

    internal static GraphQLRequest RequestChattersByLogin(string userLogin) => new(_chattersByLoginQuery, new { login = userLogin, type = Enum.GetName(UserLookupType.ALL) }, "GetChatters");

    private static readonly GraphQLQuery _chattersByLoginQuery = new GraphQLQuery(
        """
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

    internal static GraphQLRequest RequestChatSettings(string userID) => new(_userRulesQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetRules");

    private static readonly GraphQLQuery _userRulesQuery = new GraphQLQuery(
        """
        query GetRules($id: ID, $type: UserLookupType) {
            user(id:$id, lookupType: $type) {
                chatSettings{
                    rules
                    blockLinks
                }
            }
        }
        """);

    internal static GraphQLRequest RequestEmote(string emoteID) => new(_emoteQuery, new { id = emoteID }, "GetEmote");

    private static readonly GraphQLQuery _emoteQuery = new GraphQLQuery(
        """
        query GetEmote($id: ID!) {
        	emote(id: $id) {
        	    id
                owner {
                    login
                }
                artist{
                    login
                }
        	    bitsBadgeTierSummary {
                    threshold
                }
                subscriptionSummaries {
                    tier
                }
                type
                assetType
                suffix
                token
                createdAt
        	}
        }
        """);

    internal static GraphQLRequest RequestEmotesInMessage(string messageID) => new(_emotesInMessageQuery, new { id = messageID }, "GetEmotesInMessage");

    private static readonly GraphQLQuery _emotesInMessageQuery = new GraphQLQuery(
        """
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

    internal static GraphQLRequest RequestUserByID(string userID) => new(_userByIDQuery, new { id = userID, type = Enum.GetName(UserLookupType.ALL) }, "GetUser");

    private static readonly GraphQLQuery _userByIDQuery = new GraphQLQuery(
        """
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
                        approaching {
                            expiresAt
                            goal
                            isGoldenKappaTrain
                            isTreasureTrain
                            eventsRemaining {
                                events
                            }
                        }
                        execution {
                            startedAt
                            expiresAt
                            updatedAt
                            isGoldenKappaTrain
                            isTreasureTrain
                            isFastMode
                            treasureTrainDetails {
                                discountPercentage
                                discountLevelThreshold
                            }
                            allTimeHigh {
                                level {
                                    value
                                }
                                goal
                                progression
                            }
                            participations {
                                action
                                source
                                quantity
                            }
                            config {
                                difficulty
                            }
                            progress {
                                level {
                                    value
                                }
                                goal
                                progression
                                total
                            }
                            sharedHypeTrainDetails {
                                sharedProgress {
                                    user {
                                        login
                                    }
                                    channelProgress {
                                        total
                                    }
                                }
                                sharedAllTimeHighRecords {
                                    channelAllTimeHigh {
                                        level {
                                            value
                                        }
                                        goal
                                        progression
                                    }
                                }
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
                updatedAt
            }
        }
        """);

    internal static GraphQLRequest RequestUserByLogin(string userLogin) => new(_userByLoginQuery, new { login = userLogin, type = Enum.GetName(UserLookupType.ALL) }, "GetUser");

    private static readonly GraphQLQuery _userByLoginQuery = new GraphQLQuery(
        """
        query GetUser($login:String!, $type: UserLookupType) {
            isUsernameAvailable(username: $login)
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
                        approaching {
                            expiresAt
                            goal
                            isGoldenKappaTrain
                            isTreasureTrain
                            eventsRemaining {
                                events
                            }
                        }
                        execution {
                            startedAt
                            expiresAt
                            updatedAt
                            isGoldenKappaTrain
                            isTreasureTrain
                            isFastMode
                            treasureTrainDetails {
                                discountPercentage
                                discountLevelThreshold
                            }
                            allTimeHigh {
                                level {
                                    value
                                }
                                goal
                                progression
                            }
                            participations {
                                action
                                source
                                quantity
                            }
                            config {
                                difficulty
                            }
                            progress {
                                level {
                                    value
                                }
                                goal
                                progression
                                total
                            }
                            sharedHypeTrainDetails {
                                sharedProgress {
                                    user {
                                        login
                                    }
                                    channelProgress {
                                    total
                                    }
                                }
                                sharedAllTimeHighRecords {
                                    channelAllTimeHigh {
                                        level {
                                            value
                                        }
                                        goal
                                        progression
                                    }
                                }
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
                updatedAt
            }
        }
        """);

    internal static GraphQLRequest RequestHypeTrainExecution(string userLogin) => new(_getHypeTrainExecution, new { login = userLogin, type = Enum.GetName(UserLookupType.ALL) }, "GetUser");
    
    private static readonly GraphQLQuery _getHypeTrainExecution = new GraphQLQuery(
        """
       query GetUser($login:String!, $type: UserLookupType) {
           user(login:$login, lookupType: $type){
               channel {
                   hypeTrain {
                       approaching {
                           expiresAt
                           goal
                           isGoldenKappaTrain
                           isTreasureTrain
                           eventsRemaining {
                               events
                           }
                       }
                       execution {
                           startedAt
                           expiresAt
                           updatedAt
                           isGoldenKappaTrain
                           isTreasureTrain
                           isFastMode
                           treasureTrainDetails {
                               discountPercentage
                               discountLevelThreshold
                           }
                           allTimeHigh {
                               level {
                                   value
                               }
                               goal
                               progression
                           }
                           participations {
                               action
                               source
                               quantity
                           }
                           config {
                               difficulty
                           }
                           progress {
                               level {
                                   value
                               }
                               goal
                               progression
                               total
                           }
                           sharedHypeTrainDetails {
                               sharedProgress {
                                   user {
                                       login
                                   }
                                   channelProgress {
                                   total
                                   }
                               }
                               sharedAllTimeHighRecords {
                                   channelAllTimeHigh {
                                       level {
                                           value
                                       }
                                       goal
                                       progression
                                   }
                               }
                           }
                       }
                    }
                }
            }
       }
       """);
}
