namespace Rates.Protocol.Unit_Tests
{
	using System;

	using FluentAssertions;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Moq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Utils.Rates.Common;
	using Skyline.DataMiner.Utils.Rates.Protocol;
	using Skyline.DataMiner.Utils.SNMP;
	using Skyline.DataMiner.Scripting;

	[TestClass]
	public class SnmpRate32Tests
	{
		private const int GroupId = 100;
		private const double FaultyReturn = -1;

		private readonly TimeSpan minDelta = new TimeSpan(0, 1, 0);
		private readonly TimeSpan maxDelta = new TimeSpan(1, 0, 0);

		#region Calculate

		[TestMethod]
		public void Calculate_Invalid_BackInTime()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.Calculate(deltaHelper, 10);  // This fist calculate is used to add some buffered data

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, -10));
			double rate = rateHelper.Calculate(deltaHelper, 20);

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Invalid_NoBufferedDataYet()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 2, 0));
			double rate = rateHelper.Calculate(deltaHelper, 20);

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Invalid_TooLate()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.Calculate(deltaHelper, 10);  // This fist calculate is used to add some buffered data

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(1, 1, 0));
			double rate = rateHelper.Calculate(deltaHelper, 20);

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Invalid_TooSoon()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 0));
			rateHelper.Calculate(deltaHelper, 10);  // This fist calculate is used to add some buffered data

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 5));
			double rate = rateHelper.Calculate(deltaHelper, 20);

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_ToOlderCounter()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.Calculate(deltaHelper, 0);    // Old counter
			rateHelper.Calculate(deltaHelper, 1);    // Old counter

			rateHelper.Calculate(deltaHelper, 5);     // Counter to be used

			// Recent counters
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 1, 30));
			rateHelper.Calculate(deltaHelper, 10);   // 1m30s

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 1));
			rateHelper.Calculate(deltaHelper, 20);    // 1m31s
			rateHelper.Calculate(deltaHelper, 30);    // 1m32s
			rateHelper.Calculate(deltaHelper, 40);    // 1m33s

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 7));
			double rate = rateHelper.Calculate(deltaHelper, 50);  // 1m40s

			// Assert
			double expectedRate = (50.0 - 5.0) / (7 + 1 + 1 + 1 + 90);
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_ToOlderCounter_WithTimeouts()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.BufferDelta(deltaHelper);
			rateHelper.Calculate(deltaHelper, 0);    // Old counter
			rateHelper.BufferDelta(deltaHelper);
			rateHelper.Calculate(deltaHelper, 1);    // Old counter

			rateHelper.BufferDelta(deltaHelper);
			rateHelper.Calculate(deltaHelper, 5);   // Counter to be used
			rateHelper.BufferDelta(deltaHelper);   // 10s

			// Recent counters
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 80));
			rateHelper.Calculate(deltaHelper, 10);   // 1m30s

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 1));
			rateHelper.Calculate(deltaHelper, 20);  // 1m31s
			rateHelper.BufferDelta(deltaHelper);    // 1m32s
			rateHelper.Calculate(deltaHelper, 30);  // 1m33s
			rateHelper.BufferDelta(deltaHelper);   // 1m34s
			rateHelper.Calculate(deltaHelper, 40);  // 1m35s
			rateHelper.BufferDelta(deltaHelper);   // 1m36s

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 4));
			double rate = rateHelper.Calculate(deltaHelper, 50);  // 1m40s

			// Assert
			double expectedRate = (50.0 - 5.0) / (4 + 1 + 1 + 1 + 1 + 1 + 1 + 80 + 10);
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_Accurate()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupAccurateDelta(GroupId, new object[] { new object[] { "PK_1", -1 }, new object[] { "PK_2", 10000 }, new object[] { "PK_3", -1 } });  // 10s
			rateHelper.Calculate(deltaHelper, 0, "PK_2");    // Old counters
			rateHelper.Calculate(deltaHelper, 1, "PK_2");    // Old counters

			rateHelper.Calculate(deltaHelper, 5, "PK_2");    // Counter to be used

			// Act
			deltaHelper = protocolMock.SetupAccurateDelta(GroupId, new object[] { new object[] { "PK_1", -1 }, new object[] { "PK_2", 100000 }, new object[] { "PK_3", -1 } }); // 100s
			double rate = rateHelper.Calculate(deltaHelper, 50, "PK_2");

			// Assert
			double expectedRate = (50.0 - 5.0) / 100d;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_Fast()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.Calculate(deltaHelper, 0);    // Old counters
			rateHelper.Calculate(deltaHelper, 1);    // Old counters

			rateHelper.Calculate(deltaHelper, 5);    // Counter to be used

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 100));
			double rate = rateHelper.Calculate(deltaHelper, 50);

			// Assert
			double expectedRate = (50.0 - 5.0) / 100d;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_PerDay()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta, RateBase.Day);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.Calculate(deltaHelper, 5);  // This fist calculate is used to add some buffered data

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 100));
			double rate = rateHelper.Calculate(deltaHelper, 50);

			// Assert
			double expectedRate = (50.0 - 5.0) / (100d / 60 / 60 / 24);
			////Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
			rate.Should().BeApproximately(expectedRate, Math.Pow(10, -9));
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_PerHour()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta, RateBase.Hour);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.Calculate(deltaHelper, 5);  // This fist calculate is used to add some buffered data

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 100));
			double rate = rateHelper.Calculate(deltaHelper, 50);

			// Assert
			double expectedRate = (50.0 - 5.0) / (100d / 60 / 60);
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_PerMinute()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta, RateBase.Minute);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.Calculate(deltaHelper, 5);  // This fist calculate is used to add some buffered data

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 100));
			double rate = rateHelper.Calculate(deltaHelper, 50);

			// Assert
			double expectedRate = (50.0 - 5.0) / (100d / 60);
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_WithTimeouts_Accurate()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupAccurateDelta(GroupId, new object[] { new object[] { "PK_1", -1 }, new object[] { "PK_2", 10000 }, new object[] { "PK_3", -1 } });  // 10s
			rateHelper.Calculate(deltaHelper, 0, "PK_2");    // Old counters
			rateHelper.Calculate(deltaHelper, 1, "PK_2");    // Old counters

			rateHelper.Calculate(deltaHelper, 5, "PK_2");    // Counter to be used

			deltaHelper = protocolMock.SetupAccurateTimeoutDelta(GroupId, new TimeSpan(0, 0, 5));  // 5s
			rateHelper.BufferDelta(deltaHelper, "PK_2");   // 5s
			rateHelper.BufferDelta(deltaHelper, "PK_2");   // 10s
			rateHelper.BufferDelta(deltaHelper, "PK_2");   // 15s
			rateHelper.BufferDelta(deltaHelper, "PK_2");   // 20s

			// Act
			deltaHelper = protocolMock.SetupAccurateDelta(GroupId, new object[] { new object[] { "PK_1", -1 }, new object[] { "PK_2", 100000 }, new object[] { "PK_3", -1 } });  // 100s
			double rate = rateHelper.Calculate(deltaHelper, 50, "PK_2");     // 120s

			// Assert
			double expectedRate = (50.0 - 5.0) / 120d;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_WithTimeouts_Fast()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper.Calculate(deltaHelper, 0);    // Old counters
			rateHelper.Calculate(deltaHelper, 1);    // Old counters

			rateHelper.Calculate(deltaHelper, 5);    // Counter to be used

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 5));
			rateHelper.BufferDelta(deltaHelper);   // 5s
			rateHelper.BufferDelta(deltaHelper);   // 10s
			rateHelper.BufferDelta(deltaHelper);   // 15s
			rateHelper.BufferDelta(deltaHelper);   // 20s

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 100));
			double rate = rateHelper.Calculate(deltaHelper, 50);     // 120s

			// Assert
			double expectedRate = (50.0 - 5.0) / 120d;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		[TestMethod]
		public void Calculate_Valid_WithOverflow()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 0));
			rateHelper.Calculate(deltaHelper, UInt32.MaxValue - 10);  // This fist calculate is used to add some buffered data

			// Act
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 100));
			double rate = rateHelper.Calculate(deltaHelper, 9);

			// Assert
			double expectedRate = 20 / 100d;
			Assert.IsTrue(rate == expectedRate, "rate '" + rate + "' != expectedRate '" + expectedRate + "'");
		}

		#endregion

		#region SerializeTests

		[TestMethod]
		public void FromJsonString_Invalid_UnexpectedString()
		{
			// Arrange
			string serialized = "1234_HoedjeVanPapier";

			// Act & Assert
			Assert.Throws<JsonReaderException>(() => SnmpRate32.FromJsonString(serialized, minDelta, maxDelta));
		}

		[TestMethod]
		public void FromJsonString_UpdatedProperties()
		{
			// Arrange
			var minDelta1 = new TimeSpan(0, 1, 0);
			var maxDelta1 = new TimeSpan(1, 0, 0);
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper1 = SnmpRate32.FromJsonString("", minDelta1, maxDelta1);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper1.Calculate(deltaHelper, 5);

			// Act
			var minDelta2 = new TimeSpan(0, 2, 0);
			var maxDelta2 = new TimeSpan(2, 0, 0);
			string serializedTemp = rateHelper1.ToJsonString();
			var rateHelper2 = SnmpRate32.FromJsonString(serializedTemp, minDelta2, maxDelta2);

			// Assert
			Assert.IsTrue(rateHelper2.rateOnTimes.minDelta == minDelta2);
			Assert.IsTrue(rateHelper2.rateOnTimes.maxDelta == maxDelta2);
		}

		[TestMethod]
		public void Serialize_Invalid_DifferentCounter()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper1 = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper1.Calculate(deltaHelper, 5);
			rateHelper1.Calculate(deltaHelper, 10);

			string serializedTemp = rateHelper1.ToJsonString();
			var rateHelper2 = SnmpRate32.FromJsonString(serializedTemp, minDelta, maxDelta);

			// Different counter, same timing
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 9));
			rateHelper1.Calculate(deltaHelper, 20);
			rateHelper2.Calculate(deltaHelper, 21);

			// Act
			string serialized1 = rateHelper1.ToJsonString();
			string serialized2 = rateHelper2.ToJsonString();

			// Assert
			serialized1.Should().NotBeEquivalentTo(serialized2);
		}

		[TestMethod]
		public void Serialize_Invalid_DifferentTimeSpan()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper1 = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper1.Calculate(deltaHelper, 5);
			rateHelper1.Calculate(deltaHelper, 10);

			string serializedTemp = rateHelper1.ToJsonString();
			var rateHelper2 = SnmpRate32.FromJsonString(serializedTemp, minDelta, maxDelta);

			// Same counter, different timing
			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 9));
			rateHelper1.Calculate(deltaHelper, 20);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 2));
			rateHelper2.Calculate(deltaHelper, 20);

			// Act
			string serialized1 = rateHelper1.ToJsonString();
			string serialized2 = rateHelper2.ToJsonString();

			// Assert
			serialized1.Should().NotBeEquivalentTo(serialized2);
		}

		[TestMethod]
		public void Serialize_Valid()
		{
			// Arrange
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();
			var rateHelper1 = SnmpRate32.FromJsonString("", minDelta, maxDelta);

			deltaHelper = protocolMock.SetupFastDelta(GroupId, new TimeSpan(0, 0, 10));
			rateHelper1.Calculate(deltaHelper, 5);
			rateHelper1.Calculate(deltaHelper, 10);

			string serializedTemp = rateHelper1.ToJsonString();
			var rateHelper2 = SnmpRate32.FromJsonString(serializedTemp, minDelta, maxDelta);

			AddSameToBoth(rateHelper1, rateHelper2, 20, new TimeSpan(0, 0, 9));
			AddSameToBoth(rateHelper1, rateHelper2, 30, new TimeSpan(0, 0, 8));

			// Act
			string serialized1 = rateHelper1.ToJsonString();
			string serialized2 = rateHelper2.ToJsonString();

			// Assert
			serialized1.Should().BeEquivalentTo(serialized2);
		}

		#endregion

		#region HelperMethods
		private static void AddSameToBoth(SnmpRate32 helper1, SnmpRate32 helper2, uint newCounter, TimeSpan time)
		{
			SnmpDeltaHelper deltaHelper;
			var protocolMock = new Mock<SLProtocol>();

			deltaHelper = protocolMock.SetupFastDelta(GroupId, time);
			helper1.Calculate(deltaHelper, newCounter);
			helper2.Calculate(deltaHelper, newCounter);
		}
		#endregion
	}
}