using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using LogValues = System.Collections.Generic.IReadOnlyList<System.Collections.Generic.KeyValuePair<string, object?>>;

namespace ActiveSession.Tests
{
    public class MockedLogger
    {
        readonly Mock<ILogger> _loggerMock;
        public readonly String CategoryName;

        public MockedLogger(string CategoryName) 
        {
            _loggerMock=new Mock<ILogger>();
            _loggerMock.Setup(s => s.IsEnabled(It.IsAny<LogLevel>())).Returns((LogLevel x) => _enabledLevels[(int)x]);
            this.CategoryName=CategoryName;
        }

        Boolean[] _enabledLevels = new Boolean[7];
        public ILogger Logger { get { return _loggerMock.Object;} }

        public void MonitorLogEntry(LogLevel LogLevel, EventId EventId,
            Action<LogLevel, EventId, LogValues> Callback)
        {
            MonitorLogEntry(LogLevel, EventId, (LogLevel lvl, EventId id, LogValues vals, Exception? _, Delegate _)
                    => Callback(lvl, id, vals));
        }

        public void MonitorLogEntry(LogLevel LogLevel, EventId EventId,
            Action<LogLevel, EventId, LogValues, Exception?> Callback)
        {
            MonitorLogEntry(LogLevel, EventId, (LogLevel lvl, EventId id, LogValues vals, Exception? e, Delegate _)
                    => Callback(lvl, id, vals, e));
        }

        public void MonitorLogEntry(LogLevel LogLevel, EventId EventId, 
            Action<LogLevel, EventId, LogValues, Exception?, Delegate>? Callback=null)
        {
            _enabledLevels[(int)LogLevel]=true;
            var logger_setup = _loggerMock.Setup(x => x.Log<It.IsSubtype<LogValues>>(LogLevel, EventId
                , It.IsAny<It.IsSubtype<LogValues>>()
                , It.IsAny<Exception?>()
                , It.IsAny<Func<It.IsSubtype<LogValues>, Exception?, String>>())
            );
            if (Callback!=null)
                logger_setup.Callback(
                    (LogLevel lvl, EventId id, object vals,Exception? e, Delegate fmt)
                    =>Callback(lvl,id, (LogValues)vals,e, fmt));
        }


        public void VerifyLogEntry(LogLevel LogLevel, EventId EventId, Times Times)
        {
            _loggerMock.Verify(x => x.Log<It.IsSubtype<LogValues>>(LogLevel, EventId
                    , It.IsAny<It.IsSubtype<LogValues>>()
                    , It.IsAny<Exception?>()
                    , It.IsAny<Func<It.IsSubtype<LogValues>, Exception?, String>>()
                ), Times
            );

        }
    }

    public class MockedLoggerFactory
    {

        readonly Mock<ILoggerFactory> _loggerFactoryMock;
        public ILoggerFactory LoggerFactory { get => _loggerFactoryMock.Object; }
        Dictionary<String, LoggerCategoryInfo> _loggers = new Dictionary<String, LoggerCategoryInfo>();

        public MockedLoggerFactory()
        {
            _loggerFactoryMock=new Mock<ILoggerFactory>();
            _loggerFactoryMock.Setup(s => s.CreateLogger(It.IsAny<String>()))
                .Returns((String name) => {
                    MonitorLoggerCategory(name);
                    if (_loggers.ContainsKey(name)) {
                        _loggers[name].CreationCount++;
                        return _loggers[name].LoggerMock.Logger;
                    }
                    else return null!; 
                });
        }

        public void ResetAllCategories()
        {
            _loggers.Clear();
        }

        public Int32 LoggerCreationCount(String CategoryName)
        {
            return _loggers.ContainsKey(CategoryName) ? _loggers[CategoryName].CreationCount : 0;
        }

        delegate String GetTypeNameDelegate(Type T, bool FullName, bool IncludeGenericParameterNames, bool IncludeGenericParameters, char NestedTypeDelimiter);
        static Lazy<GetTypeNameDelegate> _getTypeNameDelegate = new Lazy<GetTypeNameDelegate>(CreateTypeNameDelegate);

        static GetTypeNameDelegate CreateTypeNameDelegate()
        {
            Assembly ilogger_assembly = Assembly.GetAssembly(typeof(ILogger)) 
                ?? throw new InvalidOperationException("An assebly containing ILogger not found");
            Type type_name_type = ilogger_assembly.GetType("Microsoft.Extensions.Internal.TypeNameHelper")
                ?? throw new InvalidOperationException("The assembly does not contain a TypeNameHelper type.");
            MethodInfo get_name_method_info = type_name_type
                .GetMethod("GetTypeDisplayName", new Type[] { typeof(Type), typeof(bool), typeof(bool), typeof(bool), typeof(char) })
                    ?? throw new InvalidOperationException("The type does not contain GetTypeDisplayName method.");
            GetTypeNameDelegate result = (GetTypeNameDelegate)(get_name_method_info.CreateDelegate(typeof(GetTypeNameDelegate)));

            //(Type type, bool fullName = true, bool includeGenericParameterNames = false, bool includeGenericParameters = true, char nestedTypeDelimiter = DefaultNestedTypeDelimiter));
            return result;
        }

        public MockedLogger MonitorLoggerCategory<T>()
        {
            String category_name=_getTypeNameDelegate.Value(typeof(T),true,false,false,'.');
            return MonitorLoggerCategory(category_name);
        }

        public MockedLogger MonitorLoggerCategory(String CategoryName)
        {
            Boolean already_monitored = _loggers.ContainsKey(CategoryName);
            if (!already_monitored) _loggers.Add(CategoryName, new LoggerCategoryInfo(CategoryName));
            MockedLogger category_logger = new MockedLogger(CategoryName);
            return _loggers[CategoryName].LoggerMock;
        }

        public class LoggerCategoryInfo
        {
            public MockedLogger LoggerMock { get; init; }
            public int CreationCount;
            public LoggerCategoryInfo(String CategoryName)
            {
                LoggerMock=new MockedLogger(CategoryName);
            }
        }
    }
}
