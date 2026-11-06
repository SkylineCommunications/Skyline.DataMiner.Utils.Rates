namespace Skyline.DataMiner.Utils.Rates.Common.Tests
{
	using System;

	using FluentAssertions;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Utils.Rates.Common;

	[TestClass]
	public class Rate64OnDateTimeTests
	{
		private const double FaultyReturn = -1;

		private readonly TimeSpan minDelta = new TimeSpan(0, 1, 0);
		private readonly TimeSpan maxDelta = new TimeSpan(1, 0, 0);

		#region Calculate

		[TestMethod]
		public void Calculate_Invalid_BackInTime()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			helper.Calculate(10, start);    // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(20, start.AddSeconds(-10));

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Invalid_NoBufferedDataYet()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			// Act
			double rate = helper.Calculate(20, start);

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Invalid_TooLate()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			helper.Calculate(10, start);    // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(20, start.AddHours(2));

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Invalid_TooSoon()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			helper.Calculate(10, start);    // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(20, start.AddSeconds(5));

			// Assert
			double expectedRate = FaultyReturn;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_DuplicateCounters()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			// Old counters
			helper.Calculate(0, start.AddSeconds(-200));
			helper.Calculate(1, start.AddSeconds(-100));

			// Counter to be used
			helper.Calculate(5, start);
			helper.Calculate(5, start);     // Duplicate counters should have no impact.

			// Act
			helper.Calculate(50, start.AddSeconds(100));
			helper.Calculate(50, start.AddSeconds(100));     // Duplicate counters should have no impact.
			double rate = helper.Calculate(50, start.AddSeconds(100));

			// Assert
			double expectedRate = (50.0 - 5.0) / 100d;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_DuplicateDateTimes()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			// Old counters
			helper.Calculate(0, start.AddSeconds(-200));
			helper.Calculate(1, start.AddSeconds(-100));

			// Counter to be used
			helper.Calculate(5, start);

			/* Design choice:
			 * currently, this one will be ignored as an entry with same DateTime already exists.
			 * We consider receiving two counters with different value for same DateTime is invalid use-case anyway
			 * so we didn't further invest in updating counter for that DateTime.
			 */
			helper.Calculate(40, start);

			// Act
			double rate = helper.Calculate(50, start.AddSeconds(100));

			// Assert
			double expectedRate = (50.0 - 5) / 100d;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_Negative()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			// Old counters
			helper.Calculate(0, start.AddSeconds(-200));
			helper.Calculate(1, start.AddSeconds(-100));

			helper.Calculate(100, start);     // Counter to be used

			// Act
			double rate = helper.Calculate(50, start.AddSeconds(100));

			// Assert
			double expectedRate = -50d / 100d;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_ToOlderCounter()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			// Old counters
			helper.Calculate(0, start.AddSeconds(-200));
			helper.Calculate(1, start.AddSeconds(-100));

			helper.Calculate(5, start);     // Counter to be used

			// Recent counters
			helper.Calculate(10, start.AddSeconds(90));
			helper.Calculate(20, start.AddSeconds(91));
			helper.Calculate(30, start.AddSeconds(92));
			helper.Calculate(40, start.AddSeconds(93));

			// Act
			double rate = helper.Calculate(50, start.AddSeconds(100));

			// Assert
			double expectedRate = (50.0 - 5.0) / 100d;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_ToPreviousCounter()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			// Old counters
			helper.Calculate(0, start.AddSeconds(-200));
			helper.Calculate(1, start.AddSeconds(-100));

			helper.Calculate(5, start);     // Counter to be used

			// Act
			double rate = helper.Calculate(50, start.AddSeconds(100));

			// Assert
			double expectedRate = (50.0 - 5.0) / 100d;
			Assert.IsTrue(rate == expectedRate, $"rate '{rate}' != expectedRate '{expectedRate}'");
		}

		[TestMethod]
		public void Calculate_Valid_WithOverflow()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);

			helper.Calculate(UInt64.MaxValue - 10, start);  // Need to first buffer some counter(s)

			// Act
			double rate = helper.Calculate(9, start.AddSeconds(100));

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
			Assert.Throws<JsonReaderException>(() => Rate64OnDateTime.FromJsonString(serialized, minDelta, maxDelta));
		}

		[TestMethod]
		public void FromJSonString_UpdatedProperties()
		{
			// Arrange
			var minDelta1 = new TimeSpan(0, 1, 0);
			var maxDelta1 = new TimeSpan(1, 0, 0);
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper1 = Rate64OnDateTime.FromJsonString("", minDelta1, maxDelta1);
			helper1.Calculate(5, start);

			// Act
			var minDelta2 = new TimeSpan(0, 2, 0);
			var maxDelta2 = new TimeSpan(2, 0, 0);
			string serializedTemp = helper1.ToJsonString();
			var helper2 = Rate64OnDateTime.FromJsonString(serializedTemp, minDelta2, maxDelta2);

			// Assert
			Assert.IsTrue(helper2.minDelta == minDelta2);
			Assert.IsTrue(helper2.maxDelta == maxDelta2);
		}

		[TestMethod]
		public void Serialize_Invalid_DifferentCounter()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper1 = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);
			helper1.Calculate(5, start);
			helper1.Calculate(10, start.AddSeconds(10));

			string serializedTemp = helper1.ToJsonString();
			var helper2 = Rate64OnDateTime.FromJsonString(serializedTemp, minDelta, maxDelta);

			// Different counter, same timing
			helper1.Calculate(20, start.AddSeconds(19));
			helper2.Calculate(21, start.AddSeconds(19));

			// Act
			string serialized1 = helper1.ToJsonString();
			string serialized2 = helper2.ToJsonString();

			// Assert
			serialized1.Should().NotBeEquivalentTo(serialized2);
		}

		[TestMethod]
		public void Serialize_Invalid_DifferentDateTime()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper1 = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);
			helper1.Calculate(5, start);
			helper1.Calculate(10, start.AddSeconds(10));

			string serializedTemp = helper1.ToJsonString();
			var helper2 = Rate64OnDateTime.FromJsonString(serializedTemp, minDelta, maxDelta);

			// same counter, different timing
			helper1.Calculate(20, start.AddSeconds(19));
			helper2.Calculate(20, start.AddSeconds(21));

			// Act
			string serialized1 = helper1.ToJsonString();
			string serialized2 = helper2.ToJsonString();

			// Assert
			serialized1.Should().NotBeEquivalentTo(serialized2);
		}

		[TestMethod]
		public void Serialize_Valid_DateTime()
		{
			// Arrange
			DateTime start = DateTime.SpecifyKind(new DateTime(2000, 1, 1), DateTimeKind.Utc);
			var helper1 = Rate64OnDateTime.FromJsonString("", minDelta, maxDelta);
			helper1.Calculate(5, start);
			helper1.Calculate(10, start.AddSeconds(10));

			string serializedTemp = helper1.ToJsonString();
			var helper2 = Rate64OnDateTime.FromJsonString(serializedTemp, minDelta, maxDelta);

			AddSameToBoth(helper1, helper2, 20, start.AddSeconds(19));
			AddSameToBoth(helper1, helper2, 30, start.AddSeconds(27));

			// Act
			string serialized1 = helper1.ToJsonString();
			string serialized2 = helper2.ToJsonString();

			// Assert
			serialized1.Should().BeEquivalentTo(serialized2);
		}

		#endregion

		#region HelperMethods
		private static void AddSameToBoth(Rate64OnDateTime helper1, Rate64OnDateTime helper2, ulong newCounter, DateTime time)
		{
			helper1.Calculate(newCounter, time);
			helper2.Calculate(newCounter, time);
		}
		#endregion
	}
}