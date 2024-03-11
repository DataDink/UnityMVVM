using System.Collections.Generic;

namespace UnityMVVM.Base
{
    /// <summary>
    /// Represents a data-set that should be explicitly treated as a model.
    /// </summary>
    public interface IViewModel : IDictionary<string, object> { }
}
