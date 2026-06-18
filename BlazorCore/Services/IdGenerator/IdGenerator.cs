using BlazorCore.Services.GlobalState;
using System.Security.Cryptography;
using System.Text;

namespace BlazorCore.Services.IdGenerator
{
    public class IdGenerator : IIdGenerator
    {
        private readonly IGlobalStateBase _globalState;
        private int _userId = 0;
        private int _deviceId = 0;
        private long _lastTimestamp = 0;
        private readonly object _lock = new();

        public int UserId => _userId;
        public int DeviceId => _deviceId;

        public IdGenerator(IGlobalStateBase globalState)
        {
            _globalState = globalState;
        }

        public void InitializeUser(string user)
        {
            if (!string.IsNullOrEmpty(user))
            {
                _userId = GetUserIdHash(user);
            }
            else if (_userId == 0)
            {
                _userId = GenerateSecureRandomId(6);
            }
        }

        public void InitializeDevice(string deviceInfo)
        {
            if (_deviceId == 0)
            {
                if (!string.IsNullOrEmpty(deviceInfo))
                {
                    _deviceId = GetDeviceIdHash(deviceInfo);
                }
                else
                {
                    _deviceId = GenerateSecureRandomId(6);
                }
            }
        }

        public string GenerateUniqueId()
        {
            var epoch = new DateTime(_globalState.ConfigGeneral.EpochYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            lock (_lock)
            {
                long timestamp = (long)(DateTime.UtcNow - epoch).TotalMilliseconds;
                int randomRange = 1_000_000_000;
                const string randomFormat = "D9";

                int randomValue = RandomNumberGenerator.GetInt32(0, randomRange);
                string timestampWithRandom = timestamp.ToString() + randomValue.ToString(randomFormat);

                while (timestampWithRandom == _lastTimestamp.ToString())
                {
                    randomValue = RandomNumberGenerator.GetInt32(0, randomRange);
                    timestampWithRandom = timestamp.ToString() + randomValue.ToString(randomFormat);
                }

                _lastTimestamp = timestamp;

                string uniqueId = timestampWithRandom +
                                  _deviceId.ToString("D6") +
                                  _userId.ToString("D6");

                uniqueId = uniqueId.PadLeft(34, '0');
                return "T" + uniqueId;
            }
        }

        private int GenerateSecureRandomId(int digits)
        {
            int maxValue = (int)Math.Pow(10, digits);
            return RandomNumberGenerator.GetInt32(0, maxValue);
        }

        private int GetDeviceIdHash(string deviceInfo)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceInfo));
                int hash = BitConverter.ToInt32(hashBytes, 0);
                return Math.Abs(hash) % 1000000;
            }
        }

        private int GetUserIdHash(string userId)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
                int hash = BitConverter.ToInt32(hashBytes, 0);
                return Math.Abs(hash) % 1000000;
            }
        }
    }

}
