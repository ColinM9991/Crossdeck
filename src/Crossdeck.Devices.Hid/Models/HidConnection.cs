using HidSharp.Reports;

namespace Crossdeck.Devices.Hid.Models;

public sealed record HidConnection(IHidTransport Transport, ReportDescriptor ReportDescriptor);