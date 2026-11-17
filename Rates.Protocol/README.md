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

## Negative rates

A negative rate is not necessarily wrong: for some counters a decreasing value is perfectly legitimate.
For others it is unexpected and undesired, for example because the device reset the counter or because of a device bug.
By default, negative rates are returned as calculated.

Whether to use the optional `overrideNegativeRatesWith` argument depends on the use case: on whether a negative rate is legitimate for the counter at hand, and on whether a counter reset can be detected by other means.
If an unexpected and undesired negative rate must not be returned, pass it to `FromJsonString`:

```cs
        // Returns -1 (instead of the negative rate) when an unexpected and undesired negative rate is calculated
        SnmpRate32 snmpRateHelper = SnmpRate32.FromJsonString(bufferedData, minDelta, maxDelta, RateBase.Second, overrideNegativeRatesWith: -1);
```

Choose an exceptional value (or `protocol.Leave` in a protocol) rather than `0`, as `0` is a proper rate value.

When a negative rate is detected, the buffered counters are discarded, the new counter becomes the first buffered counter and the given value is returned.
The default (`null`) keeps returning negative rates. Counter wrap-arounds are still handled as before and never result in a negative rate.
The option is not serialized, so it must be provided each time `FromJsonString` is called.