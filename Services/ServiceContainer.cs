namespace ExcelTableConverter.Services
{
    /// <summary>
    /// Simple service container for dependency injection
    /// 
    /// Provides basic service registration and resolution capabilities
    /// for the Excel table converter application.
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<Type, Func<object>> _factories = new();

        /// <summary>
        /// Registers a service instance
        /// </summary>
        /// <typeparam name="TInterface">The service interface type</typeparam>
        /// <param name="implementation">The service implementation instance</param>
        public void RegisterInstance<TInterface>(TInterface implementation)
            where TInterface : class
        {
            _services[typeof(TInterface)] = implementation;
        }

        /// <summary>
        /// Registers a service factory
        /// </summary>
        /// <typeparam name="TInterface">The service interface type</typeparam>
        /// <param name="factory">The factory function to create the service</param>
        public void RegisterFactory<TInterface>(Func<TInterface> factory)
            where TInterface : class
        {
            _factories[typeof(TInterface)] = () => factory();
        }

        /// <summary>
        /// Registers a service type with its implementation
        /// </summary>
        /// <typeparam name="TInterface">The service interface type</typeparam>
        /// <typeparam name="TImplementation">The service implementation type</typeparam>
        public void Register<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            _factories[typeof(TInterface)] = () => new TImplementation();
        }

        /// <summary>
        /// Resolves a service instance
        /// </summary>
        /// <typeparam name="T">The service type to resolve</typeparam>
        /// <returns>The service instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when the service is not registered</exception>
        public T Resolve<T>() where T : class
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var instance))
            {
                return (T)instance;
            }

            if (_factories.TryGetValue(type, out var factory))
            {
                var createdInstance = (T)factory();
                _services[type] = createdInstance; // Cache the instance
                return createdInstance;
            }

            throw new InvalidOperationException($"Service of type {type.Name} is not registered");
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        /// <typeparam name="T">The service type to check</typeparam>
        /// <returns>True if the service is registered, false otherwise</returns>
        public bool IsRegistered<T>() where T : class
        {
            var type = typeof(T);
            return _services.ContainsKey(type) || _factories.ContainsKey(type);
        }

        /// <summary>
        /// Clears all registered services
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _factories.Clear();
        }
    }
}