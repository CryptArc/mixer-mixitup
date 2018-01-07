﻿using MixItUp.Base.ViewModel.User;
using MixItUp.Base.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Threading;
using MixItUp.Base.Util;

namespace MixItUp.Base.Actions
{
    public class XSplitAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return XSplitAction.asyncSemaphore; } }

        public static string XSplitReferenceTextFilesDirectory = Path.Combine(Environment.CurrentDirectory, "XSplit", "SourceTextFiles");

        [DataMember]
        public string SceneName { get; set; }

        [DataMember]
        public string SourceName { get; set; }
        [DataMember]
        public bool SourceVisible { get; set; }

        [DataMember]
        public string SourceText { get; set; }

        [DataMember]
        public string SourceURL { get; set; }

        [DataMember]
        public XSplitSourceDimensions SourceDimensions { get; set; }

        [JsonIgnore]
        private string currentTextToWrite { get; set; }

        public XSplitAction() : base(ActionTypeEnum.XSplit) { }

        public XSplitAction(string sceneName) : this() { this.SceneName = sceneName; }

        public XSplitAction(string sourceName, bool sourceVisible, string sourceText = null, string sourceURL = null)
            : this(sourceName, sourceVisible)
        {
            this.SourceText = sourceText;
            this.SourceURL = sourceURL;
            if (!string.IsNullOrEmpty(this.SourceURL))
            {
                if (!File.Exists(this.SourceURL) && !this.SourceURL.Contains("://"))
                {
                    this.SourceURL = "http://" + this.SourceURL;
                }
            }
        }

        public XSplitAction(string sourceName, bool sourceVisible, XSplitSourceDimensions sourceDimensions)
            : this(sourceName, sourceVisible)
        {
            this.SourceDimensions = sourceDimensions;
        }

        private XSplitAction(string sourceName, bool sourceVisible)
            : this()
        {
            this.SourceName = sourceName;
            this.SourceVisible = sourceVisible;
        }

        public string LoadTextFromFilePath { get { return Path.Combine(XSplitReferenceTextFilesDirectory, this.SourceName + ".txt"); } }

        public void UpdateReferenceTextFile()
        {
            if (!string.IsNullOrEmpty(this.SourceName) && !string.IsNullOrEmpty(this.SourceText))
            {
                try
                {
                    Directory.CreateDirectory(XSplitReferenceTextFilesDirectory);

                    string filePath = Path.Combine(XSplitReferenceTextFilesDirectory, this.SourceName + ".txt");
                    using (StreamWriter writer = new StreamWriter(File.Open(filePath, FileMode.Create)))
                    {
                        writer.Write(this.currentTextToWrite);
                    }
                }
                catch (Exception ex) { Logger.Log(ex); }
            }
        }

        protected override async Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            if (ChannelSession.Services.XSplitServer == null)
            {
                await ChannelSession.Services.InitializeXSplitServer();
            }

            if (ChannelSession.Services.XSplitServer != null)
            {
                if (!string.IsNullOrEmpty(this.SceneName))
                {
                    await ChannelSession.Services.XSplitServer.SetCurrentScene(new XSplitScene() { sceneName = this.SceneName });
                }

                if (!string.IsNullOrEmpty(this.SourceName))
                {
                    if (!string.IsNullOrEmpty(this.SourceText))
                    {
                        this.currentTextToWrite = await this.ReplaceStringWithSpecialModifiers(this.SourceText, user, arguments);
                        this.UpdateReferenceTextFile();
                    }
                    else if (!string.IsNullOrEmpty(this.SourceURL))
                    {
                        string url = await this.ReplaceStringWithSpecialModifiers(this.SourceURL, user, arguments);
                        await ChannelSession.Services.XSplitServer.SetWebBrowserSource(new XSplitWebBrowserSource() { sourceName = this.SourceName, sourceVisible = this.SourceVisible, webBrowserUrl = url });
                    }
                    else if (this.SourceDimensions != null)
                    {
                        await ChannelSession.Services.XSplitServer.SetSourceDimensions(this.SourceDimensions);
                    }
                    await ChannelSession.Services.XSplitServer.SetSourceVisibility(new XSplitSource() { sourceName = this.SourceName, sourceVisible = this.SourceVisible });
                }
            }
        }
    }
}
