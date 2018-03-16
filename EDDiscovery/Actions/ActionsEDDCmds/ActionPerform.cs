﻿/*
 * Copyright © 2017 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BaseUtils;
using ActionLanguage;

namespace EDDiscovery.Actions
{
    public class ActionPerform : ActionBase
    {
        public override bool AllowDirectEditingOfUserData { get { return true; } }

        public override bool ConfigurationMenu(Form parent, ActionCoreController cp, List<string> eventvars)
        {
            string promptValue = ExtendedControls.PromptSingleLine.ShowDialog(parent, "Perform command", UserData, "Configure Perform Command" , cp.Icon);
            if (promptValue != null)
            {
                userdata = promptValue;
            }

            return (promptValue != null);
        }

        public override bool ExecuteAction(ActionProgramRun ap)
        {
            string res;
            if (ap.functions.ExpandString(UserData, out res) != Conditions.ConditionFunctions.ExpandResult.Failed)
            {
                StringParser sp = new StringParser(res);
                string cmdname = sp.NextWord(" ", lowercase: true);

                if (cmdname == null)
                {
                    ap.ReportError("Missing command in Perform");
                }
                else if (cmdname.Equals("3dmap"))
                {
                    (ap.actioncontroller as ActionController).DiscoveryForm.Open3DMap(null);
                }
                else if (cmdname.Equals("2dmap"))
                {
                    (ap.actioncontroller as ActionController).DiscoveryForm.Open2DMap();
                }
                else if (cmdname.Equals("edsm"))
                {
                    ActionController ac = (ap.actioncontroller as ActionController);

                    EliteDangerousCore.EDSM.EDSMClass edsm = new EliteDangerousCore.EDSM.EDSMClass();

                    if (edsm.ValidCredentials)
                        EliteDangerousCore.EDSM.EDSMJournalSync.SendEDSMEvents(ap.actioncontroller.LogLine, ac.DiscoveryForm.history);
                    else
                        ap.ReportError("No valid EDSM Credentials");
                }
                else if (cmdname.Equals("refresh"))
                {
                    (ap.actioncontroller as ActionController).DiscoveryForm.RefreshHistoryAsync();
                }
                else if (cmdname.Equals("url"))
                {
                    string url = sp.LineLeft;

                    if (url.StartsWith("http:", StringComparison.InvariantCultureIgnoreCase) || url.StartsWith("https:", StringComparison.InvariantCultureIgnoreCase))        // security..
                    {
                        System.Diagnostics.Process.Start(url);
                    }
                    else
                        ap.ReportError("Perform url must start with http");
                }
                else if (cmdname.Equals("configurevoice"))
                    (ap.actioncontroller as ActionController).ConfigureVoice(sp.NextQuotedWord() ?? "Configure Voice Synthesis");
                else if (cmdname.Equals("manageaddons"))
                    (ap.actioncontroller as ActionController).ManageAddOns();
                else if (cmdname.Equals("editaddons"))
                    (ap.actioncontroller as ActionController).EditAddOns();
                else if (cmdname.Equals("editlastpack"))
                    (ap.actioncontroller as ActionController).EditLastPack();
                else if (cmdname.Equals("editpack"))
                {
                    string pack = sp.NextQuotedWord();
                    if (pack != null)
                    {
                        if (!(ap.actioncontroller as ActionController).EditPack(pack))
                            ap.ReportError("Pack " + pack + " not found");
                    }
                    else
                        ap.ReportError("EditPack requires a pack name");
                }
                else if (cmdname.Equals("editspeechtext"))
                    (ap.actioncontroller as ActionController).EditSpeechText();
                else if (cmdname.Equals("configurewave"))
                    (ap.actioncontroller as ActionController).ConfigureWave(sp.NextQuotedWord() ?? "Configure Wave Output");
                else if (cmdname.Equals("enableeliteinput"))
                    (ap.actioncontroller as ActionController).EliteInput(true, true);
                else if (cmdname.Equals("enableeliteinputnoaxis"))
                    (ap.actioncontroller as ActionController).EliteInput(true, false);
                else if (cmdname.Equals("disableeliteinput"))
                    (ap.actioncontroller as ActionController).EliteInput(false, false);
                else if (cmdname.Equals("enablevoicerecognition"))
                {
                    string culture = sp.NextQuotedWord();
                    if (culture != null)
                    {
                        ap["VoiceRecognitionEnabled"] = ((ap.actioncontroller as ActionController).VoiceReconOn(culture)).ToStringIntValue();
                    }
                    else
                        ap.ReportError("EnableVoiceRecognition requires a culture");
                }
                else if (cmdname.Equals("disablevoicerecognition"))
                    (ap.actioncontroller as ActionController).VoiceReconOff();
                else if (cmdname.Equals("beginvoicerecognition"))
                    (ap.actioncontroller as ActionController).VoiceLoadEvents();
                else if (cmdname.Equals("voicerecognitionconfidencelevel"))
                {
                    float? conf = sp.NextWord().InvariantParseFloatNull();
                    if (conf != null)
                        (ap.actioncontroller as ActionController).VoiceReconConfidence(conf.Value);
                    else
                        ap.ReportError("VoiceRecognitionConfidencelLevel requires a confidence value");
                }
                else if (cmdname.Equals("voicerecognitionparameters"))
                {
                    int? babble = sp.NextWordComma().InvariantParseIntNull();        // babble at end
                    int? initialsilence = sp.NextWordComma().InvariantParseIntNull(); // silence at end
                    int? endsilence = sp.NextWordComma().InvariantParseIntNull();        // unambigious timeout
                    int? endsilenceambigious = sp.NextWordComma().InvariantParseIntNull(); // ambiguous timeout

                    if (babble != null && initialsilence != null && endsilence != null && endsilenceambigious != null)
                        (ap.actioncontroller as ActionController).VoiceReconParameters(babble.Value, initialsilence.Value, endsilence.Value, endsilenceambigious.Value);
                    else
                        ap.ReportError("VoiceRecognitionParameters requires four values");
                }
                else if (cmdname.Equals("voicerecognitionphrases"))
                {
                    ap["Phrases"] = (ap.actioncontroller as ActionController).VoicePhrases(Environment.NewLine);
                }
                else if (cmdname.Equals("listeliteinput"))
                {
                    ap["EliteInput"] = (ap.actioncontroller as ActionController).EliteInputList();
                    ap["EliteInputCheck"] = (ap.actioncontroller as ActionController).EliteInputCheck();
                }
                else if (cmdname.Equals("voicenames"))
                {
                    ap["VoiceNames"] = (ap.actioncontroller as ActionController).SpeechSynthesizer.GetVoiceNames().QuoteStrings();
                }
                else if (cmdname.Equals("bindings"))
                {
                    ap["Bindings"] = (ap.actioncontroller as ActionController).FrontierBindings.ListBindings();
                }
                else if (cmdname.Equals("bindingvalues"))
                {
                    ap["BindingValues"] = (ap.actioncontroller as ActionController).FrontierBindings.ListValues();
                }
                else if (cmdname.Equals("datadownload"))
                {
                    string gitfolder = sp.NextQuotedWord();
                    string filewildcard = sp.NextQuotedWord();
                    string directory = sp.NextQuotedWord();
                    string optclean = sp.NextWord();

                    if (gitfolder != null && filewildcard != null && directory != null)
                    {
                        if (System.IO.Directory.Exists(directory))
                        {
                            BaseUtils.GitHubClass ghc = new BaseUtils.GitHubClass(EDDiscovery.Properties.Resources.URLGithubDataDownload);
                            bool worked = ghc.Download(directory, gitfolder, filewildcard, optclean != null && optclean == "1");
                            ap["Downloaded"] = worked.ToStringIntValue();
                        }
                        else
                            ap.ReportError("Download folder " + directory + " does not exist");
                    }
                    else
                        ap.ReportError("Missing parameters in Perform Datadownload");

                }
                else if (cmdname.Equals("generateevent" ))
                {
                    string eventname = sp.NextQuotedWordComma();      // get the journal entry as JSON

                    ActionEvent f = ActionEventEDList.EventList(excludejournal: true).Find(x => x.TriggerName.Equals(eventname));

                    if (f != null)
                    {
                        Conditions.ConditionVariables c = new Conditions.ConditionVariables();
                        if (c.FromString(sp, Conditions.ConditionVariables.FromMode.MultiEntryComma))
                        {
                            if (f.TriggerName.StartsWith("UI") || f.TriggerName.Equals("onEliteUIEvent"))
                            {
                                c["EventClass_EventTimeUTC"] = DateTime.UtcNow.ToStringUS();
                                c["EventClass_EventTypeID"] = c["EventClass_EventTypeStr"] = f.TriggerName.Substring(2);
                                c["EventClass_UIDisplayed"] = EDDConfig.Instance.ShowUIEvents ? "1" : "0";
                                (ap.actioncontroller as ActionController).ActionRun(Actions.ActionEventEDList.onUIEvent, c);
                            }

                            (ap.actioncontroller as ActionController).ActionRun(f, c, now: true);
                        }
                        else
                            ap.ReportError("Variables not in correct form");
                    }
                    else
                    {
                        try
                        {
                            EliteDangerousCore.JournalEntry je = EliteDangerousCore.JournalEntry.CreateJournalEntry(eventname);

                            if (je is EliteDangerousCore.JournalEvents.JournalUnknown)
                            {
                                ap.ReportError("Unknown journal event");
                            }
                            else
                            {
                                EliteDangerousCore.HistoryEntry he = EliteDangerousCore.HistoryEntry.FromJournalEntry(je, null, out bool journalupdate);
                                // may want to fill he in a bit
                                (ap.actioncontroller as ActionController).ActionRunOnEntry(he, Actions.ActionEventEDList.NewEntry(he), now: true);
                            }
                        }
                        catch
                        {
                            ap.ReportError("Journal event not in correct JSON form");
                        }
                    }
                }
                else
                    ap.ReportError("Unknown command " + cmdname + " in Performaction");
            }
            else
                ap.ReportError(res);

            return true;
        }
    }
}
