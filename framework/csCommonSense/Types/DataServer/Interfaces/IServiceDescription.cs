using System;
using Caliburn.Micro;

namespace DataServer
{
    public class ServiceDescription : PropertyChangedBase
    {
        /// <summary>
        ///     unique serviceId
        /// </summary>
        public Guid Id { get; set; }


        /// <summary>
        ///     Userfirendly name
        /// </summary>
        public string Name { get; set; }
    }
}