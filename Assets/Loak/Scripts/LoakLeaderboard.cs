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
                listItems[i].gameObject.SetActive(false);
            }

            loadingView.SetActive(true);
            list.SetActive(false);
        }

        internal void ActivateTab(Tab tab)
        {
            tab.ToggleSelected(true);
            SetUI(tab.entries);

            if (activeTab != null)
                activeTab.ToggleSelected(false);
            activeTab = tab;
        }

        private void SetUI(List<(string, long)> entries)
        {
            
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
