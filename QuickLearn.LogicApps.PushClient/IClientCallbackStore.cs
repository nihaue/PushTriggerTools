using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickLearn.LogicApps
{
    /// <summary>
    /// Use this interface as a guide as to which methods your event source must implement to successfully deal with callbacks in a push trigger scenario
    /// </summary>
    /// <typeparam name="TConfiguration">Type used for gathering the configuration of the push trigger in the Logic App designer</typeparam>
    public interface IClientCallbackStore<TConfiguration>
    {
        /// <summary>
        /// Asynchronously reads callbacks stored in the callback store for the current implementation
        /// </summary>
        /// <returns>An enumerable list of of callbacks that can be invoked</returns>
        Task<IEnumerable<Callback<TConfiguration>>> ReadCallbacksAsync();
    }
}
