using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// using Loak.Networking;
// using Sirenix.OdinInspector;
// using Sirenix.Serialization;
using UnityEngine;

namespace Loak.Unity
{
    public class LeaderboardScore
    {
        public string username;
        public int score;
    }

    internal class LeaderboardModel : IComparable<LeaderboardModel>
    {
        public long score;
        public string username;

        public int CompareTo(LeaderboardModel other)
        {
            return score.CompareTo(other.score);
        }
    }

    internal class Leaderboard : MonoBehaviour
    {
        public static Leaderboard Instance = null;

        private Dictionary<string, List<LeaderboardModel>> LeaderboardTable = new Dictionary<string, List<LeaderboardModel>>();

        private Dictionary<string, DateTime> LastRefreshTimestamps = new Dictionary<string, DateTime>();

        public float coolDownTimer = 300f; // In seconds

        void Awake()
        {
            Instance = this;
        }


        public async Task<List<LeaderboardModel>> GetRefreshedLeaderboard(string gameId)
        {
            if (!CheckIfTimestampExists(gameId))
            {
                return new List<LeaderboardModel>();
            }

            TimeSpan timeSinceLastRefresh = DateTime.UtcNow - LastRefreshTimestamps[gameId];
            List<LeaderboardModel> retrievedLeaderboards = new List<LeaderboardModel>();
            if (timeSinceLastRefresh.TotalSeconds >= coolDownTimer)
            {
                // var userId = FirebaseManager.Instance.auth.CurrentUser.UserId;
                // retrievedLeaderboards = await APIClient.Instance.GetFriendLeaderboardsAsync(userId, gameId);
            }

            return retrievedLeaderboards;

        }

        /// <summary>
        /// If the timestamp doesnt exist, then we should do a network call to retrieve leaderboards
        /// If it does exist, RefreshLeaderboard will check if it should do a network call based on how long it's been
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public bool CheckIfTimestampExists(string gameId)
        {
            if (!LastRefreshTimestamps.ContainsKey(gameId))
            {
                LastRefreshTimestamps.Add(gameId, DateTime.UtcNow);
                return false;
            }
            return true;
        }

        public bool IsTimestampPassedCooldown(string gameId)
        {
            TimeSpan timeSinceLastRefresh = DateTime.UtcNow - LastRefreshTimestamps[gameId];
            Debug.Log("Total Seconds: " + timeSinceLastRefresh.TotalSeconds);

            if (timeSinceLastRefresh.TotalSeconds >= coolDownTimer)
            {
                return true;
            }

            return false;
        }

        public void SetTimestamp(string gameId)
        {
            if(!CheckIfTimestampExists(gameId))
            {
                return;
            }

            LastRefreshTimestamps[gameId] = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the player's score for the given leaderboard from the API
        /// </summary>
        /// <param name="gameId"></param>
        // public async Task<Score[]> TryAndGetPlayerScore(string gameId)
        // {
        //     var playerCurrentScore = UserManager.Instance.GetCurrentUser().GetItem(gameId, gameId);
        //     Score[] scoreFromApi;
        //     if (playerCurrentScore == null)
        //     {
        //         scoreFromApi = await APIClient.Instance.GetScoreAsync(FirebaseManager.Instance.auth.CurrentUser.UserId, gameId);

        //         if (scoreFromApi != null)
        //         {
        //             UserManager.Instance.GetCurrentUser().AddItem(new Item(gameId, gameId, scoreFromApi[0].score, null));
        //             return scoreFromApi;
        //         }
        //     }

        //     return null;
        // }


        public void AddLeaderboard(string worldID, List<LeaderboardModel> leaderBoard)
        {
            if (!LeaderboardTable.ContainsKey(worldID))
            {
                LeaderboardTable.Add(worldID, leaderBoard);
            }
            else
            {
                LeaderboardTable[worldID] = leaderBoard;
            }
        }

        public void AddLeaderboard(string gameId, LeaderboardScore[] scores)
        {
            var leaderBoard = new List<LeaderboardModel>();
            LeaderboardModel model;

            foreach (LeaderboardScore score in scores)
            {
                model = new LeaderboardModel();
                model.username = score.username;
                model.score = score.score;
                leaderBoard.Add(model);
            }

            if (!LeaderboardTable.ContainsKey(gameId))
            {
                LeaderboardTable.Add(gameId, leaderBoard);
            }
            else
            {
                LeaderboardTable[gameId] = leaderBoard;
            }
        }

        public bool LeaderboardLoaded(string worldID) => LeaderboardTable.ContainsKey(worldID);

        public List<LeaderboardModel> GetLeaderBoard(string worldID)
        {
            List<LeaderboardModel> leaderboard;

            if (!LeaderboardTable.TryGetValue(worldID, out leaderboard))
            {
                return new List<LeaderboardModel>();
            }

            if(leaderboard == null)
                return new List<LeaderboardModel>();

            return leaderboard;
        }

        // public int GetLeaderboardRank(string worldID, User user)
        // {
        //     List<LeaderboardModel> board;

        //     if (user == null || !LeaderboardTable.TryGetValue(worldID, out board))
        //         return -1;

        //     for (int i = 0; i < board.Count; i++)
        //     {
        //         if (board[i].username == user.GetUsername())
        //             return i + 1;
        //     }

        //     return -1;
        // }

        public void UpdateLocalLeaderboard(string worldID, long value)
        {
            //For daily leaderboards
            //string dailyKey = worldID + "Daily";

            //Get Current User for creating leaderboard data locally
            // var user = UserManager.Instance.GetCurrentUser();

            //We create the leaderboard data we wish to fit in Daily and AllTime locally
            LeaderboardModel leaderboardData = new LeaderboardModel()
            {
                score = value,
                username = "Test User 1"
            };

            //Try to add the data to the daiy
            AddToLeaderboardTable(worldID, leaderboardData);

            //Try to add the data to the all time
            //AddToLeaderboardTable(dailyKey, leaderboardData);
        }

        /// <summary>
        /// This will attempt to add the daily leaderboard to the key passed in as world ID.
        /// </summary>
        /// <param name="worldID"></param>
        /// <param name="leaderboardData"></param>
        private void AddToLeaderboardTable(string worldID, LeaderboardModel leaderboardData)
        {
            List<LeaderboardModel> board;
            //First we do All Time leaderboards
            if (LeaderboardTable.TryGetValue(worldID, out board) && board != null)
            {
                //This checks if the key exists
                if (board.Count > 0)
                {
                    //We have leaderboard data
                    //Check if this data already exists.
                    var found = board.Find(x => x.username == leaderboardData.username);
                    if (found != null)
                    {
                        if (found.score < leaderboardData.score)
                        {
                            board.Remove(found);
                            FitItemAndSort(board, leaderboardData);
                        }
                    }
                    else
                    {
                        FitItemAndSort(board, leaderboardData);
                    }
                }
                else
                {
                    //No leaderboard
                    //Just add the model to the list.
                    board.Add(leaderboardData);
                }
            }
            else
            {
                //No Table exists
                LeaderboardTable.Add(worldID, new List<LeaderboardModel>() { leaderboardData });
            }
        }

        /// <summary>
        /// This will try to add the leaderboard data to the proper location in the local cache
        /// If the user doesn't fit anywhere, this will not do anything
        /// </summary>
        /// <param name="models"></param>
        /// <param name="leaderboardData"></param>
        private void FitItemAndSort(List<LeaderboardModel> models, LeaderboardModel leaderboardData)
        {
            // insert the number at the appropriate index to maintain the sorted order
            int index = models.BinarySearch(leaderboardData);
            if (index < 0)
            {
                index = ~index;
            }
            models.Insert(index, leaderboardData);

            // remove the last element if the list size exceeds 10
            if (models.Count > 10)
            {
                models.RemoveAt(models.Count - 1);
            }

            // print the resulting list in descending order
            models.Sort((a, b) => b.CompareTo(a));
        }
    }

}
