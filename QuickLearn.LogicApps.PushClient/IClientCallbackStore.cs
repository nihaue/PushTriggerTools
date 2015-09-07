using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickLearn.LogicApps
{
    public interface IClientCallbackStore<TConfiguration, TOutput>
    {
        Task<IEnumerable<Callback<TConfiguration, TOutput>>> ReadCallbacksAsync();
    }
}
