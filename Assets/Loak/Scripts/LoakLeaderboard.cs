using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Loak.Unity
{
    // Controller for a simple to use leaderboard prefab.
    public class LoakLeaderboard : MonoBehaviour
    {
        public static LoakLeaderboard Instance;

        private enum LeaderboardConfiguration {Friend, Global, Both}
        [Tooltip("Use this to choose what tabs are available on your leaderboard.")]
        [SerializeField] private LeaderboardConfiguration leaderboardConfiguration = LeaderboardConfiguration.Both;

        [Tooltip("Specifies the number of entries to be displayed on the leaderboard.")]
        public int numberOfEntries = 10;
        [Tooltip("Username that should have a highlighted entry on the leaderboard.")]
        public string highlightedName = "You";

        private LeaderboardListItem listItemPrefab;
        private List<LeaderboardListItem> listItems = new List<LeaderboardListItem>();

        private Canvas canvas;
        private GameObject loadingView;
        private GameObject list;

        private Tab activeTab;
        private Tab friendsTab;
        private Tab globalTab;

        // Just sets the singleton reference.
        void Awake()
        {
            Instance = this;
        }

        // Grabs all necessary references and populates the leaderboard items.
        void Start()
        {
            canvas = GetComponent<Canvas>();
            friendsTab = new Tab(transform.GetChild(1).GetChild(2).GetChild(0).gameObject, null);
            globalTab = new Tab(transform.GetChild(1).GetChild(2).GetChild(1).gameObject, null);
            loadingView = transform.GetChild(1).GetChild(4).gameObject;

            switch (leaderboardConfiguration)
            {
                case LeaderboardConfiguration.Friend:
                    friendsTab.SetActive(true);
                    globalTab.SetActive(false);
                    ActivateTab(friendsTab);
                    break;

                case LeaderboardConfiguration.Global:
                    friendsTab.SetActive(false);
                    globalTab.SetActive(true);
                    ActivateTab(globalTab);
                    break;

                default:
                    friendsTab.SetActive(true);
                    globalTab.SetActive(true);
                    ActivateTab(friendsTab);
                    break;
            }

            listItemPrefab = GetComponentInChildren<LeaderboardListItem>(true);
            list = listItemPrefab.transform.parent.parent.parent.gameObject;
            listItemPrefab.gameObject.SetActive(true);

            if (listItems.Count == 0)
                listItems.Add(listItemPrefab);

            var parent = listItemPrefab.transform.parent;
            for (int i = listItems.Count; i < numberOfEntries; i++)
            {
                listItems.Add(Instantiate(listItemPrefab, parent));
                listItems[i].SetRank(i + 1);
            }

            listItemPrefab.gameObject.SetActive(false);
            loadingView.SetActive(true);
            list.SetActive(false);
        }

        /// <summary>
        /// Sets the list of leaderboard entries that the friend tab displays.
        /// </summary>
        /// <param name="entries">An ordered list of entries containing username and score.</param>
        public void SetFriendEntries(List<(string, long)> entries)
        {
            friendsTab.Update(entries);
            
            if (activeTab == friendsTab)
            {
                SetUIItems(entries);

                if (loadingView.activeSelf)
                {
                    loadingView.SetActive(false);
                    list.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Sets the list of leaderboard entries that the global tab displays.
        /// </summary>
        /// <param name="entries">An ordered list of entries containing username and score.</param>
        public void SetGlobalEntries(List<(string, long)> entries)
        {
            globalTab.Update(entries);
            
            if (activeTab == globalTab)
            {
                SetUIItems(entries);

                if (loadingView.activeSelf)
                {
                    loadingView.SetActive(false);
                    list.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Shows the leaderboard.
        /// </summary>
        public void Show()
        {
            canvas.enabled = true;
        }

        /// <summary>
        /// Hides the leaderboard.
        /// </summary>
        public void Hide()
        {
            canvas.enabled = false;
        }

        internal void ActivateTab(Tab tab)
        {
            tab.ToggleSelected(true);
            SetUIItems(tab.entries);

            if (activeTab != null)
                activeTab.ToggleSelected(false);
            activeTab = tab;
        }

        private void SetUIItems(List<(string, long)> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                listItems.ForEach((item) => {item.gameObject.SetActive(false);});
                return;
            }

            LeaderboardListItem item;
            for (int i = 0; i < numberOfEntries; i++)
            {
                if (i >= entries.Count)
                {
                    listItems[i].gameObject.SetActive(false);
                    continue;
                }

                item = listItems[i];
                item.SetUIText(entries[i].Item1, entries[i].Item2.ToString());
                item.Highlight(entries[i].Item1 == highlightedName ? true : false);
                item.gameObject.SetActive(true);
            }
        }
    }

    internal class Tab {
        private GameObject uiObject;
        private Button button;
        private Image bullet;
        public List<(string, long)> entries;

        public Tab(GameObject uiObject, List<(string, long)> entries)
        {
            this.uiObject = uiObject;
            this.button = uiObject.GetComponent<Button>();
            this.bullet = uiObject.GetComponentInChildren<Image>(true);
            this.entries = entries;

            var tab = this;
            button.onClick.AddListener(() => {LoakLeaderboard.Instance.ActivateTab(tab);});
        }

        public void SetActive(bool val)
        {
            uiObject.SetActive(val);
        }

        public void Update(List<(string, long)> entries)
        {
            this.entries = entries;
        }

        public void ToggleSelected(bool val)
        {
            bullet.enabled = val;
            button.interactable = !val;
        }
    }
}
