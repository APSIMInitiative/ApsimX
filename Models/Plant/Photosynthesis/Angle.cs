using System;
using System.ComponentModel;

namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public enum AngleType
    {   /// <summary></summary>
        Deg,
        /// <summary></summary>
        Rad
    };
    
    /// <summary></summary>
    public class Angle:INotifyPropertyChanged
    {
        private double _rad;
        private double _deg;

        /// <summary></summary>
        public Angle()
        {
        }

        /// <summary></summary>
        public Angle(double val, AngleType type)
        {
            if (type == AngleType.Deg)
            {
                _deg = val;
                _rad = DegToRad(val);
            }
            else
            {
                _rad = val;
                _deg = RadToDeg(val);
            }
        }
        //------------------------------------------------------------------------------------------------
        double DegToRad(double degs)
        {
            return degs * Math.PI / 180.0;
        }
        //------------------------------------------------------------------------------------------------
        double RadToDeg(double rads)
        {
            return rads * 180.0 / Math.PI;
        }

        //------------------------------------------------------------------------------------------------
        // Properties
        //------------------------------------------------------------------------------------------------
        /// <summary></summary>
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event 
        /// <summary></summary>
        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
             handler(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary></summary>
        public double rad
        {
            get { return _rad; }
            set
            {
                _rad = value;
                _deg = RadToDeg(value);
                //OnPropertyChanged("deg");
                OnPropertyChanged("rad");
            }
        }

        /// <summary></summary>
        public double deg
        {
            get { return _deg; }
            set
            {
                _deg = value;
                _rad = DegToRad(value);
                //OnPropertyChanged("deg");
                OnPropertyChanged("rad");

            }
        }
    }
}
