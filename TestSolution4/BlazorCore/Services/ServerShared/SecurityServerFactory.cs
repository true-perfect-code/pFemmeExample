namespace BlazorCore.Services.ServerShared
{
    /// <summary>
    /// Factory to provide the correct ISecurityServer implementation based on the platform.
    /// Each platform project (WASM, Server, WPF) must register its own creator method at startup.
    /// </summary>
    public static class SecurityServerFactory
    {
        // Delegate that defines how to create an instance of ISecurityServer
        private static Func<ISecurityServer>? _creator;

        /// <summary>
        /// Registers the platform-specific implementation. 
        /// Call this once during the startup of the specific project (e.g., Program.cs).
        /// </summary>
        /// <param name="creator">A function that returns a new ISecurityServer instance.</param>
        public static void Register(Func<ISecurityServer> creator)
        {
            _creator = creator;
        }

        /// <summary>
        /// Creates a new instance of the registered ISecurityServer implementation.
        /// Use this within a 'using' block to ensure sensitive data is disposed quickly.
        /// </summary>
        /// <returns>A platform-specific implementation of ISecurityServer.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no implementation was registered.</exception>
        public static ISecurityServer Create()
        {
            if (_creator == null)
            {
                throw new InvalidOperationException(
                    "SecurityFactory: No implementation registered. " +
                    "Please call SecurityFactory.Register() in your platform-specific startup code.");
            }

            return _creator();
        }
    }
}
