using System;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Niantic.Titan.Uploader.Tests")]
[assembly:InternalsVisibleTo("Niantic.Titan.GeoClient")]
[assembly:InternalsVisibleTo("Niantic.Titan.GeoClient.Editor")]
[assembly:InternalsVisibleTo("Niantic.Titan.GeoClient.Tests.EditMode")]
[assembly:InternalsVisibleTo("Niantic.Titan.GeoClient.Tests.PlayMode")]
[assembly:InternalsVisibleTo("GeoClientSample")]

// NSubstitute generates proxy types that are compiled into an in-memory assembly at runtime.  The
// following line makes any internal types visible to this dynamically generated assembly.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]