using System;

namespace WpfApp1.Stores
{
    public class DeviceConnectionStore
    {
        private Connection? _connection;

        public Connection Connection => _connection;

        public event Action<Connection> ConnectionCreated;

        public void CreateDeviceConnection(Connection connection)
        {
            ConnectionCreated?.Invoke(connection);
            _connection = connection;
        }

        public string ConnectionName()
        {
            return _connection.ToString();
        }

        //public void GetInkDeviceInfo()
        //{
        //    _connection.InkDeviceInfo;
        //}

    }
}
