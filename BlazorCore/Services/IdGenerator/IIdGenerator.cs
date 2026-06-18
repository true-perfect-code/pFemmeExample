namespace BlazorCore.Services.IdGenerator
{
    public interface IIdGenerator
    {
        /// <summary>
        /// Initializes the user ID based on email/user identifier.
        /// Called once after login.
        /// </summary>
        void InitializeUser(string user);

        /// <summary>
        /// Initializes the device ID based on device info.
        /// Called once during app startup.
        /// </summary>
        void InitializeDevice(string deviceInfo);

        /// <summary>
        /// Generates a unique 35-character ID (prefixed with 'T').
        /// Combines timestamp + random + deviceId + userId.
        /// </summary>
        string GenerateUniqueId();

        /// <summary>
        /// Gets the current 6-digit user ID hash.
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// Gets the current 6-digit device ID hash.
        /// </summary>
        int DeviceId { get; }
    }
}
