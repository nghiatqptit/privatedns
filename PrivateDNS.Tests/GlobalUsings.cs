global using Xunit;
using System.Runtime.CompilerServices;

// Make internal classes visible to test assembly
[assembly: InternalsVisibleTo("PrivateDNS.Tests")]