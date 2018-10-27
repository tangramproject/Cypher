using System;
using System.Linq;
using System.Collections.Generic;

namespace TangramCypher.Helpers.ServiceLocator
{
    public class Locator : IServiceLocator
    {
        static readonly IDictionary<Type, object> servicesType = new Dictionary<Type, object>();
        static readonly object TheLock = new object();
        static IServiceLocator instance;

        public Locator()
        {
        }

        public void Add<TService, TImplementation>(TImplementation obj)
        {
            servicesType.Add(typeof(TService), obj);
        }

        public static IServiceLocator Instance
        {
            get
            {
                lock (TheLock)
                {
                    if (instance == null)
                    {
                        instance = new Locator();
                    }
                }

                return instance;
            }
        }

        public T GetService<T>()
        {
            try
            {
                return (T)servicesType[typeof(T)];
            }
            catch (KeyNotFoundException)
            {
                throw new ApplicationException("The requested service is not registered");
            }
        }
    }
}

