using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Threading.Tasks;
using System.Net.Http;
using Niantic.ARDK.Extensions.Meshing;
using UnityEngine.Events;

namespace Loak.Unity
{
    public class LoakARUIController : MonoBehaviour
    {
        [Header("Leaderboard Panel")]
        [SerializeField] private GameObject canvas;
        [SerializeField] public GameObject pauseCanvas;

        [SerializeField] private GameObject leaderBoardLoadingBody;
        [SerializeField] private GameObject leaderboardBody;

        [SerializeField] private GameObject leaderboardContents;
        [SerializeField] private Button retryButton;

        [SerializeField] private GameObject friendClickIndicator;
        [SerializeField] private GameObject globalClickIndicator;
        [SerializeField] private TMP_Text completeScoreText;
        [SerializeField] private TMP_Text completeBestHighscoreText;

        [SerializeField] private Image newBestHighscoreImage;
        [SerializeField] private LeaderboardUIBehaviour leaderboardWidgetPrefab;

        [Header("Scanning Phase")]
        [SerializeField] private ARMeshManager meshMan;
        [SerializeField] private GameObject scanningPhasePanel;
        [SerializeField] private GameObject gamePhasePanel;
        [SerializeField] private Image scanFillBar;
        public bool gamePaused = false;
        private bool resetScene = true;
        bool retrievedFriendLeaderboard = false;
        bool retrievedGlobalLeaderboard = false;

        private List<LeaderboardUIBehaviour> leaderboardWidgets = new List<LeaderboardUIBehaviour>();
        // Start is called before the first frame update
        bool inFriendView = false;
        private long score; //Score of the current game
        private long previousScore;
        private string gameId; //gameId of the current game

        #region UI Specific Logic

        void Start()
        {
            // if(resetScene)
            //     ReplaceRetry(RetryGame);
            if (meshMan == null)
                meshMan = FindObjectOfType<ARMeshManager>();
            
            StartScanPhase();
        }

        void Update()
        {
            if (meshMan && scanningPhasePanel.activeSelf)
                UpdateScan(meshMan.MeshRoot.transform.childCount, 20f);
        }

        public void OpenLeaderboardWindow(long score, string gameId)
        {
            this.score = score;
            this.gameId = gameId;


            //Set Previous score before setting the UI Elements
            // var previousScoreItem = UserManager.Instance.GetCurrentUser().GetItem(gameId, gameId);
            // previousScore = previousScoreItem == null ? 0 : previousScoreItem.quantity;
            previousScore = 0;

            leaderboardWidgets = leaderboardContents.GetComponentsInChildren<LeaderboardUIBehaviour>(true).ToList();
            ToggleFriendView(true);
        }
        public void ToggleFriendView(bool val)
        {
            inFriendView = val;
            friendClickIndicator.SetActive(val);
            globalClickIndicator.SetActive(!val);

            Refresh();
        }

        public async void Refresh()
        {
            // RetrieveLeaderboardData();
            ClearUIElements();
            SetUIElements();
            retryButton.interactable = true;
        }

        private void ClearUIElements()
        {
            foreach (var widget in leaderboardWidgets)
            {
                widget.gameObject.SetActive(false);
            }
        }

        private void SetUIElements()
        {
            canvas.SetActive(true);

            if (score > previousScore)
            {
                Debug.Log("Score: " + score + " Previous Score: " + previousScore);
                Leaderboard.Instance.UpdateLocalLeaderboard("Test Experience", score);
                Leaderboard.Instance.UpdateLocalLeaderboard("Test Experience" + " friends", score);
                completeBestHighscoreText.text = score.ToString();
                newBestHighscoreImage.enabled = true;
            }
            else
            {
                completeBestHighscoreText.text = previousScore.ToString();
                newBestHighscoreImage.enabled = false;
            }

            completeScoreText.text = score.ToString();

            if (inFriendView)
            {
                if (!retrievedFriendLeaderboard)
                {
                    ToggleLoadingScreen(true);
                    return;
                }

                ToggleLoadingScreen(false);

                List<LeaderboardModel> friendLeaderboardData = new List<LeaderboardModel>();
                friendLeaderboardData = Leaderboard.Instance.GetLeaderBoard(gameId + " friends");

                for (int i = 0; i < friendLeaderboardData.Count; i++)
                {
                    if (i >= friendLeaderboardData.Count)
                    {
                        Resize();
                    }

                    var widget = leaderboardWidgets[i];
                    var model = friendLeaderboardData[i];
                    widget.SetUIText(i + 1, model.username, model.score.ToString());
                    widget.gameObject.SetActive(true);
                }
            }
            else
            {
                if (!retrievedGlobalLeaderboard)
                {
                    ToggleLoadingScreen(true);
                    return;
                }

                ToggleLoadingScreen(false);

                //do General Logic
                List<LeaderboardModel> generalLeaderboardData;
                //do friend view logic

                generalLeaderboardData = Leaderboard.Instance.GetLeaderBoard(gameId);


                for (int i = 0; i < generalLeaderboardData.Count; i++)
                {
                    if (i >= leaderboardWidgets.Count)
                    {
                        Resize();
                    }

                    var widget = leaderboardWidgets[i];
                    var model = generalLeaderboardData[i];
                    widget.SetUIText(i + 1, model.username, model.score.ToString());
                    widget.gameObject.SetActive(true);
                }
            }


        }

        // private async Task<LeaderboardScore[]> RetrieveLeaderboardData()
        // {

        //     var userId = FirebaseManager.Instance.auth.CurrentUser.UserId;
        //     LeaderboardScore[] allTimeLeaderboards = null;
        //     List<LeaderboardModel> friendLeaderboards = null;

        //     //First checks if the timestamp exists
        //     if (!Leaderboard.Instance.CheckIfTimestampExists(gameId))
        //     {
        //         retrievedGlobalLeaderboard = false;
        //         retrievedFriendLeaderboard = false;
        //         ToggleLoadingScreen(true);
        //         //Do a network call
        //         Debug.Log("Leaderboard Timestamp does NOT exist for this leaderboard. Doing a network call!");

        //         //For the Friend
        //         friendLeaderboards = await APIClient.Instance.GetFriendLeaderboardsAsync(userId, gameId);
        //         Leaderboard.Instance.AddLeaderboard(gameId + " friends", friendLeaderboards);
        //         retrievedFriendLeaderboard = true;
        //         ToggleViewBasedOnState();

        //         //For Global
        //         allTimeLeaderboards = await APIClient.Instance.GetLeaderboardAsync(gameId);
        //         Leaderboard.Instance.AddLeaderboard(gameId, allTimeLeaderboards);
        //         retrievedGlobalLeaderboard = true;
        //         ToggleViewBasedOnState();

        //         Leaderboard.Instance.SetTimestamp(gameId);
        //     }
        //     else
        //     {
        //         //Timestamp exists. Check if the refresh timer is ready
        //         if (Leaderboard.Instance.IsTimestampPassedCooldown(gameId))
        //         {
        //             retrievedGlobalLeaderboard = false;
        //             retrievedFriendLeaderboard = false;

        //             ToggleLoadingScreen(true);

        //             Debug.Log("Leaderboard Timestamp is past the cooldown timer. Refreshing");

        //             //For the Friend
        //             friendLeaderboards = await APIClient.Instance.GetFriendLeaderboardsAsync(userId, gameId);
        //             Leaderboard.Instance.AddLeaderboard(gameId + " friends", friendLeaderboards);
        //             retrievedFriendLeaderboard = true;
        //             ToggleViewBasedOnState();

        //             //For Global
        //             allTimeLeaderboards = await APIClient.Instance.GetLeaderboardAsync(gameId);
        //             Leaderboard.Instance.AddLeaderboard(gameId, allTimeLeaderboards);
        //             retrievedGlobalLeaderboard = true;
        //             ToggleViewBasedOnState();

        //             Leaderboard.Instance.SetTimestamp(gameId);
        //         }
        //         else
        //         {
        //             retrievedFriendLeaderboard = true;
        //             retrievedGlobalLeaderboard = true;
        //         }
        //     }
        //     return allTimeLeaderboards;
        // }

        private void ToggleViewBasedOnState()
        {
            if (inFriendView)
                ToggleFriendView(true);
            else
                ToggleFriendView(false);
        }

        // public void RetryGame()
        // {
        //     // Get the current scene
        //     retryButton.interactable = false;
        //     Destroy(FindObjectOfType<ARSessionManager>().gameObject);
        //     ExploreController.Instance.ReloadScene();
        //     LoakARSession.Instance.arFinished = false;
        //     gamePaused = false;
        // }

        public void TogglePauseCanvas(bool val)
        {
            pauseCanvas.SetActive(val);
            gamePaused = val;
        }

        // public void ReplaceRetry(UnityAction action)
        // {
        //     resetScene = false;
        //     retryButton.onClick.RemoveAllListeners();
        //     retryButton.onClick.AddListener(() => {
        //         retryButton.interactable = false;
        //         canvas.SetActive(false);
        //         LoakARSession.Instance.arFinished = false;
        //         });
        //     retryButton.onClick.AddListener(action);
        // }

        // public void ExitGame()
        // {
        //     LoakARSession.Instance.ExitARScene();
        //     gamePaused = false;
        // }

        private void ToggleLoadingScreen(bool val)
        {
            leaderBoardLoadingBody.SetActive(val);
            leaderboardBody.SetActive(!val);
        }
        private void Resize()
        {
            var newWidget = Instantiate(leaderboardWidgetPrefab, leaderboardContents.transform);
            newWidget.gameObject.SetActive(false);
            leaderboardWidgets.Add(newWidget);
        }

        public void ShareScore()
        {
//             string gameName = "Test Experience";
//             string link;
// #if UNITY_IOS
//             link = "https://apps.apple.com/us/app/loak/id1658220955";
// #elif UNITY_ANDROID
// 		link = "https://play.google.com/store/apps/details?id=co.loak.Loak";
// #endif
//             if (newBestHighscoreImage.enabled)
//                 StartCoroutine(ShareSocial.Instance.ShareCoroutine("Try to beat that!", $"Just beat my high score of {completeScoreText.text} for {gameName} on Loak! Think you can beat it?\n{link}"));
//             else
//                 StartCoroutine(ShareSocial.Instance.ShareCoroutine("Try to beat that!", $"Just scored {completeScoreText.text} on {gameName} on Loak! Think you can beat it?\n{link}"));
        }

        #endregion;

        #region For Scanning (If there is meshing involved)

        /// <summary>
        /// Starts the scanning.
        /// Shows the Scanning UI and enables the arMeshManager
        /// </summary>
        public void StartScanPhase(bool meshingOn = true)
        {

            // gamePhasePanel.SetActive(enableGamePhasePanel);
            scanningPhasePanel.SetActive(true);
            // if (meshingOn)
            //     LoakARMeshing.Instance.meshManager.enabled = true;
        }

        public void EndScanPhase(bool meshingOff = true)
        {
            scanningPhasePanel.SetActive(false);
            // gamePhasePanel.SetActive(true);
            // if (meshingOff)
            //     LoakARMeshing.Instance.meshManager.enabled = false;
        }

        public void UpdateScan(float currentValue, float maxValue)
        {
            var newAmount = Mathf.Max(currentValue / maxValue, scanFillBar.fillAmount);
            newAmount = Mathf.Min(newAmount, 1f);
            scanFillBar.fillAmount = newAmount;

            if (newAmount == 1f)
            {
                EndScanPhase();
            }
        }
        #endregion
    }
}