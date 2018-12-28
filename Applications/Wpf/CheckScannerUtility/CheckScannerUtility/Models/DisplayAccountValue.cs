﻿
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Rock.Apps.CheckScannerUtility.Models
{
    public class DisplayAccountValue : INotifyPropertyChanged
    {
        private string _accountDisplayName;
        private decimal _amount;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Index { get; set; }
        
        public string AccountDisplayName
        {
            get { return _accountDisplayName; }
            set
            {
                _accountDisplayName = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( "AccountDisplayName" ) );
            }
        }
        public decimal Amount
        {
            get { return _amount; }
            set
            {
                _amount = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( "Amount" ) );
            }
        }
    }
}
