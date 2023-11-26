using System;
using System.Collections.Generic;
using System.Linq;
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

        public void MonitorLogEntry(LogLevel LogLevel, EventId EventId)
        {
            _enabledLevels[(int)LogLevel]=true;
            _loggerMock.Setup(x=>x.Log<It.IsSubtype<LogValues>>(LogLevel, EventId
                , It.IsAny<It.IsSubtype<LogValues>>()
                , It.IsAny<Exception?>()
                , It.IsAny<Func<It.IsSubtype<LogValues>, Exception?,String>>() )
            );
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
