using System;
using System.Linq;
using Caliburn.Micro;

namespace csAppraisalPlugin.Classes
{
    [Serializable]
    public class FunctionList : BindableCollection<Function>
    {
        /// <summary>
        /// Retreive the function by its ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Function RetreiveById(Guid id)
        {
            return this.FirstOrDefault(function => function.Id == id);
        }
    }
}