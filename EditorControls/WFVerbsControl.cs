﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AxeSoftware.Quest.EditorControls
{
    [ControlType("verbs")]
    class WFVerbsControl : WFAttributesControl
    {
        private static Dictionary<string, string> s_allowedTypes = new Dictionary<string, string> {
            {"string", "Print a message"},
            {"script", "Run a script"}
        };

        public WFVerbsControl()
        {
            ctlSplitContainerMain.Panel1Collapsed = true;
            lblAttributesTitle.Text = "Verbs";
            lblAttributesTitle.Width = 60;
        }

        protected override bool CanDisplayAttribute(string attribute, object value)
        {
            if (!Controller.IsVerbAttribute(attribute)) return false;
            return typeof(string).IsAssignableFrom(value.GetType()) || typeof(IEditableScripts).IsAssignableFrom(value.GetType());
        }

        protected override Dictionary<string, string> AllowedTypes
        {
            get { return s_allowedTypes; }
        }

        protected override void Add()
        {
            // TO DO: This fetches all verbs in the game, but verbs can be defined in rooms, so we should
            // filter out any out-of-scope verbs.

            IDictionary<string, string> availableVerbs = Controller.GetVerbProperties();

            PopupEditors.EditStringResult result = PopupEditors.EditString(
                "Please enter a name for the new verb",
                string.Empty,
                availableVerbs.Values);

            if (result.Cancelled) return;

            string selectedPattern = result.Result;

            var attributeForSelectedPattern = from verb in availableVerbs.Keys
                                              where availableVerbs[verb] == selectedPattern
                                              select verb;

            string selectedAttribute = attributeForSelectedPattern.FirstOrDefault();

            if (selectedAttribute == null)
            {
                // we couldn't find a matching verb property name, so see if there is a matching verb
                // pattern instead. For example, if the user typed "sit on" then we want to match
                // the "sit" verb, as "sit on" is one of its patterns.

                foreach (var verb in availableVerbs)
                {
                    List<string> patterns = new List<string>(verb.Value.Split(';').Select(p => p.Trim()));
                    if (patterns.Contains(selectedPattern))
                    {
                        selectedAttribute = verb.Key;
                        break;
                    }
                }
            }

            if (selectedAttribute == null)
            {
                // selectedPattern may be like "look in", "grab; snatch". We need to get a valid
                // attribute name from the pattern.

                int semicolonPos = selectedPattern.IndexOf(';');
                if (semicolonPos > -1)
                {
                    selectedAttribute = selectedPattern.Substring(0, semicolonPos);
                }
                else
                {
                    selectedAttribute = selectedPattern;
                }

                selectedAttribute = selectedAttribute.Replace(" ", "").Replace("#object#", "");
            }

            AddVerb(selectedPattern, selectedAttribute);
        }

        private void AddVerb(string selectedPattern, string selectedAttribute)
        {
            bool setSelection = true;

            if (!lstAttributes.Items.ContainsKey(selectedAttribute))
            {
                if (!Controller.IsVerbAttribute(selectedAttribute))
                {
                    if (!Controller.CanAddVerb(selectedPattern))
                    {
                        MessageBox.Show("Unable to add verb", "Unable to add verb", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                }

                Controller.StartTransaction(string.Format("Add '{0}' verb", selectedPattern));

                if (!Controller.IsVerbAttribute(selectedAttribute))
                {
                    CreateNewVerb(selectedPattern, selectedAttribute);
                }

                ValidationResult setAttrResult = Data.SetAttribute(selectedAttribute, String.Empty);
                if (!setAttrResult.Valid)
                {
                    PopupEditors.DisplayValidationError(setAttrResult, selectedAttribute, "Unable to add verb");
                    setSelection = false;
                }
                Controller.EndTransaction();
            }

            if (setSelection)
            {
                lstAttributes.Items[selectedAttribute].Selected = true;
                lstAttributes.SelectedItems[0].EnsureVisible();
            }
        }

        private void CreateNewVerb(string selectedPattern, string selectedAttribute)
        {
            string newVerbId = Controller.CreateNewVerb(null, false);
            IEditorData verbData = Controller.GetEditorData(newVerbId);
            verbData.SetAttribute("property", selectedAttribute);
            EditableCommandPattern pattern = (EditableCommandPattern)verbData.GetAttribute("pattern");
            pattern.Pattern = selectedPattern;
            verbData.SetAttribute("defaulttext", "You can't " + selectedPattern + " that.");
        }

        protected override string GetAttributeDisplayName(IEditorAttributeData attr)
        {
            string displayName = Controller.GetVerbPatternForAttribute(attr.AttributeName);
            return displayName ?? attr.AttributeName;
        }
    }
}
