using System;
using Microsoft.Azure.WebJobs.Description;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Config
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PerperWorkerAttribute : Attribute
    {
        public string Stream { get; }
        
        public PerperWorkerAttribute(string stream)
        {
            Stream = stream;
        }
    }
}