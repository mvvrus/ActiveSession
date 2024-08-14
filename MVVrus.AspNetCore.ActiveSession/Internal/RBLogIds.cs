﻿namespace MVVrus.AspNetCore.ActiveSession.Internal
{
    internal static class RBLogIds
    {
        public const Int32 E_RUNNERBASE = 10000;
        public const Int32 E_RUNNERSTARTBKGFAILED = 10001;
        public const Int32 E_ENUMERABLERUNNERBASEGETAVAILEXCEPTION = 10101;
        public const Int32 E_ENUMERABLERUNNERBASEGETREQUIREDEXCEPTION = 10102;
        public const Int32 E_ENUMADAPTERRUNNERDISPOSEEXCEPTION = 10201;
        public const Int32 E_ENUMADAPTERRUNNERSOURCEENUMERATIONEXCEPTION = 10202;
        public const Int32 E_ENUMADAPTERRUNNERAWAITCONTINUATIONEXCEPTION = 10203;
        public const Int32 E_ASYNCENUMADAPTERRUNNERSOURCEENUMERATIONEXCEPTION = 10301;
        public const Int32 E_SESSIONPROCESSRUNNERCONTINTERNALERROR = 10501;

        public const Int32 W_RUNNERBASE = 11000;
        public const Int32 W_RUNNERBASEUNEXPECTEDSTATUS = 11001;
        public const Int32 W_RUNNERTASKRESULTALREADYSET = 11002;
        public const Int32 W_ENUMERABLERUNNERBASEPARALLELGET = 11101;
        public const Int32 W_ENUMERABLERUNNERBASEBADPARAM = 11102;
        public const Int32 W_ENUMERABLERUNNERBASETASKRESULTALREADYSET = 11103;
        public const Int32 W_SESSIONPROCESSRUNNERBADPARAM = 11501;

        public const Int32 I_RUNNERBASESTARTING = 12000;
        public const Int32 I_RUNNERBASECOMPLETED = 12001;
        public const Int32 I_RUNNERBASESTARTFAILED = 12002;
        public const Int32 I_RUNNERBASEBKGSTARTED = 12003;
        public const Int32 I_RUNNERBASEBKGFINISHED = 12004;

        public const Int32 D_GETREQUIREDASYNCCANCELED = 13001;
        public const Int32 D_GETREQUIREDASYNCFAILED = 13002;
        public const Int32 D_ENUMERABLERUNNERBASERESULT = 13100;
        public const Int32 D_ENUMADAPTERRUNNERPARAMS = 13200;
        public const Int32 D_ASYNCENUMADAPTERRUNNERPARAMS = 13300;
        public const Int32 D_TIMESERIESRUNNERPARAMS = 13400;
        public const Int32 D_SESSIONPROCESSRUNNERPARAMS = 13500;
        public const Int32 D_SESSIONPROCESSRUNNERRESULT = 13501;

        public const Int32 T_RUNNERBASE = 14000;
        public const Int32 T_RUNNERBASECONSENTER = 14001;
        public const Int32 T_RUNNERBASECONSEXIT = 14002;
        public const Int32 T_RUNNERBASEDISPOSE = 14003;
        public const Int32 T_RUNNERBASESESTATENOTSTARTED = 14004;
        public const Int32 T_RUNNERBASECHANGEFINALSTATE = 14005;
        public const Int32 T_RUNNERBASESTATESET = 14006;
        public const Int32 T_RUNNERBASEREACHFINAL = 14007;
        public const Int32 T_RUNNERBASESTARTED = 14008;
        public const Int32 T_RUNNERBASESABORTCALLED = 14009;

        public const Int32 T_ENUMRUNNERBASE = 14100;
        public const Int32 T_ENUMRUNNERBASECONSENTER = 14101;
        public const Int32 T_ENUMRUNNERBASECONSEXIT = 14102;
        public const Int32 T_ENUMRUNNERBASEDISPOSEASYNC = 14103;
        public const Int32 T_ENUMRUNNERBASEPREDISPOSE = 14104;
        public const Int32 T_ENUMRUNNERBASEDISPOSECORE = 14105;
        public const Int32 T_ENUMRUNNERBASEPSEUDOLOCKACQUIRED = 14111;
        public const Int32 T_ENUMRUNNERBASEPSEUDOLOCKRLEASED = 14112;
        public const Int32 T_ENUMRUNNERBASEABORTCORE = 14113;
        public const Int32 T_ENUMRUNNERBASEQUEUEADDITIONCANCELED = 14114;
        public const Int32 T_ENUMRUNNERBASEGETAVAILABLE = 14120;
        public const Int32 T_ENUMRUNNERBASEGETAVAILABLEEXIT = 14121;
        public const Int32 T_ENUMRUNNERBASEGETREQUIRED = 14130;
        public const Int32 T_ENUMRUNNERBASEGETREQUIREDSTARTUPCOMPLETE = 14131;
        public const Int32 T_ENUMRUNNERBASEGETREQUIREDTRYSYNCPATH = 14132;
        public const Int32 T_ENUMRUNNERBASEGETREQUIREDSYNCEXIT = 14133;
        public const Int32 T_ENUMRUNNERBASEGETREQUIREDFETCHTASK = 14134;
        public const Int32 T_ENUMRUNNERBASEGETREQUIREDSTARTUPANDFETCHTASK = 14135;
        public const Int32 T_ENUMRUNNERBASEGETREQUIREDRETURNASYNC = 14136;
        public const Int32 T_ENUMRUNNERBASEFETCH = 14140;
        public const Int32 T_ENUMRUNNERBASEFETCHFINAL = 14141;
        public const Int32 T_ENUMRUNNERBASEFETCHSTASHEDALL = 14142;
        public const Int32 T_ENUMRUNNERBASEFETCHSTASHEDPART = 14143;
        public const Int32 T_ENUMRUNNERBASEFETCHQUEUE = 14144;
        public const Int32 T_ENUMRUNNERBASEFETCHEXIT = 14145;
        public const Int32 T_ENUMRUNNERBASEASYNCSTARTBKGSUCCESS = 14150;
        public const Int32 T_ENUMRUNNERBASEASYNCSTARTBKGENOUGHDATA = 14151;
        public const Int32 T_ENUMRUNNERBASEASYNCSTARTBKGINSUFFDATA = 14152;
        public const Int32 T_ENUMRUNNERBASEASYNCATTACHFETCHCONT = 14153;
        public const Int32 T_ENUMRUNNERBASEASYNCFETCHFAILED = 14154;
        public const Int32 T_ENUMRUNNERBASEASYNCSETFAILRESULT = 14155;
        public const Int32 T_ENUMRUNNERBASEASYNCFAILRESULTSET = 14156;
        public const Int32 T_ENUMRUNNERBASEASYNCFETCHCANCELED = 14157;
        public const Int32 T_ENUMRUNNERBASEASYNCSETCANCELRESULT = 14158;
        public const Int32 T_ENUMRUNNERBASEASYNCCANCELRESULTSET = 14159;
        public const Int32 T_ENUMRUNNERBASEASYNCFETCHSUCCESS = 14160;
        public const Int32 T_ENUMRUNNERBASEASYNCSUCCESSRESULTSET = 14161;
        public const Int32 T_ENUMRUNNERBASEASYNCSTASHORPHANNED = 14162;
        public const Int32 T_ENUMRUNNERBASERESULTTOMAKE = 14163;
        public const Int32 T_ENUMRUNNERBASERESULTMAKEFORASYNC = 14164;
        public const Int32 T_ENUMRUNNERBASERESULTSETFINALSTATUS = 14165;

        public const Int32 T_ENUMADAPTERRUNNER = 14200;
        public const Int32 T_ENUMADAPTERRUNNERCONSENTER = 14201;
        public const Int32 T_ENUMADAPTERRUNNERCONSEXIT = 14202;
        public const Int32 T_ENUMADAPTERRUNNERDISPOSECORE = 14203;
        public const Int32 T_ENUMADAPTERRUNNERPREDISPOSE = 14204;
        public const Int32 T_ENUMADAPTERRUNNERPREDISPOSEEXIT = 14205;
        public const Int32 T_ENUMADAPTERRUNNERRELEASESOURCE = 14206;
        public const Int32 T_ENUMADAPTERRUNNERSOURCEDISPOSED = 14207;
        public const Int32 T_ENUMADAPTERRUNNERSTARTBKGENTER = 14211;
        public const Int32 T_ENUMADAPTERRUNNERSTARTBKGEXIT = 14212;
        public const Int32 T_ENUMADAPTERRUNNERENUMSRCSTART = 14221;
        public const Int32 T_ENUMADAPTERRUNNERENUMSRCNEWITERATION = 14222;
        public const Int32 T_ENUMADAPTERRUNNERENUMSRCLOOPBREAK = 14223;
        public const Int32 T_ENUMADAPTERRUNNERENUMSRCITEMADDED = 14224;
        public const Int32 T_ENUMADAPTERRUNNERENUMSRCCANCELAFTERADD = 14225;
        public const Int32 T_ENUMADAPTERRUNNERENUMSRCLOOPENDED = 14226;
        public const Int32 T_ENUMADAPTERRUNNERENUMSRCFINALIZE = 14227;
        public const Int32 T_ENUMADAPTERRUNNERENUMSRCEXIT= 14228;
        public const Int32 T_ENUMADAPTERRUNNERAWAITSCHEDULE = 14231;
        public const Int32 T_ENUMADAPTERRUNNERAWAITSCHEDULELASTCHANCEQUEUE = 14232;
        public const Int32 T_ENUMADAPTERRUNNERAWAITSCHEDULEEXIT = 14233;
        public const Int32 T_ENUMADAPTERRUNNERAWAITQUEUE = 14234;
        public const Int32 T_ENUMADAPTERRUNNERAWAITQUEUEREALLY = 14235;
        public const Int32 T_ENUMADAPTERRUNNERAWAITQUEUEEXIT = 14236;
        public const Int32 T_ENUMADAPTERRUNNERFETCHREQUIREDENTER = 14241;
        public const Int32 T_ENUMADAPTERRUNNERFETCHREQUIREDLOOPSTART = 14242;
        public const Int32 T_ENUMADAPTERRUNNERFETCHREQUIREDLOOPNEXT = 14243;
        public const Int32 T_ENUMADAPTERRUNNERFETCHREQUIREDITEMTAKEN = 14244;
        public const Int32 T_ENUMADAPTERRUNNERFETCHREQUIREDNOMOREITEMS = 14245;
        public const Int32 T_ENUMADAPTERRUNNERFETCHREQUIREDBEFOREAWAITING = 14246;
        public const Int32 T_ENUMADAPTERRUNNERFETCHREQUIREDAFTERAWAITING = 14247;
        public const Int32 T_ENUMADAPTERRUNNERFETCHREQUIREDEXIT = 14248;
        public const Int32 T_ENUMADAPTERRUNNERDOABORT = 14251;

        public const Int32 T_ASYNCENUMADAPTERRUNNER = 14300;
        public const Int32 T_ASYNCENUMADAPTERRUNNERCONSENTER = 14301;
        public const Int32 T_ASYNCENUMADAPTERRUNNERCONSEXIT = 14302;
        public const Int32 T_ASYNCENUMADAPTERRUNNERDISPOSECORE = 14303;
        public const Int32 T_ASYNCENUMADAPTERRUNNERPREDISPOSE = 14304;
        public const Int32 T_ASYNCENUMADAPTERRUNNERPREDISPOSEEXIT = 14305;
        public const Int32 T_ASYNCENUMADAPTERRUNNERSOURCEDISPOSED = 14306;
        public const Int32 T_ASYNCENUMADAPTERRUNNERSTARTBKGENTER = 14311;
        public const Int32 T_ASYNCENUMADAPTERRUNNERSTARTBKGEXIT = 14312;
        public const Int32 T_ASYNCENUMADAPTERRUNNERENUMSRCSTEPCOMPLETE = 14321;
        public const Int32 T_ASYNCENUMADAPTERRUNNERENUMSRCSTEPCANCELED = 14322;
        public const Int32 T_ASYNCENUMADAPTERRUNNERENUMSRCCHAINBREAK = 14323;
        public const Int32 T_ASYNCENUMADAPTERRUNNERENUMSRCITEMADDED = 14324;
        public const Int32 T_ASYNCENUMADAPTERRUNNERENUMSRCMOVENEXT = 14325;
        public const Int32 T_ASYNCENUMADAPTERRUNNERENUMSRCDONE = 14326;
        public const Int32 T_ASYNCENUMADAPTERRUNNERENUMSRCSTEPENDED = 14327;
        public const Int32 T_ASYNCENUMADAPTERRUNNERTRYRELESECONTEXT = 14328;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHPENDING = 14331;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHCANCELED = 14332;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHCOPIED = 14333;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHCOMPLETE = 14334;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHCONTEXTRELESED = 14335;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHENTER = 14336;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHFAILASDISPOSED = 14337;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHSONTEXTSTORED = 14338;
        public const Int32 T_ASYNCENUMADAPTERRUNNERFETCHEXIT = 14339;
        public const Int32 T_SESSIONPROCESSSTARTBKGENTER = 14500;
        public const Int32 T_SESSIONPROCESSSTARTBKGTASK = 14501;
        public const Int32 T_SESSIONPROCESSSTARTBKGEXIT = 14502;
        public const Int32 T_SESSIONPROCESSPREDISPOSE = 14510;
        public const Int32 T_SESSIONPROCESSDODISPOSE = 14511;
        public const Int32 T_SESSIONPROCESSBKGTASKAWAITED = 14512;
        public const Int32 T_SESSIONPROCESSENTERDISPOSEASYNC = 14513;
        public const Int32 T_SESSIONPROCESSGETAVAILENTER = 14520;
        public const Int32 T_SESSIONPROCESSGETAVAILLOCKACQUIRED = 14521;
        public const Int32 T_SESSIONPROCESSGETAVAILALL = 14522;
        public const Int32 T_SESSIONPROCESSGETAVAILNOTALL = 14523;
        public const Int32 T_SESSIONPROCESSGETAVAILTRYSETSTATUS = 14524;
        public const Int32 T_SESSIONPROCESSGETAVAILSTATUSSET = 14525;
        public const Int32 T_SESSIONPROCESSGETAVAILLOCKRELEASED = 14526;
        public const Int32 T_SESSIONPROCESSGETREQASYNCENTER = 14530;
        public const Int32 T_SESSIONPROCESSGETREQASYNCLOCKACQUIRED = 14531;
        public const Int32 T_SESSIONPROCESSGETREQASYNCSYNCPATH = 14532;
        public const Int32 T_SESSIONPROCESSGETREQASYNCNOTALL = 14533;
        public const Int32 T_SESSIONPROCESSGETREQASYNCALL = 14534;
        public const Int32 T_SESSIONPROCESSGETREQASYNCTRYSETSTATUS = 14535;
        public const Int32 T_SESSIONPROCESSGETREQASYNCSTATUSSET = 14536;
        public const Int32 T_SESSIONPROCESSGETREQASYNCASYNCPATH = 14537;
        public const Int32 T_SESSIONPROCESSGETREQASYNCTASENQUEUED = 14538;
        public const Int32 T_SESSIONPROCESSGETREQASYNCLOCKRELEASED = 14539;
        public const Int32 T_SESSIONPROCESSPARAMSENTER = 14540;
        public const Int32 T_SESSIONPROCESSPARAMSADJUSTDEFAULT = 14541;
        public const Int32 T_SESSIONPROCESSPARAMSEXIT = 14542;
        public const Int32 T_SESSIONPROCESSBKGENDENTER = 14550;
        public const Int32 T_SESSIONPROCESSBKGENDLOCKACQUIRED = 14551;
        public const Int32 T_SESSIONPROCESSBKGENDRANTOCOMPLETION = 14552;
        public const Int32 T_SESSIONPROCESSBKGENDACCEPTRESULT = 14553;
        public const Int32 T_SESSIONPROCESSBKGENDFAULTED = 14554;
        public const Int32 T_SESSIONPROCESSBKGENDCANCELED = 14555;
        public const Int32 T_SESSIONPROCESSBKGENDPEDINGLOOP = 14556;
        public const Int32 T_SESSIONPROCESSBKGENDPROCESSAPENDING = 14557;
        public const Int32 T_SESSIONPROCESSBKGENDEXIT = 14558;
        public const Int32 T_SESSIONPROCESSPENDINGSETCANCELED = 14560;
        public const Int32 T_SESSIONPROCESSPENDINGSETEXCEPTION = 14561;
        public const Int32 T_SESSIONPROCESSPENDINGSETRESULT = 14562;
        public const Int32 T_SESSIONPROCESSPENDINGALREADYCANCELED = 14563;
        public const Int32 T_SESSIONPROCESSCALLBACKENTER = 14570;
        public const Int32 T_SESSIONPROCESSCALLBACKCANCELED = 14571;
        public const Int32 T_SESSIONPROCESSCALLBACKLOCKACQUIRED = 14572;
        public const Int32 T_SESSIONPROCESSCALLBACKPENDINGLOOP = 14573;
        public const Int32 T_SESSIONPROCESSCALLBACKPROCESSAPENDING = 14574;
        public const Int32 T_SESSIONPROCESSCALLBACKEXIT = 14575;

    }
}
