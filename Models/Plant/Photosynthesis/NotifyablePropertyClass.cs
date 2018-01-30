using System.ComponentModel;

namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class NotifyablePropertyClass : INotifyPropertyChanged
    {
        /// <summary>
        /// Property changed event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        /// <summary></summary>
        public NotifyablePropertyClass() { }


        /// <summary>
        /// Create the OnPropertyChanged method to raise the event
        /// </summary>
        /// <param name="name"></param>
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