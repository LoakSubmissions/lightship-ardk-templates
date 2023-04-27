using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Loak.Unity
{
    public class LoakLeaderboard : MonoBehaviour
    {
        public static LoakLeaderboard Instance;

        private enum LeaderboardConfiguration {Friend, Global, Both}
        [SerializeField] private LeaderboardConfiguration leaderboardConfiguration = LeaderboardConfiguration.Both;

        public int numberOfEntries = 10;

        private LeaderboardUIBehaviour listItemPrefab;
        private List<LeaderboardUIBehaviour> listItems = new List<LeaderboardUIBehaviour>();

        private Canvas canvas;
        private GameObject loadingView;
        private GameObject list;

        private Tab activeTab;
        private Tab friendsTab;
        private Tab globalTab;

        void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
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

            listItemPrefab = GetComponentInChildren<LeaderboardUIBehaviour>(true);
            list = listItemPrefab.transform.parent.parent.parent.gameObject;
            listItemPrefab.gameObject.SetActive(false);

            if (listItems.Count == 0)
                listItems.Add(listItemPrefab);

            var parent = listItemPrefab.transform.parent;
            for (int i = listItems.Count; i < numberOfEntries; i++)
            {
                listItems.Add(Instantiate(listItemPrefab, parent));
                listItems[i].SetRank(i + 1);
            }

            loadingView.SetActive(true);
            list.SetActive(false);
        }

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

        public void Show()
        {
            canvas.enabled = true;
        }

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

            LeaderboardUIBehaviour item;
            for (int i = 0; i < numberOfEntries; i++)
            {
                if (i >= entries.Count)
                {
                    listItems[i].gameObject.SetActive(false);
                    continue;
                }

                item = listItems[i];
                item.SetUIText(entries[i].Item1, entries[i].Item2.ToString());
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
