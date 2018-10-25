using System;
using System.Linq;
using System.Collections.Generic;

namespace TangramCypher.Helpers.ServiceLocator
{
    public class Locator : IServiceLocator
    {
        readonly IDictionary<object, object> servicesType;
        static readonly object TheLock = new Object();
        static IServiceLocator instance;

        public Locator()
        {
            this.servicesType = new Dictionary<object, object>();
        }


        public void Add<TService, TImplementation>(TImplementation Class)
        {
            this.servicesType.Add(typeof(TService), typeof(TImplementation));
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