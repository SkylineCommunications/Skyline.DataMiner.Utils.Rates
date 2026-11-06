# Skyline.DataMiner.Utils.Rates

## About

Skyline.DataMiner.Utils.Rates are several packages available in the public [nuget store](https://www.nuget.org/) that contain assemblies that assist in the calculation of rates (e.g. bitrates).

These allow the developer to accurately calculate rates from counters commonly provided by devices or services.

> Documentation: [Rate Calculations](https://community.dataminer.services/the-many-pitfalls-of-rate-calculations-and-how-you-can-avoid-them/?hilite=rate)

The following packages are available that concern rate calculations:

- Skyline.DataMiner.Utils.Interfaces
- Skyline.DataMiner.Utils.Rates.Protocol
- Skyline.DataMiner.Utils.Rates.Common

## Requirements

The "DataMiner Integration Studio" Visual Studio extension is required for development of connectors and Automation scripts using NuGets.

See [Installing DataMiner Integration Studio](https://aka.dataminer.services/DisInstallation)

> [!IMPORTANT]
> NuGets are mandatory to be installed with PackageReferences. DIS was redesigned to work with PackageReferences and be future-proof.
>
> For more information on how to migrate from packages.config to PackageReferences, see [docs.microsoft.com](https://docs.microsoft.com/en-us/nuget/consume-packages/migrate-packages-config-to-package-reference).

## Getting started

Beginning from counters received from SNMP through the Protocol XML:

```cs
        // Build SNMP Delta Helper
        SnmpDeltaHelper snmpDeltaHelper = new SnmpDeltaHelper(protocol, GroupId);

        // Get buffered data and make a helper instance from it
        string bufferedData = Convert.ToString(protocol.GetParameter(Parameter.counterratedata));
        SnmpRate32 snmpRateHelper = SnmpRate32.FromJsonString(bufferedData, minDelta, maxDelta);

        // Calculate rate
        uint counter = GetNewCounterFromSnmpParam(protocol);
        double rate = snmpRateHelper.Calculate(snmpDeltaHelper, counter);

        // Save results and buffered data
        Dictionary<int, object> paramsToSet = new Dictionary<int, object>
        {
            { Parameter.counter, counter },
            { Parameter.counterrate, rate },
            { Parameter.counterratedata, snmpRateHelper.ToJsonString() },
        };

        protocol.SetParameters(paramsToSet.Keys.ToArray(), paramsToSet.Values.ToArray());
```

Beginning from counters received through other means:

```cs
        // Get buffered data and make a helper instance from it
        string bufferedData = Convert.ToString(protocol.GetParameter(Parameter.counterrateondatesdata)); 
        Rate32OnDateTime rate32OnDatesHelper = Rate32OnDateTime.FromJsonString(bufferedData, minDelta, maxDelta); 

        // Calculate rate
        uint counter = GetNewCounterFromDataSource(protocol);
        double rate = rate32OnDatesHelper.Calculate(counter, DateTime.Now);

        // Save results and buffered data
        Dictionary<int, object> paramsToSet = new Dictionary<int, object>
        {
            { Parameter.counter, counter },
            { Parameter.counterrateondates, rate },
            { Parameter.counterrateondatesdata, rate32OnDatesHelper.ToJsonString() },
        };

        protocol.SetParameters(paramsToSet.Keys.ToArray(), paramsToSet.Values.ToArray());
```
