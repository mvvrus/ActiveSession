﻿
using Microsoft.Extensions.Options;

namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal class ActiveSessionIdSupplier : IActiveSessionIdSupplier
    {
        readonly string _prefix;

        public ActiveSessionIdSupplier(IOptions<ActiveSessionOptions> Options)
        {
            _prefix=Options.Value.Prefix;
        }

        public String GetActiveSessionId(ISession Session)
        {
            String? result;
            //result=Session.Id; //TODO(future) Change implementation
            //TODO(future) The new implementation
            result = Session.GetString(_prefix);
            if(result == null) {
                result = Guid.NewGuid().ToString();
                Session.SetString(_prefix, result);
            }
            return result;
        }
    }
}