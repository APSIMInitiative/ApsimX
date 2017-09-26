using System;
using System.ComponentModel;

namespace Models.PMF.Phenology
{
    public enum AngleType { Deg, Rad };
    
    public class Angle:INotifyPropertyChanged
    {
        private double _rad;
        private double _deg;

        //------------------------------------------------------------------------------------------------
        public Angle()
        {
        }
       //------------------------------------------------------------------------------------------------
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
         //Property changed event handler
        public event PropertyChangedEventHandler PropertyChanged;
        
        // Create the OnPropertyChanged method to raise the event 
        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
             handler(this, new PropertyChangedEventArgs(name));
            }
        }

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
