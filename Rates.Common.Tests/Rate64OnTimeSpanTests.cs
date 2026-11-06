namespace Skyline.DataMiner.Utils.Rates.Common.Tests
{
	using System;

	using FluentAssertions;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Utils.Rates.Common;

	[TestClass]
	public class Rate64OnTimeSpanTests
	{
		private const double FaultyReturn = -1;

		private readonly TimeSpan minDelta = new TimeSpan(0, 1, 0);
		private readonly TimeSpan maxDelta = new TimeSpan(1, 0, 0);

		#region Calculate

		[TestMethod]
		public void Calculate_Invalid_BackInTime()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);

			helper.Calculate(10, new TimeSpan(0, 0, 10));   // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(20, new TimeSpan(0, 0, -10));

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Invalid_NoBufferedDataYet()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);

			// Act
			double rate = helper.Calculate(20, new TimeSpan(0, 2, 0));

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Invalid_TooLate()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);

			helper.Calculate(10, new TimeSpan(0, 0, 10));   // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(20, new TimeSpan(1, 1, 0));

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Invalid_TooSoon()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);

			helper.Calculate(10, new TimeSpan(0, 0, 0));    // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(20, new TimeSpan(0, 0, 5));

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_ToOlderCounter()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);

			// Old counters
			helper.Calculate(0, new TimeSpan(0, 0, 10));
			helper.Calculate(1, new TimeSpan(0, 0, 10));

			helper.Calculate(5, new TimeSpan(0, 0, 10));     // Counter to be used

			// Recent counters
			helper.Calculate(10, new TimeSpan(0, 0, 90));   // 1m30s
			helper.Calculate(20, new TimeSpan(0, 0, 1));    // 1m31s
			helper.Calculate(30, new TimeSpan(0, 0, 1));    // 1m32s
			helper.Calculate(40, new TimeSpan(0, 0, 1));    // 1m33s

			// Act
			double rate = helper.Calculate(50, new TimeSpan(0, 0, 7));  // 1m40s

			// Assert
			double expectedRate = (50.0 - 5.0) / (7 + 1 + 1 + 1 + 90);
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);

			// Old counters
			helper.Calculate(0, new TimeSpan(0, 0, 10));
			helper.Calculate(1, new TimeSpan(0, 0, 10));

			helper.Calculate(5, new TimeSpan(0, 0, 10));     // Counter to be used

			// Act
			double rate = helper.Calculate(50, new TimeSpan(0, 0, 100));

			// Assert
			double expectedRate = (50.0 - 5.0) / 100d;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_PerDay()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta, RateBase.Day);

			helper.Calculate(5, new TimeSpan(0, 0, 10));    // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(50, new TimeSpan(0, 0, 100));

			// Assert
			ulong counterIncrease = 50 - 5;
			double expectedRate = counterIncrease / (100d / 60 / 60 / 24);
			////Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
			rate.Should().BeApproximately(expectedRate, Math.Pow(10, -9));
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_PerHour()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta, RateBase.Hour);

			helper.Calculate(5, new TimeSpan(0, 0, 10));    // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(50, new TimeSpan(0, 0, 100));

			// Assert
			double expectedRate = (50.0 - 5.0) / (100d / 60 / 60);
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter_PerMinute()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta, RateBase.Minute);

			helper.Calculate(5, new TimeSpan(0, 0, 10));    // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(50, new TimeSpan(0, 0, 100));

			// Assert
			double expectedRate = (50.0 - 5.0) / (100d / 60);
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_WithOverflow()
		{
			// Arrange
			var helper = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);

			helper.Calculate(UInt64.MaxValue - 10, new TimeSpan(0, 0, 0));  // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(9, new TimeSpan(0, 0, 100));

			// Assert
			double expectedRate = 20 / 100d;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		#endregion

		#region SerializeTests

		[TestMethod]
		public void FromJSonString_Invalid_UnexpectedString()
		{
			// Arrange
			string serialized = "1234_HoedjeVanPapier";

			// Act & Assert
			Assert.Throws<JsonReaderException>(() => Rate64OnTimeSpan.FromJsonString(serialized, minDelta, maxDelta));
		}

		[TestMethod]
		public void FromJSonString_UpdatedProperties()
		{
			// Arrange
			var minDelta1 = new TimeSpan(0, 1, 0);
			var maxDelta1 = new TimeSpan(1, 0, 0);
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper1 = Rate64OnTimeSpan.FromJsonString("", minDelta1, maxDelta1);
			helper1.Calculate(5, new TimeSpan(0, 0, 10));

			// Act
			var minDelta2 = new TimeSpan(0, 2, 0);
			var maxDelta2 = new TimeSpan(2, 0, 0);
			string serializedTemp = helper1.ToJsonString();
			var helper2 = Rate64OnTimeSpan.FromJsonString(serializedTemp, minDelta2, maxDelta2);

			// Assert
			Assert.IsTrue(helper2.minDelta == minDelta2);
			Assert.IsTrue(helper2.maxDelta == maxDelta2);
		}

		[TestMethod]
		public void Serialize_Invalid_DifferentCounter()
		{
			// Arrange
			var helper1 = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);
			helper1.Calculate(5, new TimeSpan(0, 0, 0));
			helper1.Calculate(10, new TimeSpan(0, 0, 10));

			string serializedTemp = helper1.ToJsonString();
			var helper2 = Rate64OnTimeSpan.FromJsonString(serializedTemp, minDelta, maxDelta);

			// Different counter, same timing
			helper1.Calculate(20, new TimeSpan(0, 0, 9));
			helper2.Calculate(21, new TimeSpan(0, 0, 9));

			// Act
			string serialized1 = helper1.ToJsonString();
			string serialized2 = helper2.ToJsonString();

			// Assert
			serialized1.Should().NotBeEquivalentTo(serialized2);
		}

		[TestMethod]
		public void Serialize_Invalid_DifferentTimeSpan()
		{
			// Arrange
			var helper1 = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);
			helper1.Calculate(5, new TimeSpan(0, 0, 0));
			helper1.Calculate(10, new TimeSpan(0, 0, 10));

			string serializedTemp = helper1.ToJsonString();
			var helper2 = Rate64OnTimeSpan.FromJsonString(serializedTemp, minDelta, maxDelta);

			// same counter, different timing
			helper1.Calculate(20, new TimeSpan(0, 0, 9));
			helper2.Calculate(20, new TimeSpan(0, 0, 2));

			// Act
			string serialized1 = helper1.ToJsonString();
			string serialized2 = helper2.ToJsonString();

			// Assert
			serialized1.Should().NotBeEquivalentTo(serialized2);
		}

		[TestMethod]
		public void Serialize_Valid_TimeSpan()
		{
			// Arrange
			var helper1 = Rate64OnTimeSpan.FromJsonString("", minDelta, maxDelta);
			helper1.Calculate(5, new TimeSpan(0, 0, 0));
			helper1.Calculate(10, new TimeSpan(0, 0, 10));

			string serializedTemp = helper1.ToJsonString();
			var helper2 = Rate64OnTimeSpan.FromJsonString(serializedTemp, minDelta, maxDelta);

			AddSameToBoth(helper1, helper2, 20, new TimeSpan(0, 0, 9));
			AddSameToBoth(helper1, helper2, 30, new TimeSpan(0, 0, 8));

			// Act
			string serialized1 = helper1.ToJsonString();
			string serialized2 = helper2.ToJsonString();

			// Assert
			serialized1.Should().BeEquivalentTo(serialized2);
		}

		#endregion

		#region HelperMethods
		private static void AddSameToBoth(Rate64OnTimeSpan helper1, Rate64OnTimeSpan helper2, ulong newCounter, TimeSpan time)
		{
			helper1.Calculate(newCounter, time);
			helper2.Calculate(newCounter, time);
		}
		#endregion
	}
}