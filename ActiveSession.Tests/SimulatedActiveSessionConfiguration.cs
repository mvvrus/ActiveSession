using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using MVVrus.AspNetCore.ActiveSession.Internal;


namespace ActiveSession.Tests
{
    public static class SimulatedActiveSessionConfiguration
    {
        static Type _configType=typeof(ActiveSessionOptions);
        public static IConfiguration CreateSimulatedActiveSessionCongfiguration(Object? ConfigValues=null)
        {
            ConfigurationManager mgr = new ConfigurationManager();
            mgr.Add<MemoryConfigurationSource>(x => { });
            if (ConfigValues!=null) {
                foreach (PropertyInfo value_property in ConfigValues!.GetType().GetProperties()) {
                    PropertyInfo? config_property = _configType.GetProperty(value_property.Name);
                    if (config_property!=null) {
                        if (config_property.PropertyType.IsValueType||config_property.PropertyType==typeof(String)) {
                            String value = value_property.GetValue(ConfigValues!)?.ToString()??"";
                            mgr[ActiveSessionConstants.CONFIG_SECTION_NAME+":"+config_property.Name]=value;
                        }
                    }
                    else {
                        //TODO
                    }
                }
            }
            return mgr;
        }
    }
}
