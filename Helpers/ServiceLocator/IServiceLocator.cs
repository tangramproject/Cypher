using System;

namespace TangramCypher.Helpers.ServiceLocator
{
    public interface IServiceLocator
    {
        T GetService<T>();
        void Add<TService, TImplementation>(TImplementation Class);
    }
}