using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable
namespace SharedLibrary.util
{

    public static partial class Util
    {
        public static T DeepClone<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            // Serialize to JSON and then deserialize back to a new object
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
        }
    }
}
#pragma warning enable