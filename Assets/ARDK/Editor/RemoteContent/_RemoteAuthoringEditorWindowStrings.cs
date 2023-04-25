using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARDK.AR.WayspotAnchors.Editor
{
    internal static class _RemoteAuthoringEditorWindowStrings
    {
        /// <summary>
        /// User facing text.  En-US
        /// </summary>
        public const string DOCUMENTATION_LINK_URL =
          @"https://8th.io/ardk-remote-authoring-docs";

        public const string GEOSPATIAL_BROWSER_URL = @"https://8th.io/ardk-lightship-gsb";
        public const string ICON_PATH = "Assets/ARDK/Editor/Icons/";
        public const string ICON_EXT = ".png";
        public const string EDITOR_WINDOW_TITLE = "VPS Authoring";

        public const string GSB_DESCRIPTION =
          "Get new locations from \nthe Geospatial Browser";

        public const string TEXT_LINK_TO_DOCUMENTATION = "Documentation";
        public const string SELECTED_LOCATION_DROPDOWN_LABEL_TEXT = "Selected Location";

        public const string IMPORT_MESH_LABEL = "Import \nDownloaded Mesh";
        public const string RENAME_LOCATION_ICON_TOOLTIP = "Rename the selected location";
        public const string TRASH_LOCATION_ICON_TOOLTIP = "Remove the selected location from your project";
        public const string TRASH_ANCHOR_ICON_TOOLTIP = "Remove the selected anchor";
        public const string SELECT_LOCATION_PROMPT = "Please select a location";
        public const string CREATE_ANCHOR_PROMPT = "Please create an anchor";

        public const string SELECTED_LOCATION_DESCRIPTION_TEXT =
          "View the manifest for this location in an inspector window";

        public const string REMOVE_LOCATION_PROMPT =
          "Are you sure you want to remove this location from your project?";

        public const string DATA_MGMT_LABEL_TEXT = "   Data Management";
        public const string INSPECT_MANIFEST_BTN_TEXT = "Inspect Manifest";
        public const string INSPECT_MANIFEST_BTN_TOOLTIP = "";
        public const string SAVE_ANCHORS_BTN_TEXT = "Save All Anchors";
        public const string SAVE_DESCRIPTION_TEXT = "Write anchor data to a manifest for this location";
        public const string REMOVE_PROMPT_TITLE = "Remove Selected Anchor";
        public const string RENAME_ANCHOR_ICON_TOOLTIP = "Rename the selected anchor";
        public const string SAVE_ANCHOR_TOOLTIP = "Save anchor information";
        public const string UNDO_ANCHOR_CHANGES_TOOLTIP = "Undo changes to anchor";

        public const string REMOTE_PROMPT_DESCRIPTION =
          "Are you sure you want to remove this anchor from your location?";

        public const string CREATE_SECTION_LABEL = "   Anchor Management";
        public const string CREATE_SECTION_DESCRIPTION = "Create and edit anchors in your scene.";
        public const string CREATE_ANCHOR_LABEL_TEXT = "  Create New Anchor";

        public const string CREATE_ANCHOR_DESCRIPTION_TEXT =
          "Add a new anchor, an object designed to track a real world location \n in relation to this location";

        public const string CONFIRM_TEXT = "Yes";
        public const string CANCEL_TEXT = "Cancel";

    }
}
