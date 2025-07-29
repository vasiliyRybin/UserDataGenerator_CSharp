using MethodDecorator.Fody.Interfaces;
using Serilog;
using System;
using System.Linq;
using System.Reflection;

namespace UserDataGenerator_C_
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Assembly | AttributeTargets.Module)]
    public class LogMethodAttribute : Attribute, IMethodDecorator
    {
        private MethodBase _method;

        public void Init(object instance, MethodBase method, object[] args)
        {
            _method = method;
        }

        public void OnEntry()
        {
            if (IsDebugMode()) Log.Debug("Entering function " + _method.Name);
        }

        public void OnExit()
        {
            if (IsDebugMode()) Log.Debug("Leaving function " + _method.Name);
        }

        public void OnException(Exception exception)
        {
            if (IsDebugMode()) Log.Debug(exception, "Error in method " + _method.Name);
        }
        private bool IsDebugMode()
        {
            var args = Environment.GetCommandLineArgs();
            return args.Any(a => a.Equals("debug", StringComparison.OrdinalIgnoreCase));
        }
    }
}
