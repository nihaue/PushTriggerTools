using System;
using System.Threading.Tasks;

namespace QuickLearn.LogicApps
{
    public interface ICallbackStore<T>
    {
        Task WriteCallbackAsync(string triggerId, Uri callbackUri, T triggerConfig);

        Task DeleteCallbackAsync(string triggerId);
    }
}
