﻿using System;
using CB.Model.Common;


namespace IWantUInfrastructure
{
    public class Message: BindableObject
    {
        #region Fields
        private string _content;
        #endregion


        #region  Properties & Indexers
        public string Content
        {
            get { return _content; }
            set { SetProperty(ref _content, value); }
        }

        public Account Friend { get; set; }
        #endregion


        #region Methods
        public void AddMessageContent(string messageContent)
            => Content = Content == null
                             ? messageContent
                             : $"{Content}{Environment.NewLine}{Friend.Name}: {messageContent}";
        #endregion
    }
}