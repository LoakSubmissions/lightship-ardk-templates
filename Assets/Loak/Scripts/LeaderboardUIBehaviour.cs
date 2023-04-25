using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


namespace Loak.Unity
{
    internal class LeaderboardUIBehaviour : MonoBehaviour
    {
        [Header("Sections")]
        [SerializeField] private GameObject highLight;
        [SerializeField] private GameObject unhighlighted;


        [Header("Highlighted Section")]
        [SerializeField] private TextMeshProUGUI highlightedRankText;
        [SerializeField] private TextMeshProUGUI highlightedUsernameText;
        [SerializeField] private TextMeshProUGUI highlightedAmountCollected;

        [Header("UnHighlighted Section")]
        [SerializeField] private TextMeshProUGUI unHighlightedRankText;
        [SerializeField] private TextMeshProUGUI unHighlightedUsernameText;
        [SerializeField] private TextMeshProUGUI unHighlightedAmountCollected;



        public void SetUIText(int rank, string username, string amount)
        {
            highLight.SetActive(false);
            unhighlighted.SetActive(true);

            highlightedRankText.text = "#" + rank;
            highlightedUsernameText.text = username;
            highlightedAmountCollected.text = amount;

            unHighlightedRankText.text = "#" + rank;
            unHighlightedUsernameText.text = username;
            unHighlightedAmountCollected.text = amount;

            var currentUser = "Test User 1";

            if (currentUser != null)
            {
                if (username == "Test User 1")
                {
                    highLight.SetActive(true);
                    unhighlighted.SetActive(false);
                }
            }
        }

    }
}