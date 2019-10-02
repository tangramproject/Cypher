using System.Collections.Generic;

namespace Tangram.Address
{
    public class CachePerVersion<TValue>
    {
        private Dictionary<AddressVersion, TValue> Cache = new Dictionary<AddressVersion, TValue>();

        public bool Contains(AddressVersion addressVersion)
        {
            return Cache.ContainsKey(addressVersion);
        }

        public TValue Get(AddressVersion addressVersion)
        {
            return Cache.ContainsKey(addressVersion) ? Cache[addressVersion] : default(TValue);
        }

        public void Set(AddressVersion addressVersion, TValue value)
        {
            if (Cache.ContainsKey(addressVersion))
                Cache[addressVersion] = value;
            else
                Cache.Add(addressVersion, value);
        }
    }
}
