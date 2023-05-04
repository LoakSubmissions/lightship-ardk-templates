using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Loak.Unity
{
    internal class LeaderboardListItem : MonoBehaviour
    {
        [Header("Color Settings")]
        [SerializeField] private Color highlightedTextColor = Color.white;
        [SerializeField] private Sprite highlightedBackground;

        [SerializeField] private Color unhighlightedTextColor = Color.black;
        [SerializeField] private Sprite unhighlightedBackground;

        private Image backgroundImage;
        private TMP_Text[] textElements;

        void Awake()
        {
            backgroundImage = GetComponentInChildren<Image>(true);
            textElements = GetComponentsInChildren<TMP_Text>(true);
        }

        void Start()
        {
            backgroundImage.sprite = unhighlightedBackground;
            foreach (TMP_Text text in textElements)
            {
                text.color = unhighlightedTextColor;
            }
        }

        /// <summary>
        /// Sets the rank text of this item.
        /// </summary>
        /// <param name="rank">The integer rank of this item in the list.</param>
        public void SetRank(int rank)
        {
            textElements[0].text = "#" + rank;
        }

        /// <summary>
        /// Sets the username and score text of this item.
        /// </summary>
        /// <param name="username">The username of the player who achieved <paramref name="score" />.</param>
        /// <param name="score">The score achieved by the player.</param>
        public void SetUIText(string username, string score)
        {
            textElements[1].text = username;
            textElements[2].text = score;
        }

        /// <summary>
        /// Highlights or unhighlights this item by swapping color palettes.
        /// </summary>
        /// <param name="val">True for highlighted, false for unhighlighted.</param>
        public void Highlight(bool val)
        {
            if (val)
            {
                backgroundImage.sprite = highlightedBackground;

                foreach (TMP_Text text in textElements)
                {
                    text.color = highlightedTextColor;
                }
            }
            else
            {
                backgroundImage.sprite = unhighlightedBackground;
                
                foreach (TMP_Text text in textElements)
                {
                    text.color = unhighlightedTextColor;
                }
            }
        }
    }
}