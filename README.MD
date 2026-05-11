# uhd.net

A cross-platform C# wrapper for the UHD (USRP Hardware Driver) library, enabling .NET applications to interface with Ettus Research USRP devices.

Available on NuGet: [UhdNet](https://www.nuget.org/packages/UhdNet)

```
dotnet add package UhdNet
```

## Overview

This library wraps the native UHD C API via P/Invoke, providing a managed C# interface for transmitting and receiving RF signals using USRP hardware. It was agentic coded and has been tested against UHD version 4.9.0.

## Requirements

- UHD installed and accessible on the system path and defined in Environment variables
- .NET runtime

## Example
An example in Tests `rx_samples_from_file` is included, demonstrating how to receive IQ samples from sdr to a file using the wrapper — a C# equivalent of the demo of UHD.

## Platform Support
- Linux
- Windows
