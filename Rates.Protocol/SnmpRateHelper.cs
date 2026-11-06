namespace Skyline.DataMiner.Utils.Rates.Protocol
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Utils.Rates.Common;
	using Skyline.DataMiner.Utils.SNMP;

	/// <summary>
	/// Class <see cref="SnmpRate{T, U}"/> helps calculating rates of all sorts (bit rates, counter rates, etc) based on counters polled over SNMP.
	/// This class is meant to be used as base class for more specific SnmpRate helpers depending on the range of counters (<see cref="uint"/>, <see cref="ulong"/>, etc).
	/// </summary>
	public class SnmpRate<T, U> where U : CounterWithTimeSpan<T>
	{
		[JsonProperty]
		private protected TimeSpan bufferedDelta;

		[JsonProperty]
		internal protected RateOnTimeSpan<T, U> rateOnTimes;

		[JsonConstructor]
		private protected SnmpRate()
		{
			bufferedDelta = TimeSpan.Zero;
		}

		/// <summary>
		/// Used to buffer the delta (TimeSpan) between 2 executions of the same group whenever the group execution times-out.
		/// </summary>
		/// <param name="deltaHelper">An instance of the <see cref="SnmpDeltaHelper"/> class which will take care of fetching the delta from DataMiner.</param>
		/// <param name="rowKey">Optional argument to be used when buffering delta for an SNMP table.<br/>
		/// In that case, the primary key of the table row is to be provided.</param>
		public void BufferDelta(SnmpDeltaHelper deltaHelper, string rowKey = null)
		{
			var delta = deltaHelper.GetDelta(rowKey);
			if (!delta.HasValue)
			{
				return;
			}

			bufferedDelta += delta.Value;
		}

		/// <summary>
		/// Serializes the currently buffered data of this <see cref="SnmpRate{T, U}"/> instance.
		/// </summary>
		/// <returns>A JSON string containing the serialized data of this <see cref="SnmpRate{T, U}"/> instance.</returns>
		public string ToJsonString()
		{
			return JsonConvert.SerializeObject(this);
		}

		private protected double Calculate(SnmpDeltaHelper deltaHelper, U newCounter, string rowKey = null, double faultyReturn = -1)
		{
			var delta = deltaHelper.GetDelta(rowKey);
			if (!delta.HasValue)
			{
				return faultyReturn;
			}

			newCounter.TimeSpan = bufferedDelta + delta.Value;
			var rate = rateOnTimes.Calculate(newCounter, faultyReturn);
			bufferedDelta = TimeSpan.Zero;

			return rate;
		}
	}

	/// <summary>
	/// Allows calculating rates of all sorts (bit rates, counter rates, etc) based on <see cref="uint"/> counters polled over SNMP.
	/// </summary>
	public class SnmpRate32 : SnmpRate<uint, Counter32WithTimeSpan>
	{
		[JsonConstructor]
		private SnmpRate32(TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase)
		{
			rateOnTimes = new Rate32OnTimeSpan(minDelta, maxDelta, rateBase);
		}

		/// <summary>
		/// Deserializes a JSON <see cref="string"/> to a <see cref="SnmpRate32"/> instance.<br/>
		/// Throws a <see cref="JsonReaderException"/> if the given <paramref name="rateHelperSerialized"/> is an invalid string representation of a <see cref="SnmpRate32"/> instance.
		/// </summary>
		/// <param name="rateHelperSerialized">Serialized <see cref="SnmpRate32"/> instance.</param>
		/// <param name="minDelta">Minimum <see cref="TimeSpan"/> necessary between 2 counters when calculating a rate.<br/>
		/// Counters will be buffered until this minimum delta is met.</param>
		/// <param name="maxDelta">Maximum <see cref="TimeSpan"/> allowed between 2 counters when calculating a rate.</param>
		/// <param name="rateBase">Choose whether the rate should be calculated per second, minute, hour or day.</param>
		/// <returns>If the <paramref name="rateHelperSerialized"/> is valid, a new instance of the <see cref="SnmpRate32"/> class with all data found in <paramref name="rateHelperSerialized"/>.<br/>
		/// Otherwise, throws a <see cref="JsonReaderException"/>.</returns>
		public static SnmpRate32 FromJsonString(string rateHelperSerialized, TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase = RateBase.Second)
		{
			Rate32OnTimeSpan.ValidateMinAndMaxDeltas(minDelta, maxDelta);

			SnmpRate32 helper;
			if (String.IsNullOrWhiteSpace(rateHelperSerialized))
			{
				helper = new SnmpRate32(minDelta, maxDelta, rateBase);
			}
			else
			{
				helper = JsonConvert.DeserializeObject<SnmpRate32>(rateHelperSerialized);
				helper.rateOnTimes.minDelta = minDelta;
				helper.rateOnTimes.maxDelta = maxDelta;
				helper.rateOnTimes.RateBase = rateBase;
			}

			return helper;
		}

		/// <summary>
		/// Calculates the rate using provided <paramref name="newCounter"/> against previous counters buffered in this <see cref="SnmpRate32"/> instance.
		/// </summary>
		/// <param name="deltaHelper">An instance of the <see cref="SnmpDeltaHelper"/> class which will take care of fetching the delta from DataMiner.</param>
		/// <param name="newCounter">The latest known counter value.</param>
		/// <param name="rowKey">The primary key of the table row for which a rate should be calculated.</param>
		/// <param name="faultyReturn">The value to be returned in case a correct rate could not be calculated.</param>
		/// <returns>The calculated rate or the value specified in <paramref name="faultyReturn"/> in case the rate can not be calculated.</returns>
		public double Calculate(SnmpDeltaHelper deltaHelper, uint newCounter, string rowKey = null, double faultyReturn = -1)
		{
			var rateCounter = new Counter32WithTimeSpan(newCounter, TimeSpan.Zero);
			return Calculate(deltaHelper, rateCounter, rowKey, faultyReturn);
		}
	}

	/// <summary>
	/// Allows calculating rates of all sorts (bit rates, counter rates, etc) based on <see cref="ulong"/> counters polled over SNMP.
	/// </summary>
	public class SnmpRate64 : SnmpRate<ulong, Counter64WithTimeSpan>
	{
		[JsonConstructor]
		private SnmpRate64(TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase)
		{
			rateOnTimes = new Rate64OnTimeSpan(minDelta, maxDelta, rateBase);
		}

		/// <summary>
		/// Deserializes a JSON <see cref="string"/> to a <see cref="SnmpRate64"/> instance.<br/>
		/// Throws a <see cref="JsonReaderException"/> if the given <paramref name="rateHelperSerialized"/> is an invalid string representation of a <see cref="SnmpRate64"/> instance.
		/// </summary>
		/// <param name="rateHelperSerialized">Serialized <see cref="SnmpRate64"/> instance.</param>
		/// <param name="minDelta">Minimum <see cref="TimeSpan"/> necessary between 2 counters when calculating a rate.<br/>
		/// Counters will be buffered until this minimum delta is met.</param>
		/// <param name="maxDelta">Maximum <see cref="TimeSpan"/> allowed between 2 counters when calculating a rate.</param>
		/// <param name="rateBase">Choose whether the rate should be calculated per second, minute, hour or day.</param>
		/// <returns>If the <paramref name="rateHelperSerialized"/> is valid, a new instance of the <see cref="SnmpRate64"/> class with all data found in <paramref name="rateHelperSerialized"/>.<br/>
		/// Otherwise, throws a <see cref="JsonReaderException"/>.</returns>
		public static SnmpRate64 FromJsonString(string rateHelperSerialized, TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase = RateBase.Second)
		{
			Rate64OnTimeSpan.ValidateMinAndMaxDeltas(minDelta, maxDelta);

			SnmpRate64 helper;
			if (String.IsNullOrWhiteSpace(rateHelperSerialized))
			{
				helper = new SnmpRate64(minDelta, maxDelta, rateBase);
			}
			else
			{
				helper = JsonConvert.DeserializeObject<SnmpRate64>(rateHelperSerialized);
				helper.rateOnTimes.minDelta = minDelta;
				helper.rateOnTimes.maxDelta = maxDelta;
				helper.rateOnTimes.RateBase = rateBase;
			}

			return helper;
		}

		/// <summary>
		/// Calculates the rate using provided <paramref name="newCounter"/> against previous counters buffered in this <see cref="SnmpRate64"/> instance.
		/// </summary>
		/// <param name="deltaHelper">An instance of the <see cref="SnmpDeltaHelper"/> class which will take care of fetching the delta from DataMiner.</param>
		/// <param name="newCounter">The latest known counter value.</param>
		/// <param name="rowKey">The primary key of the table row for which a rate should be calculated.</param>
		/// <param name="faultyReturn">The value to be returned in case a correct rate could not be calculated.</param>
		/// <returns>The calculated rate or the value specified in <paramref name="faultyReturn"/> in case the rate can not be calculated.</returns>
		public double Calculate(SnmpDeltaHelper deltaHelper, ulong newCounter, string rowKey = null, double faultyReturn = -1)
		{
			var rateCounter = new Counter64WithTimeSpan(newCounter, TimeSpan.Zero);
			return Calculate(deltaHelper, rateCounter, rowKey, faultyReturn);
		}
	}
}
