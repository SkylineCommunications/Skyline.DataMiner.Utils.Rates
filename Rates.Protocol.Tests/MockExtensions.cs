namespace Rates.Protocol.Unit_Tests
{
	using System;

	using Moq;

	using Skyline.DataMiner.Utils.Rates.Protocol;
	using Skyline.DataMiner.Utils.SNMP;
	using Skyline.DataMiner.Scripting;

	internal static class MockExtensions
	{
		internal static SnmpDeltaHelper SetupFastDelta(this Mock<SLProtocol> protocolMock, int groupId, TimeSpan delta)
		{
			SnmpDeltaHelper deltaHelper = new SnmpDeltaHelper(protocolMock.Object, groupId);
			protocolMock.Setup(p => p.NotifyProtocol(269, groupId, null)).Returns((int)delta.TotalMilliseconds);
			return deltaHelper;
		}

		internal static SnmpDeltaHelper SetupAccurateDelta(this Mock<SLProtocol> protocolMock, int groupId, object[] deltaValues, uint calculationMethodPid = 100)
		{
			protocolMock.Setup(p => p.GetParameter((int)calculationMethodPid)).Returns((int)CalculationMethod.Accurate);

			SnmpDeltaHelper deltaHelper = new SnmpDeltaHelper(protocolMock.Object, groupId, calculationMethodPid);
			protocolMock.Setup(p => p.NotifyProtocol(269, groupId, "")).Returns(deltaValues);
			return deltaHelper;
		}

		internal static SnmpDeltaHelper SetupAccurateTimeoutDelta(this Mock<SLProtocol> protocolMock, int groupId, TimeSpan delta, uint calculationMethodPid = 100)
		{
			protocolMock.Setup(p => p.GetParameter((int)calculationMethodPid)).Returns((int)CalculationMethod.Accurate);

			SnmpDeltaHelper deltaHelper = new SnmpDeltaHelper(protocolMock.Object, groupId, calculationMethodPid);
			protocolMock.Setup(p => p.NotifyProtocol(269, groupId, "")).Returns((int)delta.TotalMilliseconds);
			return deltaHelper;
		}
	}
}
