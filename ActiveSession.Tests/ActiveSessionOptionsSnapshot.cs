using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActiveSession.Tests
{
    internal class ActiveSessionOptionsSnapshot : IOptionsSnapshot<ActiveSessionOptions>
    {

        public ActiveSessionOptionsSnapshot(ActiveSessionOptions Value)
        {
            this.Value = Value;
        }

        public ActiveSessionOptions Value { get; set; }

        public ActiveSessionOptions Get(String name)
        {
            return Value;
        }
    }
}
