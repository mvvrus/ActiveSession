﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace ActiveSession.Tests
{
    public class SpyRunnerBase<TResult> : IActiveSessionRunner<TResult>
    {
        public ActiveSessionRunnerState State => throw new NotImplementedException();

        public int Position => throw new NotImplementedException();

        public void Abort()
        {
            throw new NotImplementedException();
        }

        public ActiveSessionRunnerResult<TResult> GetAvailable(int StartPosition, Int32 Advance, String? TraceIdentifier)
        {
            throw new NotImplementedException();
        }

        public IChangeToken GetCompletionToken()
        {
            throw new NotImplementedException();
        }

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMore(int StartPosition, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask<ActiveSessionRunnerResult<TResult>> GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }

    public record Request1
    {
        public String Arg { get; set; } = "";
    }

    public class Result1
    {
        public String Value { get; init; }
        public Result1(String Value)
        {
            this.Value=Value;
        }
    }

    public class SpyRunner1 : SpyRunnerBase<Result1>
    {
        public Request1 Request { get; init; }
        public SpyRunner1(Request1 Request)
        {
            this.Request=Request;
        }
    }

    class SpyRunner2 : SpyRunner1
    {
        public SpyRunner2(Request1 Request) : base(Request) { }
        public SpyRunner2(String StringRequest) : base(new Request1 { Arg=StringRequest }) { }
        public SpyRunner2(int IntRequest) : base(new Request1 { Arg=IntRequest.ToString() }) { }
    }

    class SpyRunner3 : SpyRunner1
    {
        public SpyRunner3(Request1 Request) : base(Request) { }
        [ActiveSessionConstructor]
        public SpyRunner3(String StringRequest) : base(new Request1 { Arg=StringRequest }) { }
        public SpyRunner3(int IntRequest) : base(new Request1 { Arg=IntRequest.ToString() }) { }
    }

    class SpyRunner4 : SpyRunner1
    {
        public SpyRunner4(Request1 Request) : base(Request) { }
        [ActiveSessionConstructor(false)]
        public SpyRunner4(String StringRequest) : base(new Request1 { Arg=StringRequest }) { }
        public SpyRunner4(int IntRequest) : base(new Request1 { Arg=IntRequest.ToString() }) { }
    }

    class SpyRunner5 : SpyRunner1
    {
        public SpyRunner5(Request1 Request) : base(Request) { }
        [ActiveSessionConstructor]
        public SpyRunner5(String StringRequest) : base(new Request1 { Arg=StringRequest }) { }
        [ActivatorUtilitiesConstructor]
        public SpyRunner5(int IntRequest) : base(new Request1 { Arg=IntRequest.ToString() }) { }
    }

    class SpyRunner6 : SpyRunner1, IActiveSessionRunner<String>
    {
        public SpyRunner6(Request1 Request) : base(Request)
        {
        }

        ActiveSessionRunnerResult<String> IActiveSessionRunner<String>.GetAvailable(Int32 StartPosition, Int32 Advance, String? TraceIdentifier)
        {
            throw new NotImplementedException();
        }

        ValueTask<ActiveSessionRunnerResult<String>> IActiveSessionRunner<String>.GetMoreAsync(Int32 StartPosition, Int32 Advance, String? TraceIdentifier, CancellationToken Token)
        {
            throw new NotImplementedException();
        }
    }

    class SpyRunner7 : SpyRunner6
    {
        public SpyRunner7(Request1 Request) : base(Request) { }
        public SpyRunner7(String StringRequest) : base(new Request1 { Arg=StringRequest }) { }
        public SpyRunner7(int IntRequest) : base(new Request1 { Arg=IntRequest.ToString() }) { }
    }

    public interface ISpyInterface1
    {
        public String Value { get; }
    }
    public class SpyService : ISpyInterface1
    {
        public const String THE_VALUE = "SpyService";
        public string Value => THE_VALUE;
    }

    public class SpyRunner8 : SpyRunner1
    {
        public String Param1 { get; init; } = "";
        public int Param2 { get; init; }
        public ISpyInterface1? Param3 { get; init; }
        public SpyRunner8(Request1 Request, String Param1, int Param2, ISpyInterface1? Param3) : base(Request)
        {
            this.Param1=Param1;
            this.Param2=Param2;
            this.Param3=Param3;
        }
    }

    public class SpyService2 : ISpyInterface1
    {
        public const String THE_VALUE = "SpyService2";
        public string Value => THE_VALUE;
    }

}

