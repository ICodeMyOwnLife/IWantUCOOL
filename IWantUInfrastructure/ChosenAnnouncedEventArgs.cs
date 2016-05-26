using System;


namespace IWantUInfrastructure
{
    public class ChosenAnnouncedEventArgs: EventArgs
    {
        #region  Properties & Indexers
        public string Id { get; set; }
        public bool IsChosen { get; set; }
        #endregion
    }
}