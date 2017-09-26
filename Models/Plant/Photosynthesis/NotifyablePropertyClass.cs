using System.ComponentModel;

namespace Models.PMF.Phenology
{
    public class NotifyablePropertyClass : INotifyPropertyChanged
    {
        //Property changed event handler
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        public NotifyablePropertyClass() { }

        //-------------------------------------------------------------------
        // Create the OnPropertyChanged method to raise the event 
        //-------------------------------------------------------------------

        protected virtual void OnPropertyChanged(string name)
        {
            //PropertyChangedEventHandler handler = PropertyChanged;
            //if (handler != null)
            //{
            //    handler(this, new PropertyChangedEventArgs(name));
            //}
        }
    }
}
