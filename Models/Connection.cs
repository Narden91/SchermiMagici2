using System;
using System.Windows.Media;
using Wacom.Devices;

namespace WpfApp1
{
    public class Connection
    {
        private readonly IInkDeviceInfo _inkDeviceInfo;

        public IInkDeviceInfo InkDeviceInfo => _inkDeviceInfo;

        public string Id => _inkDeviceInfo.Id;
        public ImageSource TransportImage => App.TransportImage(_inkDeviceInfo.TransportProtocol);
        public string DeviceName => _inkDeviceInfo.DeviceName;

        public Connection(IInkDeviceInfo inkDeviceInfo)
        {
            _inkDeviceInfo = inkDeviceInfo;
        }

        public override string ToString()
        {
            return "Device Id: " + this._inkDeviceInfo.Id + Environment.NewLine +
                   "Device Name: " + this._inkDeviceInfo.DeviceName + Environment.NewLine;
        }

        public IInkDeviceInfo GetInkDeviceInfo()
        {
            return _inkDeviceInfo;
        }

    }
}