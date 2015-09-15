using System;
using System.Threading.Tasks;

namespace QuickLearn.LogicApps
{

    /// <summary>
    /// Use this interface as a guide as to which methods your push trigger must implement to successfully deal with callbacks
    /// </summary>
    /// <typeparam name="TConfiguration">Type used for gathering the configuration of the push trigger in the Logic App designer</typeparam>
    public interface ICallbackStore<TConfiguration>
    {
        /// <summary>
        /// Asynchronously writes a callback to the callback store for the current implementation
        /// </summary>
        /// <param name="triggerId">Name of the Logic App for which this callback is being stored</param>
        /// <param name="callbackUri">URI with inline credentials as provided by the Logic App</param>
        /// <param name="triggerConfig">Configuration for the push trigger as specified in the card for the push trigger in the Logic App designer</param>
        Task WriteCallbackAsync(string triggerId, Uri callbackUri, TConfiguration triggerConfig);

        /// <summary>
        /// Asynchronously deletes a callback to the callback store for the current implementation
        /// </summary>
        /// <param name="triggerId">Name of the Logic App for which this callback is being stored</param>
        Task DeleteCallbackAsync(string triggerId);
    }
}
