using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace TangramCypher.Helpers
{
    public static class ExtentionMethods
    {
        public static StringContent AsJson(this object o)
          => new StringContent(JsonConvert.SerializeObject(o), Encoding.UTF8, "application/json");
    }
}
